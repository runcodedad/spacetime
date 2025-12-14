using System.Net;
using NSubstitute;

namespace Spacetime.Network.Tests;

public class PeerDiscoveryTests
{
    [Fact]
    public void AddSeedNode_WithValidEndPoint_AddsSeedNode()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var discovery = new PeerDiscovery(connectionManager, peerManager);
        var endPoint = new IPEndPoint(IPAddress.Loopback, 8333);

        // Act
        discovery.AddSeedNode(endPoint);
        var seedNodes = discovery.GetSeedNodes();

        // Assert
        Assert.Contains(endPoint, seedNodes);
    }

    [Fact]
    public void AddSeedNode_WithNullEndPoint_ThrowsArgumentNullException()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var discovery = new PeerDiscovery(connectionManager, peerManager);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => discovery.AddSeedNode(null!));
    }

    [Fact]
    public void GetSeedNodes_WithNoSeeds_ReturnsEmptyList()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var discovery = new PeerDiscovery(connectionManager, peerManager);

        // Act
        var seedNodes = discovery.GetSeedNodes();

        // Assert
        Assert.Empty(seedNodes);
    }

    [Fact]
    public void EncodePeerList_WithValidPeers_EncodesCorrectly()
    {
        // Arrange
        var peers = new List<IPEndPoint>
        {
            new IPEndPoint(IPAddress.Parse("192.168.1.100"), 8333),
            new IPEndPoint(IPAddress.Parse("10.0.0.1"), 8334)
        };

        // Act
        var encoded = PeerDiscovery.EncodePeerList(peers);

        // Assert
        Assert.NotEmpty(encoded);
        // 4 bytes for count + 2 * (1 byte length + 4 bytes IPv4 + 2 bytes port)
        Assert.Equal(4 + 2 * (1 + 4 + 2), encoded.Length);
    }

    [Fact]
    public void EncodePeerList_WithNullPeers_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PeerDiscovery.EncodePeerList(null!));
    }

    [Fact]
    public void EncodePeerList_WithEmptyList_EncodesZeroCount()
    {
        // Arrange
        var peers = new List<IPEndPoint>();

        // Act
        var encoded = PeerDiscovery.EncodePeerList(peers);

        // Assert
        Assert.Equal(4, encoded.Length); // Just the count field
    }

    [Fact]
    public async Task RequestPeersAsync_WithNullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var discovery = new PeerDiscovery(connectionManager, peerManager);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            discovery.RequestPeersAsync(null!));
    }

    [Fact]
    public async Task RequestPeersAsync_WithValidConnection_SendsGetPeersMessage()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var discovery = new PeerDiscovery(connectionManager, peerManager);
        var connection = Substitute.For<IPeerConnection>();
        
        // Configure connection to return null (no response)
        connection.ReceiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NetworkMessage?>(null));

        // Act
        await discovery.RequestPeersAsync(connection);

        // Assert
        await connection.Received(1).SendAsync(
            Arg.Is<NetworkMessage>(m => m.Type == MessageType.GetPeers),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DiscoverPeersAsync_WithNoSeedNodes_CompletesSuccessfully()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var discovery = new PeerDiscovery(connectionManager, peerManager);

        // Act
        await discovery.DiscoverPeersAsync();

        // Assert - should complete without error
        Assert.True(true);
    }

    [Fact]
    public async Task DiscoverPeersAsync_WithSeedNodes_AttemptsConnections()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();
        var peerManager = Substitute.For<IPeerManager>();
        var discovery = new PeerDiscovery(connectionManager, peerManager);
        
        var seedNode = new IPEndPoint(IPAddress.Loopback, 8333);
        discovery.AddSeedNode(seedNode);

        connectionManager.ConnectAsync(Arg.Any<IPEndPoint>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IPeerConnection?>(null));

        // Act
        await discovery.DiscoverPeersAsync();

        // Assert
        await connectionManager.Received(1).ConnectAsync(seedNode, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_WithNullConnectionManager_ThrowsArgumentNullException()
    {
        // Arrange
        var peerManager = Substitute.For<IPeerManager>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new PeerDiscovery(null!, peerManager));
    }

    [Fact]
    public void Constructor_WithNullPeerManager_ThrowsArgumentNullException()
    {
        // Arrange
        var connectionManager = Substitute.For<IConnectionManager>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new PeerDiscovery(connectionManager, null!));
    }
}
