using System.Net;
using NSubstitute;

namespace Spacetime.Network.Tests;

public class PeerExchangeTests
{
    private static PeerAddress CreateTestAddress(string ip, int port)
    {
        return new PeerAddress(new IPEndPoint(IPAddress.Parse(ip), port), "test");
    }

    [Fact]
    public void Constructor_WithNullAddressBook_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PeerExchange(null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();

        // Act
        var exchange = new PeerExchange(addressBook);

        // Assert
        Assert.NotNull(exchange);
    }

    [Fact]
    public async Task RequestPeersAsync_WithNullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var exchange = new PeerExchange(addressBook);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            exchange.RequestPeersAsync(null!));
    }

    [Fact]
    public async Task RequestPeersAsync_SendsGetPeersMessage()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var exchange = new PeerExchange(addressBook);
        var connection = Substitute.For<IPeerConnection>();

        connection.ReceiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NetworkMessage?>(null));

        // Act
        await exchange.RequestPeersAsync(connection);

        // Assert
        await connection.Received(1).SendAsync(
            Arg.Is<GetPeersMessage>(m => m.Type == MessageType.GetPeers),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestPeersAsync_WithPeerListResponse_ReturnsPeers()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var exchange = new PeerExchange(addressBook);
        var connection = Substitute.For<IPeerConnection>();

        var peers = new List<IPEndPoint>
        {
            new IPEndPoint(IPAddress.Parse("203.0.113.1"), 8000),
            new IPEndPoint(IPAddress.Parse("203.0.113.2"), 8000)
        };
        var response = new PeerListMessage(peers);

        connection.ReceiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NetworkMessage?>(response));

        // Act
        var result = await exchange.RequestPeersAsync(connection);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task RequestPeersAsync_WithTimeout_ReturnsEmptyList()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var exchange = new PeerExchange(addressBook, requestTimeout: TimeSpan.FromMilliseconds(10));
        var connection = Substitute.For<IPeerConnection>();

        connection.ReceiveAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(1000, callInfo.Arg<CancellationToken>());
                return null;
            });

        // Act
        var result = await exchange.RequestPeersAsync(connection);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void HandlePeerRequest_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var exchange = new PeerExchange(addressBook);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            exchange.HandlePeerRequest(null!, "peer1"));
    }

    [Fact]
    public void HandlePeerRequest_WithNullRequesterId_ThrowsArgumentNullException()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var exchange = new PeerExchange(addressBook);
        var request = new GetPeersMessage();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            exchange.HandlePeerRequest(request, null!));
    }

    [Fact]
    public void HandlePeerRequest_ReturnsAddressesFromBook()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var exchange = new PeerExchange(addressBook);

        var addresses = new List<PeerAddress>
        {
            CreateTestAddress("203.0.113.1", 8000),
            CreateTestAddress("203.0.113.2", 8000)
        };
        addressBook.GetBestAddresses(Arg.Any<int>(), Arg.Any<IEnumerable<IPEndPoint>?>())
            .Returns(addresses);

        var request = new GetPeersMessage(maxCount: 100);

        // Act
        var response = exchange.HandlePeerRequest(request, "peer1");

        // Assert
        Assert.Equal(2, response.Peers.Count);
    }

    [Fact]
    public void HandlePeerRequest_RespectsMaxCount()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var exchange = new PeerExchange(addressBook);

        var addresses = new List<PeerAddress>
        {
            CreateTestAddress("203.0.113.1", 8000),
            CreateTestAddress("203.0.113.2", 8000)
        };
        addressBook.GetBestAddresses(Arg.Any<int>(), Arg.Any<IEnumerable<IPEndPoint>?>())
            .Returns(addresses);

        var request = new GetPeersMessage(maxCount: 50);

        // Act
        exchange.HandlePeerRequest(request, "peer1");

        // Assert
        addressBook.Received(1).GetBestAddresses(50, Arg.Any<IEnumerable<IPEndPoint>?>());
    }

    [Fact]
    public void HandlePeerRequest_ExcludesSpecifiedAddresses()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var exchange = new PeerExchange(addressBook);

        addressBook.GetBestAddresses(Arg.Any<int>(), Arg.Any<IEnumerable<IPEndPoint>?>())
            .Returns(new List<PeerAddress>());

        var excludeList = new List<string> { "203.0.113.1:8000" };
        var request = new GetPeersMessage(excludeAddresses: excludeList);

        // Act
        exchange.HandlePeerRequest(request, "peer1");

        // Assert
        addressBook.Received(1).GetBestAddresses(
            Arg.Any<int>(),
            Arg.Is<IEnumerable<IPEndPoint>?>(endpoints => endpoints != null && endpoints.Any()));
    }

    [Fact]
    public void CanRequestPeers_WithNullPeerId_ThrowsArgumentNullException()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var exchange = new PeerExchange(addressBook);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exchange.CanRequestPeers(null!));
    }

    [Fact]
    public void CanRequestPeers_FirstTime_ReturnsTrue()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var exchange = new PeerExchange(addressBook);

        // Act
        var result = exchange.CanRequestPeers("peer1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanRequestPeers_WithinMinInterval_ReturnsFalse()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var exchange = new PeerExchange(
            addressBook,
            minRequestInterval: TimeSpan.FromMinutes(5));

        var request = new GetPeersMessage();
        exchange.HandlePeerRequest(request, "peer1");

        // Act
        var result = exchange.CanRequestPeers("peer1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanRequestPeers_AfterMinInterval_ReturnsTrue()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var exchange = new PeerExchange(
            addressBook,
            minRequestInterval: TimeSpan.FromMilliseconds(10));

        var request = new GetPeersMessage();
        exchange.HandlePeerRequest(request, "peer1");

        // Act
        Task.Delay(20).Wait();
        var result = exchange.CanRequestPeers("peer1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HandlePeerRequest_WhenRateLimited_ReturnsEmptyList()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var exchange = new PeerExchange(addressBook);

        var request = new GetPeersMessage();

        // Exhaust rate limiter by making multiple requests
        for (int i = 0; i < 20; i++)
        {
            exchange.HandlePeerRequest(request, "peer1");
        }

        // Act
        var response = exchange.HandlePeerRequest(request, "peer1");

        // Assert
        Assert.Empty(response.Peers);
    }
}
