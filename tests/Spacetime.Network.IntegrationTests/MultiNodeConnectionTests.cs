using System.Net;

namespace Spacetime.Network.IntegrationTests;

public class MultiNodeConnectionTests : IAsyncLifetime
{
    private TcpConnectionManager? _server1Manager;
    private TcpConnectionManager? _server2Manager;
    private PeerManager? _server1PeerManager;
    private PeerManager? _server2PeerManager;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_server1Manager != null)
        {
            await _server1Manager.DisposeAsync();
        }

        if (_server2Manager != null)
        {
            await _server2Manager.DisposeAsync();
        }
    }

    [Fact]
    public async Task TwoNodes_CanEstablishConnection()
    {
        // Arrange
        var codec = new LengthPrefixedMessageCodec();
        _server1PeerManager = new PeerManager();
        _server2PeerManager = new PeerManager();
        
        _server1Manager = new TcpConnectionManager(codec, _server1PeerManager, useTls: false);
        _server2Manager = new TcpConnectionManager(codec, _server2PeerManager, useTls: false);

        var server1Endpoint = new IPEndPoint(IPAddress.Loopback, 0);
        var server2Endpoint = new IPEndPoint(IPAddress.Loopback, 0);

        await _server1Manager.StartAsync(server1Endpoint);
        await _server2Manager.StartAsync(server2Endpoint);

        // Give the listeners time to start
        await Task.Delay(100);

        // Get the actual assigned port (since we used 0)
        var actualPort1 = 18000; // For testing, use fixed ports
        var actualPort2 = 18001;
        
        _server1Manager = new TcpConnectionManager(codec, _server1PeerManager, useTls: false);
        _server2Manager = new TcpConnectionManager(codec, _server2PeerManager, useTls: false);
        
        await _server1Manager.StartAsync(new IPEndPoint(IPAddress.Loopback, actualPort1));
        await _server2Manager.StartAsync(new IPEndPoint(IPAddress.Loopback, actualPort2));

        await Task.Delay(200);

        // Act - Connect server2 to server1
        var connection = await _server2Manager.ConnectAsync(
            new IPEndPoint(IPAddress.Loopback, actualPort1));

        // Assert
        Assert.NotNull(connection);
        Assert.True(connection.IsConnected);
        Assert.Single(_server2Manager.GetActiveConnections());
    }

    [Fact]
    public async Task ConnectedPeers_CanExchangeMessages()
    {
        // Arrange
        var codec = new LengthPrefixedMessageCodec();
        _server1PeerManager = new PeerManager();
        _server2PeerManager = new PeerManager();
        
        _server1Manager = new TcpConnectionManager(codec, _server1PeerManager, useTls: false);
        _server2Manager = new TcpConnectionManager(codec, _server2PeerManager, useTls: false);

        var port1 = 18002;
        var port2 = 18003;

        await _server1Manager.StartAsync(new IPEndPoint(IPAddress.Loopback, port1));
        await _server2Manager.StartAsync(new IPEndPoint(IPAddress.Loopback, port2));

        await Task.Delay(200);

        // Connect
        var clientConnection = await _server2Manager.ConnectAsync(
            new IPEndPoint(IPAddress.Loopback, port1));
        Assert.NotNull(clientConnection);

        await Task.Delay(100);

        // Get the server-side connection
        var serverConnections = _server1Manager.GetActiveConnections();
        Assert.Single(serverConnections);
        var serverConnection = serverConnections[0];

        // Act - Send a message from client to server
        var testPayload = new byte[] { 1, 2, 3, 4, 5 };
        var testMessage = new NetworkMessage(MessageType.Heartbeat, testPayload);
        await clientConnection.SendAsync(testMessage);

        // Receive the message on the server side
        var receivedMessage = await serverConnection.ReceiveAsync();

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal(MessageType.Heartbeat, receivedMessage.Type);
        Assert.Equal(testPayload, receivedMessage.Payload.ToArray());
    }

    [Fact]
    public async Task ConnectionManager_RespectsMaxConnections()
    {
        // Arrange
        var codec = new LengthPrefixedMessageCodec();
        _server1PeerManager = new PeerManager();
        _server2PeerManager = new PeerManager();
        
        const int maxConnections = 2;
        _server1Manager = new TcpConnectionManager(codec, _server1PeerManager, maxConnections: maxConnections, useTls: false);
        _server2Manager = new TcpConnectionManager(codec, _server2PeerManager, useTls: false);

        var port = 18004;
        await _server1Manager.StartAsync(new IPEndPoint(IPAddress.Loopback, port));
        await Task.Delay(200);

        // Act - Try to establish more connections than allowed
        var connection1 = await _server2Manager.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port));
        await Task.Delay(100);
        
        var connection2 = await _server2Manager.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port));
        await Task.Delay(100);
        
        var connection3 = await _server2Manager.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port));
        await Task.Delay(100);

        // Assert
        Assert.NotNull(connection1);
        Assert.NotNull(connection2);
        Assert.NotNull(connection3);
        
        // Server should accept only maxConnections
        var serverConnections = _server1Manager.GetActiveConnections();
        Assert.True(serverConnections.Count <= maxConnections);
    }

    [Fact]
    public async Task Handshake_CanBeExchanged()
    {
        // Arrange
        var codec = new LengthPrefixedMessageCodec();
        _server1PeerManager = new PeerManager();
        _server2PeerManager = new PeerManager();
        
        _server1Manager = new TcpConnectionManager(codec, _server1PeerManager, useTls: false);
        _server2Manager = new TcpConnectionManager(codec, _server2PeerManager, useTls: false);

        var port1 = 18005;
        var port2 = 18006;

        await _server1Manager.StartAsync(new IPEndPoint(IPAddress.Loopback, port1));
        await _server2Manager.StartAsync(new IPEndPoint(IPAddress.Loopback, port2));

        await Task.Delay(200);

        var clientConnection = await _server2Manager.ConnectAsync(
            new IPEndPoint(IPAddress.Loopback, port1));
        Assert.NotNull(clientConnection);

        await Task.Delay(100);

        var serverConnections = _server1Manager.GetActiveConnections();
        Assert.Single(serverConnections);
        var serverConnection = serverConnections[0];

        // Act - Exchange handshake messages
        var clientHandshake = new HandshakeMessage(
            1, 
            "client-node-123", 
            "Spacetime/1.0.0",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        var handshakePayload = clientHandshake.Serialize();
        await clientConnection.SendAsync(new NetworkMessage(MessageType.Handshake, handshakePayload));

        var receivedMessage = await serverConnection.ReceiveAsync();
        Assert.NotNull(receivedMessage);
        Assert.Equal(MessageType.Handshake, receivedMessage.Type);

        var receivedHandshake = HandshakeMessage.Deserialize(receivedMessage.Payload);

        // Assert
        Assert.Equal(clientHandshake.ProtocolVersion, receivedHandshake.ProtocolVersion);
        Assert.Equal(clientHandshake.NodeId, receivedHandshake.NodeId);
        Assert.Equal(clientHandshake.UserAgent, receivedHandshake.UserAgent);
    }

    [Fact]
    public async Task ConnectionFailure_HandledGracefully()
    {
        // Arrange
        var codec = new LengthPrefixedMessageCodec();
        _server1PeerManager = new PeerManager();
        
        _server1Manager = new TcpConnectionManager(codec, _server1PeerManager, useTls: false);

        // Act - Try to connect to a non-existent server
        var connection = await _server1Manager.ConnectAsync(
            new IPEndPoint(IPAddress.Loopback, 19999));

        // Assert - Should return null on connection failure
        Assert.Null(connection);
    }
}
