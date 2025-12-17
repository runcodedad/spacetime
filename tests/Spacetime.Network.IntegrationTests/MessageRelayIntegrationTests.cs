using System.Net;
using NSubstitute;

namespace Spacetime.Network.IntegrationTests;

public class MessageRelayIntegrationTests
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
    public async Task BroadcastAsync_ToMultiplePeers_SendsToAllExceptSource()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();

        var peer1 = new PeerInfo("peer1", new IPEndPoint(IPAddress.Loopback, 8001), 1);
        var peer2 = new PeerInfo("peer2", new IPEndPoint(IPAddress.Loopback, 8002), 1);
        var peer3 = new PeerInfo("peer3", new IPEndPoint(IPAddress.Loopback, 8003), 1);

        var connection1 = Substitute.For<IPeerConnection>();
        connection1.PeerInfo.Returns(peer1);
        connection1.IsConnected.Returns(true);

        var connection2 = Substitute.For<IPeerConnection>();
        connection2.PeerInfo.Returns(peer2);
        connection2.IsConnected.Returns(true);

        var connection3 = Substitute.For<IPeerConnection>();
        connection3.PeerInfo.Returns(peer3);
        connection3.IsConnected.Returns(true);

        connectionManager.GetActiveConnections().Returns(new[] { connection1, connection2, connection3 });

        await using var relay = new MessageRelay(connectionManager, peerManager);
        var message = CreateTestMessage(1);

        // Act
        await relay.BroadcastAsync(message, sourcePeerId: "peer1");

        // Give background worker time to process
        await Task.Delay(100);

        // Assert - Should send to peer2 and peer3, but not peer1 (source)
        await connection1.DidNotReceive().SendAsync(Arg.Any<NetworkMessage>(), Arg.Any<CancellationToken>());
        await connection2.Received(1).SendAsync(Arg.Is<NetworkMessage>(m => m.Type == MessageType.Transaction), Arg.Any<CancellationToken>());
        await connection3.Received(1).SendAsync(Arg.Is<NetworkMessage>(m => m.Type == MessageType.Transaction), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RelayAsync_WithValidMessage_BroadcastsToOtherPeers()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();

        var peer1 = new PeerInfo("peer1", new IPEndPoint(IPAddress.Loopback, 8001), 1);
        var peer2 = new PeerInfo("peer2", new IPEndPoint(IPAddress.Loopback, 8002), 1);

        var connection1 = Substitute.For<IPeerConnection>();
        connection1.PeerInfo.Returns(peer1);
        connection1.IsConnected.Returns(true);

        var connection2 = Substitute.For<IPeerConnection>();
        connection2.PeerInfo.Returns(peer2);
        connection2.IsConnected.Returns(true);

        connectionManager.GetActiveConnections().Returns(new[] { connection1, connection2 });

        await using var relay = new MessageRelay(connectionManager, peerManager);
        var message = CreateTestMessage(1);

        // Act
        var result = await relay.RelayAsync(message, "peer1");

        // Give background worker time to process
        await Task.Delay(100);

        // Assert
        Assert.True(result);
        await connection1.DidNotReceive().SendAsync(Arg.Any<NetworkMessage>(), Arg.Any<CancellationToken>());
        await connection2.Received(1).SendAsync(Arg.Is<NetworkMessage>(m => m.Type == MessageType.Transaction), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RelayAsync_WithDuplicateMessage_DoesNotRelay()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();

        var peer1 = new PeerInfo("peer1", new IPEndPoint(IPAddress.Loopback, 8001), 1);
        var connection1 = Substitute.For<IPeerConnection>();
        connection1.PeerInfo.Returns(peer1);
        connection1.IsConnected.Returns(true);

        connectionManager.GetActiveConnections().Returns(new[] { connection1 });

        await using var relay = new MessageRelay(connectionManager, peerManager);
        var message = CreateTestMessage(1);

        // Act - Relay same message twice
        var result1 = await relay.RelayAsync(message, "peer1");
        await Task.Delay(100);
        var result2 = await relay.RelayAsync(message, "peer2");
        await Task.Delay(100);

        // Assert
        Assert.True(result1);
        Assert.False(result2); // Second relay should be rejected as duplicate
        Assert.Equal(1, relay.TotalDuplicatesFiltered);
    }

    [Fact]
    public async Task RelayAsync_WithRateLimitExceeded_DropsMessage()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var rateLimiter = new RateLimiter(maxTokens: 2, refillInterval: TimeSpan.FromSeconds(10), refillAmount: 1);

        var peer1 = new PeerInfo("peer1", new IPEndPoint(IPAddress.Loopback, 8001), 1);
        var connection1 = Substitute.For<IPeerConnection>();
        connection1.PeerInfo.Returns(peer1);
        connection1.IsConnected.Returns(true);

        connectionManager.GetActiveConnections().Returns(new[] { connection1 });

        await using var relay = new MessageRelay(connectionManager, peerManager, rateLimiter: rateLimiter);

        // Act - Relay 3 messages rapidly (exceeds rate limit of 2)
        var result1 = await relay.RelayAsync(CreateTestMessage(1), "source1");
        var result2 = await relay.RelayAsync(CreateTestMessage(2), "source1");
        var result3 = await relay.RelayAsync(CreateTestMessage(3), "source1");

        await Task.Delay(100);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.False(result3); // Third message should be rate limited
        Assert.True(relay.TotalMessagesDropped > 0);
    }

    [Fact]
    public async Task BroadcastAsync_WithMultipleMessages_SendsAll()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();

        var peer1 = new PeerInfo("peer1", new IPEndPoint(IPAddress.Loopback, 8001), 1);
        var connection1 = Substitute.For<IPeerConnection>();
        connection1.PeerInfo.Returns(peer1);
        connection1.IsConnected.Returns(true);

        connectionManager.GetActiveConnections().Returns(new[] { connection1 });

        await using var relay = new MessageRelay(connectionManager, peerManager);

        var msg1 = CreateTestMessage(1); 
        var msg2 = CreateTestMessage(2); 

        // Act - Broadcast multiple messages
        await relay.BroadcastAsync(msg1);
        await relay.BroadcastAsync(msg2);

        await Task.Delay(300);

        // Assert - Both messages should be sent
        await connection1.Received(2).SendAsync(Arg.Any<NetworkMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BroadcastAsync_WithBandwidthExceeded_DropsMessages()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var bandwidthMonitor = new BandwidthMonitor(maxBytesPerSecondPerPeer: 500, maxTotalBytesPerSecond: 1000);

        var peer1 = new PeerInfo("peer1", new IPEndPoint(IPAddress.Loopback, 8001), 1);
        var connection1 = Substitute.For<IPeerConnection>();
        connection1.PeerInfo.Returns(peer1);
        connection1.IsConnected.Returns(true);

        connectionManager.GetActiveConnections().Returns(new[] { connection1 });

        await using var relay = new MessageRelay(connectionManager, peerManager, bandwidthMonitor: bandwidthMonitor);

        // Act - Broadcast multiple messages that exceed bandwidth limit
        for (int i = 0; i < 10; i++)
        {
            await relay.BroadcastAsync(CreateTestMessage(i));
        }

        await Task.Delay(200);

        // Assert - Not all messages should be sent due to bandwidth limits
        var callCount = connection1.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IPeerConnection.SendAsync));
        Assert.True(callCount < 10, "Some messages should have been dropped due to bandwidth limits");
    }

    [Fact]
    public async Task RelayAsync_RecordsSuccessForPeer()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();

        var peer1 = new PeerInfo("peer1", new IPEndPoint(IPAddress.Loopback, 8001), 1);
        var connection1 = Substitute.For<IPeerConnection>();
        connection1.PeerInfo.Returns(peer1);
        connection1.IsConnected.Returns(true);

        connectionManager.GetActiveConnections().Returns(new[] { connection1 });

        await using var relay = new MessageRelay(connectionManager, peerManager);
        var message = CreateTestMessage(1);

        // Act
        await relay.RelayAsync(message, "source1");
        await Task.Delay(100);

        // Assert
        peerManager.Received(1).RecordSuccess("peer1");
    }

    [Fact]
    public async Task RelayAsync_WithSendFailure_RecordsFailureForPeer()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();

        var peer1 = new PeerInfo("peer1", new IPEndPoint(IPAddress.Loopback, 8001), 1);
        var connection1 = Substitute.For<IPeerConnection>();
        connection1.PeerInfo.Returns(peer1);
        connection1.IsConnected.Returns(true);
        connection1.SendAsync(Arg.Any<NetworkMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("Connection lost")));

        connectionManager.GetActiveConnections().Returns(new[] { connection1 });

        await using var relay = new MessageRelay(connectionManager, peerManager);
        var message = CreateTestMessage(1);

        // Act
        await relay.RelayAsync(message, "source1");
        await Task.Delay(100);

        // Assert
        peerManager.Received(1).RecordFailure("peer1");
        Assert.True(relay.TotalMessagesDropped > 0);
    }

    [Fact]
    public async Task BroadcastAsync_SkipsDisconnectedPeers()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();

        var peer1 = new PeerInfo("peer1", new IPEndPoint(IPAddress.Loopback, 8001), 1);
        var peer2 = new PeerInfo("peer2", new IPEndPoint(IPAddress.Loopback, 8002), 1);

        var connection1 = Substitute.For<IPeerConnection>();
        connection1.PeerInfo.Returns(peer1);
        connection1.IsConnected.Returns(true);

        var connection2 = Substitute.For<IPeerConnection>();
        connection2.PeerInfo.Returns(peer2);
        connection2.IsConnected.Returns(false); // Disconnected

        connectionManager.GetActiveConnections().Returns(new[] { connection1, connection2 });

        await using var relay = new MessageRelay(connectionManager, peerManager);
        var message = CreateTestMessage(1);

        // Act
        await relay.BroadcastAsync(message);
        await Task.Delay(100);

        // Assert
        await connection1.Received(1).SendAsync(Arg.Any<NetworkMessage>(), Arg.Any<CancellationToken>());
        await connection2.DidNotReceive().SendAsync(Arg.Any<NetworkMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MessageRelay_DisposesCleanly()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var relay = new MessageRelay(connectionManager, peerManager);

        // Act
        await relay.DisposeAsync();

        // Assert - Should not throw and stats should be available
        Assert.Equal(0, relay.TotalMessagesRelayed);
        Assert.Equal(0, relay.TotalDuplicatesFiltered);
        Assert.Equal(0, relay.TotalMessagesDropped);
    }
}
