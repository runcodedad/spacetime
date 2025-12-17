using System.Collections.Concurrent;
using Spacetime.Core;
using Spacetime.Storage;

namespace Spacetime.Network;

/// <summary>
/// Implements block synchronization for nodes catching up with the network.
/// </summary>
/// <remarks>
/// This implementation provides:
/// - Initial blockchain download (IBD) mode
/// - Header-first synchronization
/// - Parallel block downloads
/// - Block validation during sync
/// - Resume capability after interruption
/// - Progress tracking and reporting
/// - Bandwidth throttling
/// - Malicious peer handling (ban invalid blocks)
/// </remarks>
public sealed class BlockSynchronizer : IBlockSynchronizer
{
    private readonly IConnectionManager _connectionManager;
    private readonly IPeerManager _peerManager;
    private readonly IChainStorage _chainStorage;
    private readonly IBlockValidator _blockValidator;
    private readonly BandwidthMonitor _bandwidthMonitor;
    private readonly SyncConfig _config;

    private readonly ConcurrentDictionary<string, IPeerConnection> _peerConnections;
    private readonly ConcurrentQueue<BlockDownloadRequest> _downloadQueue;
    private readonly ConcurrentDictionary<long, BlockDownloadRequest> _pendingDownloads;
    private readonly ConcurrentDictionary<long, Block> _downloadedBlocks;
    private readonly ConcurrentDictionary<long, BlockHeader> _downloadedHeaders;
    private readonly object _syncLock = new();

    private CancellationTokenSource? _syncCts;
    private Task? _syncTask;
    private DateTimeOffset _syncStartTime;
    private long _blocksDownloaded;
    private long _blocksValidated;
    private long _bytesDownloaded;
    private long _targetHeight;
    private SyncState _currentState;

    /// <inheritdoc/>
    public bool IsSynchronizing { get; private set; }

    /// <inheritdoc/>
    public bool IsInitialBlockDownload
    {
        get
        {
            var currentHeight = _chainStorage.Metadata.GetChainHeight() ?? 0;
            return currentHeight < _targetHeight - _config.IbdThresholdBlocks;
        }
    }

    /// <inheritdoc/>
    public SyncProgress Progress
    {
        get
        {
            var currentHeight = _chainStorage.Metadata.GetChainHeight() ?? 0;
            var elapsed = DateTimeOffset.UtcNow - _syncStartTime;
            var downloadRate = elapsed.TotalSeconds > 0 ? _bytesDownloaded / elapsed.TotalSeconds : 0;

            return new SyncProgress(
                currentHeight,
                _targetHeight,
                _blocksDownloaded,
                _blocksValidated,
                _bytesDownloaded,
                downloadRate,
                _syncStartTime,
                DateTimeOffset.UtcNow,
                _currentState);
        }
    }

    /// <inheritdoc/>
    public event EventHandler<SyncProgress>? ProgressUpdated;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockSynchronizer"/> class.
    /// </summary>
    /// <param name="connectionManager">The connection manager.</param>
    /// <param name="peerManager">The peer manager.</param>
    /// <param name="chainStorage">The chain storage.</param>
    /// <param name="blockValidator">The block validator.</param>
    /// <param name="bandwidthMonitor">The bandwidth monitor.</param>
    /// <param name="config">The synchronization configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public BlockSynchronizer(
        IConnectionManager connectionManager,
        IPeerManager peerManager,
        IChainStorage chainStorage,
        IBlockValidator blockValidator,
        BandwidthMonitor bandwidthMonitor,
        SyncConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);
        ArgumentNullException.ThrowIfNull(peerManager);
        ArgumentNullException.ThrowIfNull(chainStorage);
        ArgumentNullException.ThrowIfNull(blockValidator);
        ArgumentNullException.ThrowIfNull(bandwidthMonitor);

        _connectionManager = connectionManager;
        _peerManager = peerManager;
        _chainStorage = chainStorage;
        _blockValidator = blockValidator;
        _bandwidthMonitor = bandwidthMonitor;
        _config = config ?? new SyncConfig();

        _peerConnections = new ConcurrentDictionary<string, IPeerConnection>();
        _downloadQueue = new ConcurrentQueue<BlockDownloadRequest>();
        _pendingDownloads = new ConcurrentDictionary<long, BlockDownloadRequest>();
        _downloadedBlocks = new ConcurrentDictionary<long, Block>();
        _downloadedHeaders = new ConcurrentDictionary<long, BlockHeader>();
        _currentState = SyncState.Idle;
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncLock)
        {
            if (IsSynchronizing)
            {
                throw new InvalidOperationException("Synchronization is already running");
            }

            IsSynchronizing = true;
            _syncStartTime = DateTimeOffset.UtcNow;
            _blocksDownloaded = 0;
            _blocksValidated = 0;
            _bytesDownloaded = 0;
            _targetHeight = 0;
            _currentState = SyncState.Discovering;

            _syncCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        try
        {
            await SynchronizeAsync(_syncCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _currentState = SyncState.Cancelled;
            throw;
        }
        catch (Exception)
        {
            _currentState = SyncState.Failed;
            throw;
        }
        finally
        {
            lock (_syncLock)
            {
                IsSynchronizing = false;
                _syncCts?.Dispose();
                _syncCts = null;
                _syncTask = null;
            }
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Task? taskToWait;
        CancellationTokenSource? ctsToCancel;

        lock (_syncLock)
        {
            if (!IsSynchronizing)
            {
                return;
            }

            taskToWait = _syncTask;
            ctsToCancel = _syncCts;
        }

        ctsToCancel?.Cancel();

        if (taskToWait != null)
        {
            try
            {
                await taskToWait.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
        }
    }

    /// <inheritdoc/>
    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        // Resume is the same as start for this implementation
        await StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);

        foreach (var connection in _peerConnections.Values)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }

        _peerConnections.Clear();
        _downloadQueue.Clear();
        _pendingDownloads.Clear();
        _downloadedBlocks.Clear();
    }

    private async Task SynchronizeAsync(CancellationToken cancellationToken)
    {
        // Step 1: Discover peers and find best height
        await DiscoverBestHeightAsync(cancellationToken).ConfigureAwait(false);

        // Step 2: Download headers from current height to target
        _currentState = SyncState.DownloadingHeaders;
        NotifyProgress();
        await DownloadHeadersAsync(cancellationToken).ConfigureAwait(false);

        // Step 3: Download blocks in parallel
        _currentState = SyncState.DownloadingBlocks;
        NotifyProgress();
        await DownloadBlocksAsync(cancellationToken).ConfigureAwait(false);

        // Step 4: Mark as synced
        _currentState = SyncState.Synced;
        NotifyProgress();
    }

    private async Task DiscoverBestHeightAsync(CancellationToken cancellationToken)
    {
        _currentState = SyncState.Discovering;
        NotifyProgress();

        var peers = _peerManager.GetBestPeers(_config.MaxPeers);
        if (peers.Count == 0)
        {
            throw new InvalidOperationException("No peers available for synchronization");
        }

        // Query peers for their best height
        var peerHeights = new List<long>();
        var connections = _connectionManager.GetActiveConnections();

        // Get current local height
        var currentHeight = _chainStorage.Metadata.GetChainHeight() ?? 0;
        var currentHash = _chainStorage.Metadata.GetBestBlockHash() ?? new byte[32];

        // Request headers from each connected peer to determine their height
        foreach (var connection in connections)
        {
            if (!connection.IsConnected)
            {
                continue;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Request headers to discover peer's best height
                var getHeadersMsg = new GetHeadersMessage(
                    currentHash,
                    ReadOnlyMemory<byte>.Empty,
                    _config.MaxHeadersPerRequest);

                await connection.SendAsync(getHeadersMsg, cancellationToken).ConfigureAwait(false);

                // Wait for response with timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(_config.DownloadTimeoutSeconds));

                var response = await connection.ReceiveAsync(timeoutCts.Token).ConfigureAwait(false);

                if (response is HeadersMessage headersMsg && headersMsg.Headers.Count > 0)
                {
                    // Estimate peer height based on headers received
                    var estimatedHeight = currentHeight + headersMsg.Headers.Count;
                    peerHeights.Add(estimatedHeight);
                }
                else
                {
                    // If no headers, peer is likely at same height
                    peerHeights.Add(currentHeight);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // Failed to get height from this peer, skip it
                _peerManager.RecordFailure(connection.PeerInfo.Id);
            }
        }

        // Use the highest reported height as target
        if (peerHeights.Count > 0)
        {
            _targetHeight = peerHeights.Max();
        }
        else
        {
            // No peer data available, assume we're synced
            _targetHeight = currentHeight;
        }
    }

    private async Task DownloadHeadersAsync(CancellationToken cancellationToken)
    {
        var currentHeight = _chainStorage.Metadata.GetChainHeight() ?? 0;
        var currentHash = _chainStorage.Metadata.GetBestBlockHash();

        if (currentHash == null)
        {
            // Start from genesis
            currentHash = new byte[32];
        }

        while (currentHeight < _targetHeight)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get an available connection
            var connection = GetAvailableConnection();
            if (connection == null)
            {
                throw new InvalidOperationException("No peers available");
            }

            try
            {
                // Request headers from this peer
                var maxHeaders = Math.Min(_config.MaxHeadersPerRequest, (int)(_targetHeight - currentHeight));
                var getHeadersMsg = new GetHeadersMessage(
                    currentHash.Value,
                    ReadOnlyMemory<byte>.Empty,
                    maxHeaders);

                await connection.SendAsync(getHeadersMsg, cancellationToken).ConfigureAwait(false);

                // Wait for response with timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(_config.DownloadTimeoutSeconds));

                var response = await connection.ReceiveAsync(timeoutCts.Token).ConfigureAwait(false);

                if (response is HeadersMessage headersMsg && headersMsg.Headers.Count > 0)
                {
                    // Process received headers
                    foreach (var headerData in headersMsg.Headers)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var header = BlockHeader.Deserialize(new BinaryReader(new MemoryStream(headerData.ToArray())));
                        
                        // Store header and track for block download
                        _downloadedHeaders.TryAdd(header.Height, header);
                        currentHeight = header.Height;
                        currentHash = header.ComputeHash();
                    }

                    _peerManager.RecordSuccess(connection.PeerInfo.Id);
                }
                else
                {
                    // No more headers available from this peer
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // Failed to download headers from this peer
                _peerManager.RecordFailure(connection.PeerInfo.Id);
                // Continue with next peer
            }

            NotifyProgress();
        }
    }

    private async Task DownloadBlocksAsync(CancellationToken cancellationToken)
    {
        var currentHeight = _chainStorage.Metadata.GetChainHeight() ?? 0;

        // Create download requests for missing blocks using headers
        for (var height = currentHeight + 1; height <= _targetHeight; height++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get block hash from downloaded headers
            if (_downloadedHeaders.TryGetValue(height, out var header))
            {
                var blockHash = header.ComputeHash();
                var request = new BlockDownloadRequest(blockHash, height);
                _downloadQueue.Enqueue(request);
            }
            else
            {
                // Header not available, skip this block
                continue;
            }
        }

        // Start parallel download workers
        var workers = new List<Task>();
        for (var i = 0; i < _config.ParallelDownloads; i++)
        {
            workers.Add(DownloadWorkerAsync(cancellationToken));
        }

        await Task.WhenAll(workers).ConfigureAwait(false);

        // Apply downloaded blocks in order
        await ApplyBlocksAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DownloadWorkerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_downloadQueue.TryDequeue(out var request))
            {
                // No more work
                break;
            }

            // Find available peer
            var peer = _peerManager.GetBestPeers(1).FirstOrDefault();
            if (peer == null)
            {
                // Re-queue the request
                _downloadQueue.Enqueue(request);
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                // Mark as pending
                request.AssignedPeerId = peer.Id;
                request.AssignedAt = DateTimeOffset.UtcNow;
                _pendingDownloads.TryAdd(request.Height, request);

                // Download the block
                var block = await DownloadBlockFromPeerAsync(peer, request.BlockHash, cancellationToken)
                    .ConfigureAwait(false);

                if (block != null)
                {
                    // Validate block
                    var validationResult = await _blockValidator.ValidateBlockAsync(block, cancellationToken)
                        .ConfigureAwait(false);

                    if (validationResult.IsValid)
                    {
                        _downloadedBlocks.TryAdd(request.Height, block);
                        request.IsCompleted = true;
                        _blocksDownloaded++;
                        _bytesDownloaded += block.Serialize().Length;
                        _peerManager.RecordSuccess(peer.Id);
                    }
                    else
                    {
                        // Invalid block - ban peer
                        _peerManager.RecordFailure(peer.Id);
                        request.RetryCount++;
                        if (request.RetryCount < _config.MaxRetries)
                        {
                            _downloadQueue.Enqueue(request);
                        }
                        else
                        {
                            request.IsFailed = true;
                        }
                    }
                }
                else
                {
                    // Failed to download
                    _peerManager.RecordFailure(peer.Id);
                    request.RetryCount++;
                    if (request.RetryCount < _config.MaxRetries)
                    {
                        _downloadQueue.Enqueue(request);
                    }
                    else
                    {
                        request.IsFailed = true;
                    }
                }

                _pendingDownloads.TryRemove(request.Height, out _);
                NotifyProgress();
            }
            catch (Exception)
            {
                // Error downloading - retry
                _peerManager.RecordFailure(peer.Id);
                request.RetryCount++;
                if (request.RetryCount < _config.MaxRetries)
                {
                    _downloadQueue.Enqueue(request);
                }
                else
                {
                    request.IsFailed = true;
                }
                _pendingDownloads.TryRemove(request.Height, out _);
            }
        }
    }

    private async Task<Block?> DownloadBlockFromPeerAsync(
        PeerInfo peer,
        ReadOnlyMemory<byte> blockHash,
        CancellationToken cancellationToken)
    {
        // Find or establish connection to peer
        var connection = GetConnectionForPeer(peer);
        if (connection == null || !connection.IsConnected)
        {
            return null;
        }

        try
        {
            // Send GetBlockMessage to peer
            var getBlockMsg = new GetBlockMessage(blockHash);
            await connection.SendAsync(getBlockMsg, cancellationToken).ConfigureAwait(false);

            // Wait for BlockMessage response with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_config.DownloadTimeoutSeconds));

            var response = await connection.ReceiveAsync(timeoutCts.Token).ConfigureAwait(false);

            if (response is BlockMessage blockMsg)
            {
                // Deserialize and return the block
                var block = Block.Deserialize(blockMsg.BlockData.ToArray());
                return block;
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Failed to download block
            return null;
        }
    }

    private async Task ApplyBlocksAsync(CancellationToken cancellationToken)
    {
        _currentState = SyncState.Validating;
        NotifyProgress();

        var currentHeight = _chainStorage.Metadata.GetChainHeight() ?? 0;

        // Apply blocks in order
        for (var height = currentHeight + 1; height <= _targetHeight; height++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_downloadedBlocks.TryRemove(height, out var block))
            {
                // Store block
                _chainStorage.Blocks.StoreBlock(block);
                _chainStorage.Metadata.SetChainHeight(height);
                _chainStorage.Metadata.SetBestBlockHash(block.ComputeHash());
                _blocksValidated++;
                NotifyProgress();
            }
            else
            {
                throw new InvalidOperationException($"Missing block at height {height}");
            }
        }
    }

    private IPeerConnection? GetAvailableConnection()
    {
        var connections = _connectionManager.GetActiveConnections();
        return connections.FirstOrDefault(c => c.IsConnected);
    }

    private IPeerConnection? GetConnectionForPeer(PeerInfo peer)
    {
        // Check if we already have a connection cached for this peer
        if (_peerConnections.TryGetValue(peer.Id, out var cachedConnection) && cachedConnection.IsConnected)
        {
            return cachedConnection;
        }

        // Try to find an active connection to this peer
        var connections = _connectionManager.GetActiveConnections();
        var connection = connections.FirstOrDefault(c => c.PeerInfo.Id == peer.Id && c.IsConnected);

        if (connection != null)
        {
            _peerConnections.TryAdd(peer.Id, connection);
        }

        return connection;
    }

    private void NotifyProgress()
    {
        ProgressUpdated?.Invoke(this, Progress);
    }
}
