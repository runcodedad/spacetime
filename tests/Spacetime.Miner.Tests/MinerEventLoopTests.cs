using MerkleTree.Hashing;
using NSubstitute;
using Spacetime.Consensus;
using Spacetime.Core;
using Spacetime.Network;
using Spacetime.Plotting;

namespace Spacetime.Miner.Tests;

public class MinerEventLoopTests
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

    public MinerEventLoopTests()
    {
        _config = MinerConfiguration.Default();
        _plotManager = Substitute.For<IPlotManager>();
        _epochManager = Substitute.For<IEpochManager>();
        _connectionManager = Substitute.For<IConnectionManager>();
        _messageRelay = Substitute.For<IMessageRelay>();
        _blockSigner = Substitute.For<IBlockSigner>();
        _blockValidator = Substitute.For<IBlockValidator>();
        _mempool = Substitute.For<IMempool>();
        _hashFunction = Substitute.For<IHashFunction>();
        _chainState = Substitute.For<IChainState>();
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MinerEventLoop(
                null!,
                _plotManager,
                _epochManager,
                _connectionManager,
                _messageRelay,
                _blockSigner,
                _blockValidator,
                _mempool,
                _hashFunction,
                _chainState));
    }

    [Fact]
    public void Constructor_WithNullPlotManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MinerEventLoop(
                _config,
                null!,
                _epochManager,
                _connectionManager,
                _messageRelay,
                _blockSigner,
                _blockValidator,
                _mempool,
                _hashFunction,
                _chainState));
    }

    [Fact]
    public void Constructor_WithNullEpochManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MinerEventLoop(
                _config,
                _plotManager,
                null!,
                _connectionManager,
                _messageRelay,
                _blockSigner,
                _blockValidator,
                _mempool,
                _hashFunction,
                _chainState));
    }

    [Fact]
    public void Constructor_WithNullConnectionManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MinerEventLoop(
                _config,
                _plotManager,
                _epochManager,
                null!,
                _messageRelay,
                _blockSigner,
                _blockValidator,
                _mempool,
                _hashFunction,
                _chainState));
    }

    [Fact]
    public void Constructor_WithNullMessageRelay_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MinerEventLoop(
                _config,
                _plotManager,
                _epochManager,
                _connectionManager,
                null!,
                _blockSigner,
                _blockValidator,
                _mempool,
                _hashFunction,
                _chainState));
    }

    [Fact]
    public void Constructor_WithNullBlockSigner_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MinerEventLoop(
                _config,
                _plotManager,
                _epochManager,
                _connectionManager,
                _messageRelay,
                null!,
                _blockValidator,
                _mempool,
                _hashFunction,
                _chainState));
    }

    [Fact]
    public void Constructor_WithNullBlockValidator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MinerEventLoop(
                _config,
                _plotManager,
                _epochManager,
                _connectionManager,
                _messageRelay,
                _blockSigner,
                null!,
                _mempool,
                _hashFunction,
                _chainState));
    }

    [Fact]
    public void Constructor_WithNullMempool_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MinerEventLoop(
                _config,
                _plotManager,
                _epochManager,
                _connectionManager,
                _messageRelay,
                _blockSigner,
                _blockValidator,
                null!,
                _hashFunction,
                _chainState));
    }

    [Fact]
    public void Constructor_WithNullHashFunction_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MinerEventLoop(
                _config,
                _plotManager,
                _epochManager,
                _connectionManager,
                _messageRelay,
                _blockSigner,
                _blockValidator,
                _mempool,
                null!,
                _chainState));
    }

    [Fact]
    public void Constructor_WithNullChainState_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MinerEventLoop(
                _config,
                _plotManager,
                _epochManager,
                _connectionManager,
                _messageRelay,
                _blockSigner,
                _blockValidator,
                _mempool,
                _hashFunction,
                null!));
    }

    [Fact]
    public void Constructor_WithValidArguments_InitializesProperties()
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
        Assert.Equal(0, eventLoop.TotalChallengesReceived);
        Assert.Equal(0, eventLoop.TotalProofsGenerated);
        Assert.Equal(0, eventLoop.TotalProofsSubmitted);
        Assert.Equal(0, eventLoop.TotalBlocksWon);
    }

    [Fact]
    public async Task StartAsync_WithNoPlots_StartsSuccessfullyWithWarning()
    {
        // Arrange
        _plotManager.ValidPlotCount.Returns(0);
        _plotManager.LoadMetadataAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
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

        // Assert - should start successfully even with no plots
        Assert.True(eventLoop.IsRunning);

        // Cleanup
        await eventLoop.StopAsync();
        await eventLoop.DisposeAsync();
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        _plotManager.ValidPlotCount.Returns(1);
        _plotManager.LoadMetadataAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var mockConnection = Substitute.For<IPeerConnection>();
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

        await eventLoop.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await eventLoop.StartAsync());

        await eventLoop.StopAsync();
        await eventLoop.DisposeAsync();
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_DoesNotThrow()
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

        // Act & Assert - should not throw
        await eventLoop.StopAsync();
        await eventLoop.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_DisposesResources()
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

        // Act
        await eventLoop.DisposeAsync();

        // Assert - subsequent operations should throw
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await eventLoop.StartAsync());
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_DoesNotThrow()
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

        // Act & Assert - should not throw on multiple dispose
        await eventLoop.DisposeAsync();
        await eventLoop.DisposeAsync();
        await eventLoop.DisposeAsync();
    }
}
