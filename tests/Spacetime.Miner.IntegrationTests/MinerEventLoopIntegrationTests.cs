using MerkleTree.Hashing;
using NSubstitute;
using Spacetime.Consensus;
using Spacetime.Core;
using Spacetime.Network;
using Spacetime.Plotting;

namespace Spacetime.Miner.IntegrationTests;

/// <summary>
/// Integration tests for the MinerEventLoop that verify component interactions.
/// </summary>
public class MinerEventLoopIntegrationTests : IAsyncDisposable
{
    private readonly string _tempDir;
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

    public MinerEventLoopIntegrationTests()
    {
        // Create temporary directory for tests
        _tempDir = Path.Combine(Path.GetTempPath(), $"miner_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        // Setup configuration
        _config = new MinerConfiguration
        {
            PlotDirectory = _tempDir,
            PlotMetadataPath = Path.Combine(_tempDir, "metadata.json"),
            NodeAddress = "127.0.0.1",
            NodePort = 8333,
            PrivateKeyPath = Path.Combine(_tempDir, "key.dat"),
            NetworkId = "testnet",
            MaxConcurrentProofs = 1,
            ProofGenerationTimeoutSeconds = 5,
            ConnectionRetryIntervalSeconds = 1,
            MaxConnectionRetries = 2,
            EnablePerformanceMonitoring = false
        };

        // Create mocks for dependencies
        _plotManager = Substitute.For<IPlotManager>();
        _epochManager = Substitute.For<IEpochManager>();
        _connectionManager = Substitute.For<IConnectionManager>();
        _messageRelay = Substitute.For<IMessageRelay>();
        _blockSigner = Substitute.For<IBlockSigner>();
        _blockValidator = Substitute.For<IBlockValidator>();
        _mempool = Substitute.For<IMempool>();
        _hashFunction = new Sha256HashFunction();
        _chainState = Substitute.For<IChainState>();

        // Setup default mock behaviors
        _epochManager.CurrentChallenge.Returns(new byte[32]);
        _epochManager.CurrentEpoch.Returns(0);
        _blockSigner.GetPublicKey().Returns(new byte[33]);
    }

    [Fact]
    public async Task MinerEventLoop_CanBeConstructed_WithValidDependencies()
    {
        // Act
        var eventLoop = new MinerEventLoop(
            _config,
            _plotManager,
            _epochManager,
            _connectionManager,
            _messageRelay,
            _blockSigner,
            _blockValidator,
            _mempool,
            _hashFunction,
            _chainState);

        // Assert
        Assert.NotNull(eventLoop);
        Assert.False(eventLoop.IsRunning);

        // Cleanup
        await eventLoop.DisposeAsync();
    }

    [Fact]
    public async Task MinerEventLoop_LoadsPlotMetadata_OnStart()
    {
        // Arrange
        _plotManager.ValidPlotCount.Returns(1);
        _plotManager.LoadMetadataAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        var mockConnection = Substitute.For<IPeerConnection>();
        mockConnection.IsConnected.Returns(true);
        _connectionManager.ConnectAsync(Arg.Any<System.Net.IPEndPoint>(), Arg.Any<CancellationToken>())
            .Returns(mockConnection);

        var eventLoop = new MinerEventLoop(
            _config,
            _plotManager,
            _epochManager,
            _connectionManager,
            _messageRelay,
            _blockSigner,
            _blockValidator,
            _mempool,
            _hashFunction,
            _chainState);

        // Act
        await eventLoop.StartAsync();

        // Assert
        await _plotManager.Received(1).LoadMetadataAsync(Arg.Any<CancellationToken>());
        Assert.True(eventLoop.IsRunning);

        // Cleanup
        await eventLoop.StopAsync();
        await eventLoop.DisposeAsync();
    }

    [Fact]
    public async Task MinerEventLoop_AttemptsConnection_OnStart()
    {
        // Arrange
        _plotManager.ValidPlotCount.Returns(1);
        _plotManager.LoadMetadataAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        var mockConnection = Substitute.For<IPeerConnection>();
        mockConnection.IsConnected.Returns(true);
        _connectionManager.ConnectAsync(Arg.Any<System.Net.IPEndPoint>(), Arg.Any<CancellationToken>())
            .Returns(mockConnection);

        var eventLoop = new MinerEventLoop(
            _config,
            _plotManager,
            _epochManager,
            _connectionManager,
            _messageRelay,
            _blockSigner,
            _blockValidator,
            _mempool,
            _hashFunction,
            _chainState);

        // Act
        await eventLoop.StartAsync();

        // Assert
        await _connectionManager.Received(1).ConnectAsync(
            Arg.Any<System.Net.IPEndPoint>(),
            Arg.Any<CancellationToken>());
        Assert.True(eventLoop.IsRunning);

        // Cleanup
        await eventLoop.StopAsync();
        await eventLoop.DisposeAsync();
    }

    [Fact]
    public async Task MinerEventLoop_RetriesConnection_OnFailure()
    {
        // Arrange
        _plotManager.ValidPlotCount.Returns(1);
        _plotManager.LoadMetadataAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        var attemptCount = 0;
        _connectionManager.ConnectAsync(Arg.Any<System.Net.IPEndPoint>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                attemptCount++;
                if (attemptCount < 2)
                {
                    throw new IOException("Connection refused");
                }
                var mockConnection = Substitute.For<IPeerConnection>();
                mockConnection.IsConnected.Returns(true);
                return mockConnection;
            });

        var eventLoop = new MinerEventLoop(
            _config,
            _plotManager,
            _epochManager,
            _connectionManager,
            _messageRelay,
            _blockSigner,
            _blockValidator,
            _mempool,
            _hashFunction,
            _chainState);

        // Act
        await eventLoop.StartAsync();

        // Assert - should have retried
        Assert.True(attemptCount >= 2);
        Assert.True(eventLoop.IsRunning);

        // Cleanup
        await eventLoop.StopAsync();
        await eventLoop.DisposeAsync();
    }

    [Fact]
    public async Task MinerEventLoop_FailsToStart_AfterMaxRetries()
    {
        // Arrange
        _plotManager.ValidPlotCount.Returns(1);
        _plotManager.LoadMetadataAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        _connectionManager.ConnectAsync(Arg.Any<System.Net.IPEndPoint>(), Arg.Any<CancellationToken>())
            .Returns<IPeerConnection>(_ => throw new IOException("Connection refused"));

        var eventLoop = new MinerEventLoop(
            _config,
            _plotManager,
            _epochManager,
            _connectionManager,
            _messageRelay,
            _blockSigner,
            _blockValidator,
            _mempool,
            _hashFunction,
            _chainState);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await eventLoop.StartAsync());

        Assert.False(eventLoop.IsRunning);

        // Cleanup
        await eventLoop.DisposeAsync();
    }

    [Fact]
    public async Task MinerEventLoop_StopsGracefully_AfterStart()
    {
        // Arrange
        _plotManager.ValidPlotCount.Returns(1);
        _plotManager.LoadMetadataAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        var mockConnection = Substitute.For<IPeerConnection>();
        mockConnection.IsConnected.Returns(true);
        _connectionManager.ConnectAsync(Arg.Any<System.Net.IPEndPoint>(), Arg.Any<CancellationToken>())
            .Returns(mockConnection);

        var eventLoop = new MinerEventLoop(
            _config,
            _plotManager,
            _epochManager,
            _connectionManager,
            _messageRelay,
            _blockSigner,
            _blockValidator,
            _mempool,
            _hashFunction,
            _chainState);

        // Act
        await eventLoop.StartAsync();
        Assert.True(eventLoop.IsRunning);

        await eventLoop.StopAsync();

        // Assert
        Assert.False(eventLoop.IsRunning);

        // Cleanup
        await eventLoop.DisposeAsync();
    }

    [Fact]
    public async Task MinerEventLoop_TracksStatistics_Correctly()
    {
        // Arrange
        var eventLoop = new MinerEventLoop(
            _config,
            _plotManager,
            _epochManager,
            _connectionManager,
            _messageRelay,
            _blockSigner,
            _blockValidator,
            _mempool,
            _hashFunction,
            _chainState);

        // Assert initial state
        Assert.Equal(0, eventLoop.TotalChallengesReceived);
        Assert.Equal(0, eventLoop.TotalProofsGenerated);
        Assert.Equal(0, eventLoop.TotalProofsSubmitted);
        Assert.Equal(0, eventLoop.TotalBlocksWon);

        // Cleanup
        await eventLoop.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup temporary directory
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }

        await Task.CompletedTask;
    }
}
