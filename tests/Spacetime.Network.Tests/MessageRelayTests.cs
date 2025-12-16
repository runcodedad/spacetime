using NSubstitute;

namespace Spacetime.Network.Tests;

public class MessageRelayTests
{
    private static TransactionMessage CreateTestMessage(int seed = 0)
    {
        var data = new byte[100];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(seed + i);
        }
        return new TransactionMessage(data);
    }

    [Fact]
    public void Constructor_WithNullConnectionManager_ThrowsArgumentNullException()
    {
        // Arrange
        var peerManager = Substitute.For<IPeerManager>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MessageRelay(null!, peerManager));
    }

    [Fact]
    public void Constructor_WithNullPeerManager_ThrowsArgumentNullException()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MessageRelay(connectionManager, null!));
    }

    [Fact]
    public async Task ShouldRelay_WithValidBlockMessage_ReturnsTrue()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        await using var relay = new MessageRelay(connectionManager, peerManager);

        var blockData = new byte[100];
        var message = new BlockMessage(blockData);

        // Act
        var result = relay.ShouldRelay(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ShouldRelay_WithValidTransactionMessage_ReturnsTrue()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        await using var relay = new MessageRelay(connectionManager, peerManager);

        var message = CreateTestMessage(1);

        // Act
        var result = relay.ShouldRelay(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ShouldRelay_WithDuplicateMessage_ReturnsFalse()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var messageTracker = new MessageTracker();
        await using var relay = new MessageRelay(connectionManager, peerManager, messageTracker: messageTracker);

        var message = CreateTestMessage(1);
        messageTracker.MarkAndCheckIfNew(message);

        // Act
        var result = relay.ShouldRelay(message);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldRelay_WithHandshakeMessage_ReturnsFalse()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        await using var relay = new MessageRelay(connectionManager, peerManager);

        var message = new HandshakeMessage(1, "node1", "Spacetime/1.0", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        // Act
        var result = relay.ShouldRelay(message);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldRelay_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        await using var relay = new MessageRelay(connectionManager, peerManager);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => relay.ShouldRelay(null!));
    }

    [Fact]
    public async Task BroadcastAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        await using var relay = new MessageRelay(connectionManager, peerManager);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await relay.BroadcastAsync(null!));
    }

    [Fact]
    public async Task RelayAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        await using var relay = new MessageRelay(connectionManager, peerManager);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await relay.RelayAsync(null!, "peer1"));
    }

    [Fact]
    public async Task RelayAsync_WithNullSourcePeerId_ThrowsArgumentNullException()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        await using var relay = new MessageRelay(connectionManager, peerManager);

        var message = CreateTestMessage(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await relay.RelayAsync(message, null!));
    }

    [Fact]
    public async Task RelayAsync_WithDuplicateMessage_ReturnsFalse()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var messageTracker = new MessageTracker();
        await using var relay = new MessageRelay(connectionManager, peerManager, messageTracker: messageTracker);

        var message = CreateTestMessage(1);
        messageTracker.MarkAndCheckIfNew(message);

        // Act
        var result = await relay.RelayAsync(message, "peer1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RelayAsync_WithRateLimitExceeded_ReturnsFalse()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var rateLimiter = new RateLimiter(maxTokens: 1);
        await using var relay = new MessageRelay(connectionManager, peerManager, rateLimiter: rateLimiter);

        var message1 = CreateTestMessage(1);
        var message2 = CreateTestMessage(2);

        // Act - First message should succeed, second should fail
        var result1 = await relay.RelayAsync(message1, "peer1");
        var result2 = await relay.RelayAsync(message2, "peer1");

        // Assert
        Assert.True(result1);
        Assert.False(result2);
    }

    [Fact]
    public async Task TotalMessagesRelayed_StartsAtZero()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        await using var relay = new MessageRelay(connectionManager, peerManager);

        // Assert
        Assert.Equal(0, relay.TotalMessagesRelayed);
    }

    [Fact]
    public async Task TotalDuplicatesFiltered_IncreasesOnDuplicate()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var messageTracker = new MessageTracker();
        await using var relay = new MessageRelay(connectionManager, peerManager, messageTracker: messageTracker);

        var message = CreateTestMessage(1);

        // Act
        messageTracker.MarkAndCheckIfNew(message); // Mark as seen first
        relay.ShouldRelay(message); // Should detect duplicate

        // Assert
        Assert.Equal(1, relay.TotalDuplicatesFiltered);
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var relay = new MessageRelay(connectionManager, peerManager);

        // Act
        await relay.DisposeAsync();
        await relay.DisposeAsync(); // Should not throw

        // Assert - No exception
    }

    [Fact]
    public async Task BroadcastAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var relay = new MessageRelay(connectionManager, peerManager);
        await relay.DisposeAsync();

        var message = CreateTestMessage(1);

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await relay.BroadcastAsync(message));
    }

    [Fact]
    public async Task RelayAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var relay = new MessageRelay(connectionManager, peerManager);
        await relay.DisposeAsync();

        var message = CreateTestMessage(1);

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await relay.RelayAsync(message, "peer1"));
    }
}
