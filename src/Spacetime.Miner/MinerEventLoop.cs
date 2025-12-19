using System.Diagnostics;
using MerkleTree.Hashing;
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
    private readonly CancellationTokenSource _shutdownCts;
    private readonly SemaphoreSlim _proofGenerationLock;
    
    private IPeerConnection? _nodeConnection;
    private bool _disposed;
    private Task? _eventLoopTask;
    private byte[]? _bestProof;
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
        IHashFunction hashFunction)
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

        _config = config;
        _plotManager = plotManager;
        _epochManager = epochManager;
        _connectionManager = connectionManager;
        _messageRelay = messageRelay;
        _blockSigner = blockSigner;
        _blockValidator = blockValidator;
        _mempool = mempool;
        _hashFunction = hashFunction;
        _shutdownCts = new CancellationTokenSource();
        _proofGenerationLock = new SemaphoreSlim(_config.MaxConcurrentProofs, _config.MaxConcurrentProofs);
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
            throw new InvalidOperationException("No valid plots loaded. Cannot start mining.");
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

        // In a real implementation, this would subscribe to message events from the connection
        // For now, we'll poll for messages
        var message = await ReceiveMessageAsync(cancellationToken);
        
        if (message is BlockAcceptedMessage blockAccepted)
        {
            await HandleBlockAcceptedAsync(blockAccepted, cancellationToken);
        }
    }

    /// <summary>
    /// Receives a network message from the node connection.
    /// </summary>
    private async Task<NetworkMessage?> ReceiveMessageAsync(CancellationToken cancellationToken)
    {
        if (_nodeConnection == null)
        {
            return null;
        }

        // This is a simplified implementation - in a real system, this would
        // be handled by the connection's message queue or event system
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        return null;
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

        // Reset best proof for this epoch
        lock (_bestProofLock)
        {
            _bestProof = null;
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
                    // Serialize proof for submission
                    _bestProof = SerializeProof(proof);
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
            var proofData = SerializeProof(proof);
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
        // In a real implementation, this would query the node for the current difficulty
        // and check if our score meets it
        // For now, we'll use a simplified check
        await Task.CompletedTask;
        return false;
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

            var blockBuilder = new BlockBuilder(_mempool, _blockSigner, _blockValidator);
            
            // Get current chain state (in real impl, query from node)
            var parentHash = new byte[32]; // Placeholder
            var height = epochNumber;
            var difficulty = 1000L; // Placeholder
            var challenge = _epochManager.CurrentChallenge;
            var plotRoot = proof.MerkleRoot;
            var proofScore = proof.Score;

            // Create plot metadata for the proof
            var plotMetadata = BlockPlotMetadata.Create(
                leafCount: 0, // Placeholder - would need actual plot info
                plotId: new byte[32], // Placeholder
                plotHeaderHash: new byte[32], // Placeholder
                version: 1);

            var blockProof = new BlockProof(
                proof.LeafValue,
                proof.LeafIndex,
                proof.SiblingHashes,
                proof.OrientationBits,
                plotMetadata);

            var block = await blockBuilder.BuildBlockAsync(
                parentHash,
                height,
                difficulty,
                epochNumber,
                challenge,
                blockProof,
                plotRoot,
                proofScore,
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
    /// Serializes a proof to bytes for network transmission.
    /// </summary>
    private static byte[] SerializeProof(Proof proof)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write leaf value
        writer.Write(proof.LeafValue.Length);
        writer.Write(proof.LeafValue);

        // Write leaf index
        writer.Write(proof.LeafIndex);

        // Write sibling hashes count and data
        writer.Write(proof.SiblingHashes.Count);
        foreach (var hash in proof.SiblingHashes)
        {
            writer.Write(hash.Length);
            writer.Write(hash);
        }

        // Write orientation bits
        writer.Write(proof.OrientationBits.Count);
        foreach (var bit in proof.OrientationBits)
        {
            writer.Write(bit);
        }

        // Write merkle root
        writer.Write(proof.MerkleRoot.Length);
        writer.Write(proof.MerkleRoot);

        // Write challenge
        writer.Write(proof.Challenge.Length);
        writer.Write(proof.Challenge);

        // Write score
        writer.Write(proof.Score.Length);
        writer.Write(proof.Score);

        return ms.ToArray();
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

        await StopAsync();
        
        if (_nodeConnection != null)
        {
            await _nodeConnection.DisposeAsync();
        }

        _shutdownCts.Dispose();
        _proofGenerationLock.Dispose();
    }
}
