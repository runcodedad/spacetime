namespace Spacetime.Network.Tests;

public class BandwidthMonitorTests
{
    [Fact]
    public void CanSend_WithinLimits_ReturnsTrue()
    {
        // Arrange
        var monitor = new BandwidthMonitor(maxBytesPerSecondPerPeer: 1000, maxTotalBytesPerSecond: 5000);

        // Act
        var result = monitor.CanSend("peer1", 500);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanSend_ExceedingPeerLimit_ReturnsFalse()
    {
        // Arrange
        var monitor = new BandwidthMonitor(maxBytesPerSecondPerPeer: 1000, maxTotalBytesPerSecond: 5000);
        monitor.RecordSent("peer1", 800);

        // Act
        var result = monitor.CanSend("peer1", 300);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanSend_ExceedingTotalLimit_ReturnsFalse()
    {
        // Arrange
        var monitor = new BandwidthMonitor(maxBytesPerSecondPerPeer: 2000, maxTotalBytesPerSecond: 3000);
        monitor.RecordSent("peer1", 1500);
        monitor.RecordSent("peer2", 1000);

        // Act - Trying to send 600 more bytes would exceed total limit of 3000
        var result = monitor.CanSend("peer3", 600);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanSend_WithNullPeerId_ThrowsArgumentNullException()
    {
        // Arrange
        var monitor = new BandwidthMonitor();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => monitor.CanSend(null!, 100));
    }

    [Fact]
    public void RecordSent_UpdatesPeerStats()
    {
        // Arrange
        var monitor = new BandwidthMonitor();

        // Act
        monitor.RecordSent("peer1", 500);
        monitor.RecordSent("peer1", 300);

        // Assert
        var stats = monitor.GetPeerStats("peer1");
        Assert.NotNull(stats);
        Assert.Equal(800, stats.TotalBytes);
        Assert.Equal(800, stats.BytesThisSecond);
    }

    [Fact]
    public void RecordSent_WithNullPeerId_ThrowsArgumentNullException()
    {
        // Arrange
        var monitor = new BandwidthMonitor();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => monitor.RecordSent(null!, 100));
    }

    [Fact]
    public void TotalBytesThisSecond_ReturnsCorrectSum()
    {
        // Arrange
        var monitor = new BandwidthMonitor();

        // Act
        monitor.RecordSent("peer1", 500);
        monitor.RecordSent("peer2", 300);

        // Assert
        Assert.Equal(800, monitor.TotalBytesThisSecond);
    }

    [Fact]
    public void TotalBytesSent_AccumulatesAcrossPeers()
    {
        // Arrange
        var monitor = new BandwidthMonitor();

        // Act
        monitor.RecordSent("peer1", 500);
        monitor.RecordSent("peer2", 300);
        monitor.RecordSent("peer1", 200);

        // Assert
        Assert.Equal(1000, monitor.TotalBytesSent);
    }

    [Fact]
    public void GetPeerStats_ForUnknownPeer_ReturnsNull()
    {
        // Arrange
        var monitor = new BandwidthMonitor();

        // Act
        var stats = monitor.GetPeerStats("unknown");

        // Assert
        Assert.Null(stats);
    }

    [Fact]
    public void GetPeerStats_WithNullPeerId_ThrowsArgumentNullException()
    {
        // Arrange
        var monitor = new BandwidthMonitor();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => monitor.GetPeerStats(null!));
    }

    [Fact]
    public void RemovePeer_RemovesPeerTracking()
    {
        // Arrange
        var monitor = new BandwidthMonitor();
        monitor.RecordSent("peer1", 500);

        // Act
        monitor.RemovePeer("peer1");

        // Assert
        var stats = monitor.GetPeerStats("peer1");
        Assert.Null(stats);
    }

    [Fact]
    public void RemovePeer_WithNullPeerId_ThrowsArgumentNullException()
    {
        // Arrange
        var monitor = new BandwidthMonitor();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => monitor.RemovePeer(null!));
    }

    [Fact]
    public void CanSend_AfterSecondReset_AllowsNewQuota()
    {
        // Arrange
        var monitor = new BandwidthMonitor(maxBytesPerSecondPerPeer: 1000, maxTotalBytesPerSecond: 5000);
        monitor.RecordSent("peer1", 1000);
        Assert.False(monitor.CanSend("peer1", 100));

        // Act - Wait for second to roll over
        Thread.Sleep(1100);
        var result = monitor.CanSend("peer1", 100);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RecordSent_MultiplePeers_TracksSeparately()
    {
        // Arrange
        var monitor = new BandwidthMonitor();

        // Act
        monitor.RecordSent("peer1", 500);
        monitor.RecordSent("peer2", 300);

        // Assert
        var stats1 = monitor.GetPeerStats("peer1");
        var stats2 = monitor.GetPeerStats("peer2");
        Assert.NotNull(stats1);
        Assert.NotNull(stats2);
        Assert.Equal(500, stats1.TotalBytes);
        Assert.Equal(300, stats2.TotalBytes);
    }
}
