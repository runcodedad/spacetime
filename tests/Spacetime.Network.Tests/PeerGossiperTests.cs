using System.Net;
using NSubstitute;

namespace Spacetime.Network.Tests;

public class PeerGossiperTests
{
    private static PeerAddress CreateTestAddress(string ip, int port)
    {
        return new PeerAddress(new IPEndPoint(IPAddress.Parse(ip), port), "test");
    }

    private static PeerInfo CreateTestPeerInfo(string id, string ip, int port)
    {
        return new PeerInfo(id, new IPEndPoint(IPAddress.Parse(ip), port), 1);
    }

    [Fact]
    public void Constructor_WithNullAddressBook_ThrowsArgumentNullException()
    {
        // Arrange
        var peerManager = Substitute.For<IPeerManager>();
        var connectionManager = Substitute.For<IConnectionManager>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PeerGossiper(null!, peerManager, connectionManager));
    }

    [Fact]
    public void Constructor_WithNullPeerManager_ThrowsArgumentNullException()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var connectionManager = Substitute.For<IConnectionManager>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PeerGossiper(addressBook, null!, connectionManager));
    }

    [Fact]
    public void Constructor_WithNullConnectionManager_ThrowsArgumentNullException()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var peerManager = Substitute.For<IPeerManager>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PeerGossiper(addressBook, peerManager, null!));
    }

    [Fact]
    public void Constructor_WithInvalidAddressesPerGossip_ThrowsArgumentException()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var peerManager = Substitute.For<IPeerManager>();
        var connectionManager = Substitute.For<IConnectionManager>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new PeerGossiper(addressBook, peerManager, connectionManager, addressesPerGossip: 0));
    }

    [Fact]
    public async Task StartAsync_WhenNotRunning_StartsSuccessfully()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var peerManager = Substitute.For<IPeerManager>();
        var connectionManager = Substitute.For<IConnectionManager>();
        var gossiper = new PeerGossiper(addressBook, peerManager, connectionManager);

        // Act
        await gossiper.StartAsync();

        // Assert - no exception thrown
        await gossiper.StopAsync();
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var peerManager = Substitute.For<IPeerManager>();
        var connectionManager = Substitute.For<IConnectionManager>();
        var gossiper = new PeerGossiper(addressBook, peerManager, connectionManager);

        await gossiper.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => gossiper.StartAsync());

        await gossiper.StopAsync();
    }

    [Fact]
    public async Task StopAsync_WhenRunning_StopsSuccessfully()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var peerManager = Substitute.For<IPeerManager>();
        var connectionManager = Substitute.For<IConnectionManager>();
        var gossiper = new PeerGossiper(addressBook, peerManager, connectionManager);

        await gossiper.StartAsync();

        // Act
        await gossiper.StopAsync();

        // Assert - can start again
        await gossiper.StartAsync();
        await gossiper.StopAsync();
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_CompletesSuccessfully()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var peerManager = Substitute.For<IPeerManager>();
        var connectionManager = Substitute.For<IConnectionManager>();
        var gossiper = new PeerGossiper(addressBook, peerManager, connectionManager);

        // Act & Assert
        await gossiper.StopAsync();
    }

    [Fact]
    public void ProcessReceivedAddresses_WithNullAddresses_ThrowsArgumentNullException()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var peerManager = Substitute.For<IPeerManager>();
        var connectionManager = Substitute.For<IConnectionManager>();
        var gossiper = new PeerGossiper(addressBook, peerManager, connectionManager);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            gossiper.ProcessReceivedAddresses(null!, "peer1"));
    }

    [Fact]
    public void ProcessReceivedAddresses_WithNullSourceId_ThrowsArgumentNullException()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var peerManager = Substitute.For<IPeerManager>();
        var connectionManager = Substitute.For<IConnectionManager>();
        var gossiper = new PeerGossiper(addressBook, peerManager, connectionManager);

        var addresses = new List<PeerAddress> { CreateTestAddress("203.0.113.1", 8000) };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            gossiper.ProcessReceivedAddresses(addresses, null!));
    }

    [Fact]
    public void ProcessReceivedAddresses_AddsNewAddressesToBook()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var peerManager = Substitute.For<IPeerManager>();
        var connectionManager = Substitute.For<IConnectionManager>();
        var gossiper = new PeerGossiper(addressBook, peerManager, connectionManager);

        var address = CreateTestAddress("203.0.113.1", 8000);
        var addresses = new List<PeerAddress> { address };

        addressBook.GetAddress(Arg.Any<IPEndPoint>()).Returns((PeerAddress?)null);
        addressBook.AddAddress(Arg.Any<PeerAddress>()).Returns(true);

        // Act
        gossiper.ProcessReceivedAddresses(addresses, "peer1");

        // Assert
        addressBook.Received(1).AddAddress(
            Arg.Is<PeerAddress>(a => a.EndPoint.Equals(address.EndPoint)));
    }

    [Fact]
    public void ProcessReceivedAddresses_UpdatesExistingAddresses()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var peerManager = Substitute.For<IPeerManager>();
        var connectionManager = Substitute.For<IConnectionManager>();
        var gossiper = new PeerGossiper(addressBook, peerManager, connectionManager);

        var address = CreateTestAddress("203.0.113.1", 8000);
        var addresses = new List<PeerAddress> { address };

        addressBook.GetAddress(address.EndPoint).Returns(address);

        // Act
        gossiper.ProcessReceivedAddresses(addresses, "peer1");

        // Assert
        addressBook.Received(1).UpdateLastSeen(address.EndPoint);
    }

    [Fact]
    public void ProcessReceivedAddresses_DeduplicatesRecentlySeen()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var peerManager = Substitute.For<IPeerManager>();
        var connectionManager = Substitute.For<IConnectionManager>();
        var gossiper = new PeerGossiper(
            addressBook,
            peerManager,
            connectionManager,
            addressDeduplicationWindow: TimeSpan.FromSeconds(10));

        var address = CreateTestAddress("203.0.113.1", 8000);
        var addresses = new List<PeerAddress> { address };

        addressBook.GetAddress(Arg.Any<IPEndPoint>()).Returns((PeerAddress?)null);
        addressBook.AddAddress(Arg.Any<PeerAddress>()).Returns(true);

        // Act
        gossiper.ProcessReceivedAddresses(addresses, "peer1");
        gossiper.ProcessReceivedAddresses(addresses, "peer2"); // Duplicate

        // Assert - should only be added once
        addressBook.Received(1).AddAddress(Arg.Any<PeerAddress>());
    }

    [Fact]
    public void ProcessReceivedAddresses_AllowsAfterDeduplicationWindow()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var peerManager = Substitute.For<IPeerManager>();
        var connectionManager = Substitute.For<IConnectionManager>();
        var gossiper = new PeerGossiper(
            addressBook,
            peerManager,
            connectionManager,
            addressDeduplicationWindow: TimeSpan.FromMilliseconds(10));

        var address = CreateTestAddress("203.0.113.1", 8000);
        var addresses = new List<PeerAddress> { address };

        addressBook.GetAddress(Arg.Any<IPEndPoint>()).Returns((PeerAddress?)null);
        addressBook.AddAddress(Arg.Any<PeerAddress>()).Returns(true);

        // Act
        gossiper.ProcessReceivedAddresses(addresses, "peer1");
        Task.Delay(20).Wait();
        gossiper.ProcessReceivedAddresses(addresses, "peer2");

        // Assert - should be added twice (after window expires)
        addressBook.Received(2).AddAddress(Arg.Any<PeerAddress>());
    }

    [Fact]
    public async Task DisposeAsync_StopsGossiper()
    {
        // Arrange
        var addressBook = Substitute.For<IPeerAddressBook>();
        var peerManager = Substitute.For<IPeerManager>();
        var connectionManager = Substitute.For<IConnectionManager>();
        var gossiper = new PeerGossiper(addressBook, peerManager, connectionManager);

        await gossiper.StartAsync();

        // Act
        await gossiper.DisposeAsync();

        // Assert - should be able to start again
        await gossiper.StartAsync();
        await gossiper.StopAsync();
    }
}
