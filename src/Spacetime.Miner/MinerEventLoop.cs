using System.Diagnostics;
using MerkleTree.Hashing;
using Spacetime.Consensus;
using Spacetime.Core;
using Spacetime.Network;
using Spacetime.Plotting;

namespace Spacetime.Miner;

/// <summary>
/// Main event loop for the miner node.
/// </summary>
/// <remarks>
/// The miner event loop:
/// - Loads configuration and plots on startup
/// - Connects to a full node or validator
/// - Listens for BlockAccepted messages to derive new challenges
/// - Generates proofs from all loaded plots in response to challenges
/// - Submits winning proofs to the network
/// - Builds and broadcasts blocks when winning
/// - Handles concurrent epochs and error recovery
/// </remarks>
public sealed class MinerEventLoop : IAsyncDisposable
{
    private readonly MinerConfiguration _config;
    private readonly IPlotManager _plotManager;
    private readonly IEpochManager _epochManager;
    private readonly IConnectionManager _connectionManager;
    private readonly IMessageRelay _messageRelay;
    private readonly IBlockSigner _blockSigner;
    private readonly IBlockValidator _blockValidator;
    private readonly IMempool _mempool;
    private readonly IHashFunction _hashFunction;
    private readonly IChainState _chainState;
    private readonly ProofValidator _proofValidator;
    private readonly CancellationTokenSource _shutdownCts;
    private readonly SemaphoreSlim _proofGenerationLock;
    
    private IPeerConnection? _nodeConnection;
    private bool _disposed;
    private Task? _eventLoopTask;
    private byte[]? _bestProofScore;
    private readonly object _bestProofLock = new();

    /// <summary>
    /// Gets a value indicating whether the miner is currently running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets the total number of challenges received.
    /// </summary>
    public long TotalChallengesReceived { get; private set; }

    /// <summary>
    /// Gets the total number of proofs generated.
    /// </summary>
    public long TotalProofsGenerated { get; private set; }

    /// <summary>
    /// Gets the total number of proofs submitted.
    /// </summary>
    public long TotalProofsSubmitted { get; private set; }

    /// <summary>
    /// Gets the total number of blocks won and built.
    /// </summary>
    public long TotalBlocksWon { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MinerEventLoop"/> class.
    /// </summary>
    /// <param name="config">The miner configuration.</param>
    /// <param name="plotManager">The plot manager.</param>
    /// <param name="epochManager">The epoch manager.</param>
    /// <param name="connectionManager">The connection manager.</param>
    /// <param name="messageRelay">The message relay.</param>
    /// <param name="blockSigner">The block signer.</param>
    /// <param name="blockValidator">The block validator.</param>
    /// <param name="mempool">The mempool.</param>
    /// <param name="hashFunction">The hash function.</param>
    /// <param name="chainState">The chain state for querying blockchain info.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public MinerEventLoop(
        MinerConfiguration config,
        IPlotManager plotManager,
        IEpochManager epochManager,
        IConnectionManager connectionManager,
        IMessageRelay messageRelay,
        IBlockSigner blockSigner,
        IBlockValidator blockValidator,
        IMempool mempool,
        IHashFunction hashFunction,
        IChainState chainState)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(plotManager);
        ArgumentNullException.ThrowIfNull(epochManager);
        ArgumentNullException.ThrowIfNull(connectionManager);
        ArgumentNullException.ThrowIfNull(messageRelay);
        ArgumentNullException.ThrowIfNull(blockSigner);
        ArgumentNullException.ThrowIfNull(blockValidator);
        ArgumentNullException.ThrowIfNull(mempool);
        ArgumentNullException.ThrowIfNull(hashFunction);
        ArgumentNullException.ThrowIfNull(chainState);

        _config = config;
        _plotManager = plotManager;
        _epochManager = epochManager;
        _connectionManager = connectionManager;
        _messageRelay = messageRelay;
        _blockSigner = blockSigner;
        _blockValidator = blockValidator;
        _mempool = mempool;
        _hashFunction = hashFunction;
        _chainState = chainState;
        _proofValidator = new ProofValidator(hashFunction);
        _shutdownCts = new CancellationTokenSource();
        _proofGenerationLock = new SemaphoreSlim(_config.MaxConcurrentProofs, _config.MaxConcurrentProofs);
        // Subscribe to plot manager events so the miner can react to runtime changes
        _plotManager.PlotAdded += PlotManager_PlotAdded;
        _plotManager.PlotRemoved += PlotManager_PlotRemoved;
    }

    private void PlotManager_PlotAdded(object? sender, Spacetime.Plotting.PlotChangedEventArgs e)
    {
        try
        {
            Console.WriteLine($"Plot added: {Path.GetFileName(e.Metadata.FilePath)} ({FormatBytes(e.Metadata.SpaceAllocatedBytes)})");
        }
        catch
        {
            // Ignore logging failures
        }
    }

    private void PlotManager_PlotRemoved(object? sender, Spacetime.Plotting.PlotChangedEventArgs e)
    {
        try
        {
            Console.WriteLine($"Plot removed: {Path.GetFileName(e.Metadata.FilePath)}");
        }
        catch
        {
            // Ignore logging failures
        }
    }

    /// <summary>
    /// Starts the miner event loop.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the miner is already running.</exception>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsRunning)
        {
            throw new InvalidOperationException("Miner is already running.");
        }

        Console.WriteLine("=== Miner Boot Sequence ===");

        // Step 1: Load configuration
        Console.WriteLine($"Network ID: {_config.NetworkId}");
        Console.WriteLine($"Node: {_config.NodeAddress}:{_config.NodePort}");
        Console.WriteLine($"Plot Directory: {_config.PlotDirectory}");

        // Step 2: Load plots
        Console.WriteLine("\nLoading plots...");
        await LoadPlotsAsync(cancellationToken);
        Console.WriteLine($"Loaded {_plotManager.ValidPlotCount} valid plots ({FormatBytes(_plotManager.TotalSpaceAllocatedBytes)})");

        if (_plotManager.ValidPlotCount == 0)
        {
            Console.WriteLine("⚠️  WARNING: No valid plots loaded. Miner will run but cannot generate proofs until plots are added.");
        }

        // Step 3: Connect to full node or validator
        Console.WriteLine("\nConnecting to full node...");
        await ConnectToNodeAsync(cancellationToken);
        Console.WriteLine("Connected successfully");

        // Step 4: Start event loop
        IsRunning = true;
        _eventLoopTask = Task.Run(() => RunEventLoopAsync(_shutdownCts.Token), _shutdownCts.Token);

        Console.WriteLine("\n=== Miner Started Successfully ===\n");
    }

    /// <summary>
    /// Stops the miner event loop.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        Console.WriteLine("Stopping miner...");

        // Signal shutdown
        await _shutdownCts.CancelAsync();

        // Wait for event loop to complete
        if (_eventLoopTask != null)
        {
            try
            {
                await _eventLoopTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        IsRunning = false;
        Console.WriteLine("Miner stopped");
    }

    /// <summary>
    /// Loads plots from the configured directory.
    /// </summary>
    private async Task LoadPlotsAsync(CancellationToken cancellationToken)
    {
        // Load metadata if exists
        await _plotManager.LoadMetadataAsync(cancellationToken);

        // Discover plot files in directory
        if (!Directory.Exists(_config.PlotDirectory))
        {
            Console.WriteLine($"Plot directory does not exist: {_config.PlotDirectory}");
            return;
        }

        var plotFiles = Directory.GetFiles(_config.PlotDirectory, "*.plot");
        Console.WriteLine($"Found {plotFiles.Length} plot files");

        foreach (var plotFile in plotFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var metadata = await _plotManager.AddPlotAsync(plotFile, null, cancellationToken);
                if (metadata?.Status == PlotStatus.Valid)
                {
                    Console.WriteLine($"  ✓ Loaded: {Path.GetFileName(plotFile)} ({FormatBytes(metadata.SpaceAllocatedBytes)})");
                }
                else
                {
                    Console.WriteLine($"  ✗ Failed: {Path.GetFileName(plotFile)} - {metadata?.Status}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error loading {Path.GetFileName(plotFile)}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Connects to the full node or validator.
    /// </summary>
    private async Task ConnectToNodeAsync(CancellationToken cancellationToken)
    {
        var attempts = 0;
        var endPoint = new System.Net.IPEndPoint(
            System.Net.IPAddress.Parse(_config.NodeAddress),
            _config.NodePort);
        
        while (attempts < _config.MaxConnectionRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _nodeConnection = await _connectionManager.ConnectAsync(
                    endPoint,
                    cancellationToken);
                
                return;
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts >= _config.MaxConnectionRetries)
                {
                    throw new InvalidOperationException(
                        $"Failed to connect after {_config.MaxConnectionRetries} attempts", ex);
                }

                Console.WriteLine($"Connection attempt {attempts} failed: {ex.Message}");
                Console.WriteLine($"Retrying in {_config.ConnectionRetryIntervalSeconds} seconds...");
                
                await Task.Delay(
                    TimeSpan.FromSeconds(_config.ConnectionRetryIntervalSeconds),
                    cancellationToken);
            }
        }
    }

    /// <summary>
    /// Main event loop that listens for challenges and generates proofs.
    /// </summary>
    private async Task RunEventLoopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Event loop started - listening for challenges...");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Listen for BlockAccepted messages
                    await ListenForChallengesAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Shutdown requested
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in event loop: {ex.Message}");
                    
                    // Attempt to recover connection
                    if (_nodeConnection == null || !_nodeConnection.IsConnected)
                    {
                        Console.WriteLine("Connection lost - attempting to reconnect...");
                        try
                        {
                            await ConnectToNodeAsync(cancellationToken);
                            Console.WriteLine("Reconnected successfully");
                        }
                        catch (Exception reconnectEx)
                        {
                            Console.WriteLine($"Reconnection failed: {reconnectEx.Message}");
                            throw;
                        }
                    }

                    // Brief delay before retrying
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error in event loop: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Listens for BlockAccepted messages and triggers proof generation.
    /// </summary>
    private async Task ListenForChallengesAsync(CancellationToken cancellationToken)
    {
        if (_nodeConnection == null)
        {
            throw new InvalidOperationException("Not connected to node");
        }

        // Receive message from the connection's message queue
        var message = await _nodeConnection.ReceiveAsync(cancellationToken);
        
        if (message is BlockAcceptedMessage blockAccepted)
        {
            await HandleBlockAcceptedAsync(blockAccepted, cancellationToken);
        }
    }

    /// <summary>
    /// Handles a BlockAccepted message by deriving a new challenge and generating proofs.
    /// </summary>
    private async Task HandleBlockAcceptedAsync(
        BlockAcceptedMessage blockAccepted,
        CancellationToken cancellationToken)
    {
        TotalChallengesReceived++;
        
        var height = blockAccepted.BlockHeight;
        var blockHash = blockAccepted.BlockHash;
        
        Console.WriteLine($"\n[Epoch {height + 1}] New challenge received (block {height})");

        // Derive new challenge from the block hash
        var challenge = ChallengeDerivation.DeriveChallenge(blockHash.Span, height + 1);
        _epochManager.AdvanceEpoch(blockHash);

        Console.WriteLine($"Challenge: {Convert.ToHexString(challenge)[..16]}...");

        // Reset best proof score for this epoch
        lock (_bestProofLock)
        {
            _bestProofScore = null;
        }

        // Generate proof with timeout
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            using var proofCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            proofCts.CancelAfter(TimeSpan.FromSeconds(_config.ProofGenerationTimeoutSeconds));

            await _proofGenerationLock.WaitAsync(proofCts.Token);
            try
            {
                await GenerateAndSubmitProofAsync(challenge, height + 1, proofCts.Token);
            }
            finally
            {
                _proofGenerationLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"Proof generation timed out after {stopwatch.Elapsed.TotalSeconds:F2}s");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating proof: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a proof from all plots and submits it if valid.
    /// </summary>
    private async Task GenerateAndSubmitProofAsync(
        byte[] challenge,
        long epochNumber,
        CancellationToken cancellationToken)
    {
        // Check if we have any plots to scan
        if (_plotManager.ValidPlotCount == 0)
        {
            if (_config.EnablePerformanceMonitoring)
            {
                Console.WriteLine("⚠️  No plots available to scan");
            }
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        
        // Use sampling strategy for faster proof generation (sample 10,000 leaves)
        var strategy = new SamplingScanStrategy(10000);
        
        var progress = _config.EnablePerformanceMonitoring 
            ? new Progress<double>(p => 
                {
                    if (p % 10 == 0)
                    {
                        Console.Write($"\rScanning plots... {p:F0}%");
                    }
                })
            : null;

        var proof = await _plotManager.GenerateProofAsync(
            challenge,
            strategy,
            progress,
            cancellationToken);

        stopwatch.Stop();

        if (proof != null)
        {
            TotalProofsGenerated++;
            
            if (_config.EnablePerformanceMonitoring)
            {
                Console.WriteLine($"\n✓ Proof generated in {stopwatch.Elapsed.TotalSeconds:F2}s");
                Console.WriteLine($"  Score: {Convert.ToHexString(proof.Score)[..16]}...");
            }

            // Track best proof
            lock (_bestProofLock)
            {
                if (_bestProofScore == null || CompareScores(proof.Score, _bestProofScore) < 0)
                {
                    _bestProofScore = proof.Score;
                }
            }

            // Submit proof to network
            await SubmitProofAsync(proof, epochNumber, cancellationToken);

            // Check if this proof wins the block
            // In a real implementation, the node would inform us if we won
            // For now, we'll check if the score meets the difficulty threshold
            if (await CheckIfWinningProofAsync(proof, cancellationToken))
            {
                await BuildAndBroadcastBlockAsync(proof, epochNumber, cancellationToken);
            }
        }
        else
        {
            Console.WriteLine($"\n✗ No valid proof found in {stopwatch.Elapsed.TotalSeconds:F2}s");
        }
    }

    /// <summary>
    /// Submits a proof to the network.
    /// </summary>
    private async Task SubmitProofAsync(
        Proof proof,
        long epochNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            // Convert Proof to BlockProof for submission
            var plotMetadata = FindPlotForProof(proof);
            if (plotMetadata == null)
            {
                Console.WriteLine("  ⚠️  Cannot submit proof: plot metadata not found");
                return;
            }

            var plotLoader = _plotManager.LoadedPlots.FirstOrDefault(p => p.MerkleRoot.SequenceEqual(proof.MerkleRoot));
            if (plotLoader == null)
            {
                Console.WriteLine("  ⚠️  Cannot submit proof: plot loader not found");
                return;
            }

            var blockPlotMetadata = BlockPlotMetadata.Create(
                leafCount: plotLoader.LeafCount,
                plotId: plotLoader.PlotSeed.ToArray(),
                plotHeaderHash: ComputePlotHeaderHash(plotLoader.Header),
                version: PlotHeader.FormatVersion);

            var blockProof = new BlockProof(
                proof.LeafValue,
                proof.LeafIndex,
                proof.SiblingHashes,
                proof.OrientationBits,
                blockPlotMetadata);

            // Serialize BlockProof
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            blockProof.Serialize(writer);
            var proofData = ms.ToArray();

            var minerId = _blockSigner.GetPublicKey();

            var message = new ProofSubmissionMessage(
                proofData,
                minerId,
                epochNumber);

            await _messageRelay.BroadcastAsync(message, sourcePeerId: null, cancellationToken);
            
            TotalProofsSubmitted++;
            
            if (_config.EnablePerformanceMonitoring)
            {
                Console.WriteLine("  Proof submitted to network");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error submitting proof: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a proof wins the block (meets difficulty threshold).
    /// </summary>
    private async Task<bool> CheckIfWinningProofAsync(Proof proof, CancellationToken cancellationToken)
    {
        // Get the current difficulty target from chain state
        var difficultyTarget = await GetDifficultyTargetAsync(cancellationToken);
        
        // Check if the proof score is below the difficulty target
        return ProofValidator.IsScoreBelowTarget(proof.Score, difficultyTarget);
    }

    /// <summary>
    /// Gets the difficulty target from the chain state.
    /// </summary>
    /// <remarks>
    /// The difficulty target is a 32-byte value. Proofs with scores less than this target are winners.
    /// This method converts the difficulty integer from chain state to a 32-byte target if needed.
    /// </remarks>
    private async Task<byte[]> GetDifficultyTargetAsync(CancellationToken cancellationToken)
    {
        var difficulty = await _chainState.GetExpectedDifficultyAsync(cancellationToken);
        
        // TODO: Convert difficulty integer to 32-byte target
        // For now, use a simple conversion (this should be replaced with proper difficulty adjustment logic)
        // Higher difficulty integer = lower target = harder to win
        var target = new byte[32];
        for (int i = 0; i < 32; i++)
        {
            target[i] = 0xFF;
        }
        
        // Reduce target based on difficulty (simplified - proper implementation needed)
        if (difficulty > 0)
        {
            var shift = Math.Min((int)(difficulty / 1000), 31);
            for (int i = 0; i < shift; i++)
            {
                target[i] = 0x00;
            }
        }
        
        return target;
    }

    /// <summary>
    /// Builds and broadcasts a winning block.
    /// </summary>
    private async Task BuildAndBroadcastBlockAsync(
        Proof proof,
        long epochNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine("\n🎉 WINNING PROOF! Building block...");

            // Get current chain state
            var parentHash = await _chainState.GetChainTipHashAsync(cancellationToken);
            if (parentHash == null)
            {
                Console.WriteLine("✗ Error: Cannot build block - chain tip not available");
                return;
            }

            var height = await _chainState.GetChainTipHeightAsync(cancellationToken) + 1;
            var difficulty = await _chainState.GetExpectedDifficultyAsync(cancellationToken);
            var challenge = await _chainState.GetExpectedChallengeAsync(cancellationToken);
            
            // Find the plot that generated this proof by matching merkle roots
            var plotMetadata = FindPlotForProof(proof);
            if (plotMetadata == null)
            {
                Console.WriteLine("✗ Error: Cannot find plot metadata for proof");
                return;
            }

            // Get the plot header to extract plot ID and other metadata
            var plotLoader = _plotManager.LoadedPlots.FirstOrDefault(p => p.MerkleRoot.SequenceEqual(proof.MerkleRoot));
            if (plotLoader == null)
            {
                Console.WriteLine("✗ Error: Cannot find plot loader for proof");
                return;
            }

            // Create BlockPlotMetadata from the plot header
            var blockPlotMetadata = BlockPlotMetadata.Create(
                leafCount: plotLoader.LeafCount,
                plotId: plotLoader.PlotSeed.ToArray(), // Use plot seed as plot ID
                plotHeaderHash: ComputePlotHeaderHash(plotLoader.Header),
                version: PlotHeader.FormatVersion);

            // Create BlockProof from the Proof
            var blockProof = new BlockProof(
                proof.LeafValue,
                proof.LeafIndex,
                proof.SiblingHashes,
                proof.OrientationBits,
                blockPlotMetadata);

            // Build the block
            var blockBuilder = new BlockBuilder(_mempool, _blockSigner, _blockValidator);
            var block = await blockBuilder.BuildBlockAsync(
                parentHash,
                height,
                difficulty,
                epochNumber,
                challenge,
                blockProof,
                proof.MerkleRoot,
                proof.Score,
                maxTransactions: 1000,
                cancellationToken);

            // Serialize and broadcast block
            var blockData = block.Serialize();
            var blockMessage = new BlockMessage(blockData);
            
            await _messageRelay.BroadcastAsync(blockMessage, sourcePeerId: null, cancellationToken);
            
            TotalBlocksWon++;
            
            Console.WriteLine($"✓ Block {height} broadcast successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error building/broadcasting block: {ex.Message}");
        }
    }

    /// <summary>
    /// Finds the plot metadata for a given proof by matching Merkle roots.
    /// </summary>
    private PlotMetadata? FindPlotForProof(Proof proof)
    {
        return _plotManager.PlotMetadataCollection
            .FirstOrDefault(m => m.MerkleRoot.SequenceEqual(proof.MerkleRoot));
    }

    /// <summary>
    /// Computes the hash of a plot header.
    /// </summary>
    private byte[] ComputePlotHeaderHash(PlotHeader header)
    {
        var serialized = header.Serialize();
        return _hashFunction.ComputeHash(serialized);
    }

    /// <summary>
    /// Compares two proof scores (lower is better).
    /// </summary>
    private static int CompareScores(ReadOnlySpan<byte> score1, ReadOnlySpan<byte> score2)
    {
        for (var i = 0; i < score1.Length && i < score2.Length; i++)
        {
            var diff = score1[i] - score2[i];
            if (diff != 0)
            {
                return diff;
            }
        }
        return 0;
    }

    /// <summary>
    /// Formats bytes to a human-readable string.
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        var order = 0;
        var size = (double)bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:F2} {sizes[order]}";
    }

    /// <summary>
    /// Disposes the miner event loop.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Unsubscribe from plot manager events
        try
        {
            _plotManager.PlotAdded -= PlotManager_PlotAdded;
            _plotManager.PlotRemoved -= PlotManager_PlotRemoved;
        }
        catch
        {
            // ignore
        }

        await StopAsync();
        
        if (_nodeConnection != null)
        {
            await _nodeConnection.DisposeAsync();
        }

        _shutdownCts.Dispose();
        _proofGenerationLock.Dispose();
    }
}
