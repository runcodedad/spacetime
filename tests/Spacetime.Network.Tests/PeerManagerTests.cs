using System.Net;

namespace Spacetime.Network.Tests;

public class PeerManagerTests
{
    private static PeerInfo CreateTestPeer(string id = "peer1", int port = 8000)
    {
        return new PeerInfo(id, new IPEndPoint(IPAddress.Loopback, port), 1);
    }

    [Fact]
    public void AddPeer_WithValidPeer_AddsPeerSuccessfully()
    {
        // Arrange
        var manager = new PeerManager();
        var peer = CreateTestPeer();

        // Act
        var result = manager.AddPeer(peer);

        // Assert
        Assert.True(result);
        Assert.Contains(peer, manager.KnownPeers);
    }

    [Fact]
    public void AddPeer_WithDuplicatePeer_ReturnsFalse()
    {
        // Arrange
        var manager = new PeerManager();
        var peer = CreateTestPeer();
        manager.AddPeer(peer);

        // Act
        var result = manager.AddPeer(peer);

        // Assert
        Assert.False(result);
        Assert.Single(manager.KnownPeers);
    }

    [Fact]
    public void AddPeer_WithNullPeer_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new PeerManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.AddPeer(null!));
    }

    [Fact]
    public void RemovePeer_WithExistingPeer_RemovesPeerSuccessfully()
    {
        // Arrange
        var manager = new PeerManager();
        var peer = CreateTestPeer();
        manager.AddPeer(peer);

        // Act
        var result = manager.RemovePeer(peer.Id);

        // Assert
        Assert.True(result);
        Assert.Empty(manager.KnownPeers);
    }

    [Fact]
    public void RemovePeer_WithNonExistentPeer_ReturnsFalse()
    {
        // Arrange
        var manager = new PeerManager();

        // Act
        var result = manager.RemovePeer("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetPeer_WithExistingPeer_ReturnsPeer()
    {
        // Arrange
        var manager = new PeerManager();
        var peer = CreateTestPeer();
        manager.AddPeer(peer);

        // Act
        var result = manager.GetPeer(peer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(peer.Id, result.Id);
    }

    [Fact]
    public void GetPeer_WithNonExistentPeer_ReturnsNull()
    {
        // Arrange
        var manager = new PeerManager();

        // Act
        var result = manager.GetPeer("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void UpdatePeerConnectionStatus_ConnectedPeer_UpdatesStatusAndLastSeen()
    {
        // Arrange
        var manager = new PeerManager();
        var peer = CreateTestPeer();
        manager.AddPeer(peer);
        var originalLastSeen = peer.LastSeen;

        // Act
        Thread.Sleep(10); // Ensure time passes
        manager.UpdatePeerConnectionStatus(peer.Id, true);

        // Assert
        Assert.True(peer.IsConnected);
        Assert.True(peer.LastSeen > originalLastSeen);
        Assert.Equal(0, peer.FailureCount);
    }

    [Fact]
    public void UpdatePeerConnectionStatus_DisconnectedPeer_UpdatesStatus()
    {
        // Arrange
        var manager = new PeerManager();
        var peer = CreateTestPeer();
        manager.AddPeer(peer);
        manager.UpdatePeerConnectionStatus(peer.Id, true);

        // Act
        manager.UpdatePeerConnectionStatus(peer.Id, false);

        // Assert
        Assert.False(peer.IsConnected);
    }

    [Fact]
    public void RecordSuccess_IncrementsReputationAndResetsFailures()
    {
        // Arrange
        var manager = new PeerManager();
        var peer = CreateTestPeer();
        manager.AddPeer(peer);
        peer.RecordFailure();
        var originalScore = peer.ReputationScore;

        // Act
        manager.RecordSuccess(peer.Id);

        // Assert
        Assert.Equal(originalScore + 1, peer.ReputationScore);
        Assert.Equal(0, peer.FailureCount);
    }

    [Fact]
    public void RecordFailure_DecrementsReputationAndIncrementsFailureCount()
    {
        // Arrange
        var manager = new PeerManager();
        var peer = CreateTestPeer();
        manager.AddPeer(peer);
        var originalScore = peer.ReputationScore;

        // Act
        manager.RecordFailure(peer.Id);

        // Assert
        Assert.Equal(originalScore - 2, peer.ReputationScore); // Failures decrement by 2
        Assert.Equal(1, peer.FailureCount);
    }

    [Fact]
    public void GetBestPeers_ReturnsHighestReputationPeers()
    {
        // Arrange
        var manager = new PeerManager();
        var peer1 = CreateTestPeer("peer1", 8001);
        var peer2 = CreateTestPeer("peer2", 8002);
        var peer3 = CreateTestPeer("peer3", 8003);
        
        manager.AddPeer(peer1);
        manager.AddPeer(peer2);
        manager.AddPeer(peer3);

        peer1.IncrementReputation(10);
        peer2.IncrementReputation(5);

        // Act
        var bestPeers = manager.GetBestPeers(2);

        // Assert
        Assert.Equal(2, bestPeers.Count);
        Assert.Equal("peer1", bestPeers[0].Id);
        Assert.Equal("peer2", bestPeers[1].Id);
    }

    [Fact]
    public void GetBestPeers_ExcludesConnectedPeers()
    {
        // Arrange
        var manager = new PeerManager();
        var peer1 = CreateTestPeer("peer1", 8001);
        var peer2 = CreateTestPeer("peer2", 8002);
        
        manager.AddPeer(peer1);
        manager.AddPeer(peer2);
        manager.UpdatePeerConnectionStatus(peer1.Id, true);

        // Act
        var bestPeers = manager.GetBestPeers(2);

        // Assert
        Assert.Single(bestPeers);
        Assert.Equal("peer2", bestPeers[0].Id);
    }

    [Fact]
    public void GetBestPeers_ExcludesBlacklistedPeers()
    {
        // Arrange
        var manager = new PeerManager(blacklistThreshold: -5, maxFailures: 3);
        var peer1 = CreateTestPeer("peer1", 8001);
        var peer2 = CreateTestPeer("peer2", 8002);
        
        manager.AddPeer(peer1);
        manager.AddPeer(peer2);

        // Blacklist peer1 by reputation
        peer1.DecrementReputation(10);

        // Act
        var bestPeers = manager.GetBestPeers(2);

        // Assert
        Assert.Single(bestPeers);
        Assert.Equal("peer2", bestPeers[0].Id);
    }

    [Fact]
    public void ShouldBlacklist_WithLowReputation_ReturnsTrue()
    {
        // Arrange
        var manager = new PeerManager(blacklistThreshold: -5);
        var peer = CreateTestPeer();
        manager.AddPeer(peer);
        peer.DecrementReputation(10);

        // Act
        var result = manager.ShouldBlacklist(peer.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldBlacklist_WithTooManyFailures_ReturnsTrue()
    {
        // Arrange
        var manager = new PeerManager(maxFailures: 3);
        var peer = CreateTestPeer();
        manager.AddPeer(peer);
        
        for (int i = 0; i < 3; i++)
        {
            manager.RecordFailure(peer.Id);
        }

        // Act
        var result = manager.ShouldBlacklist(peer.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldBlacklist_WithGoodPeer_ReturnsFalse()
    {
        // Arrange
        var manager = new PeerManager();
        var peer = CreateTestPeer();
        manager.AddPeer(peer);

        // Act
        var result = manager.ShouldBlacklist(peer.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ConnectedPeers_ReturnsOnlyConnectedPeers()
    {
        // Arrange
        var manager = new PeerManager();
        var peer1 = CreateTestPeer("peer1", 8001);
        var peer2 = CreateTestPeer("peer2", 8002);
        var peer3 = CreateTestPeer("peer3", 8003);
        
        manager.AddPeer(peer1);
        manager.AddPeer(peer2);
        manager.AddPeer(peer3);

        manager.UpdatePeerConnectionStatus(peer1.Id, true);
        manager.UpdatePeerConnectionStatus(peer2.Id, true);

        // Act
        var connected = manager.ConnectedPeers;

        // Assert
        Assert.Equal(2, connected.Count);
        Assert.Contains(peer1, connected);
        Assert.Contains(peer2, connected);
    }
}
