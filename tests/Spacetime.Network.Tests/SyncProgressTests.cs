namespace Spacetime.Network.Tests;

public class SyncProgressTests
{
    [Fact]
    public void Constructor_InitializesAllProperties()
    {
        // Arrange
        var currentHeight = 100L;
        var targetHeight = 1000L;
        var blocksDownloaded = 50L;
        var blocksValidated = 45L;
        var bytesDownloaded = 5000000L;
        var downloadRate = 100000.0;
        var startTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        var currentTime = DateTimeOffset.UtcNow;
        var state = SyncState.DownloadingBlocks;

        // Act
        var progress = new SyncProgress(
            currentHeight,
            targetHeight,
            blocksDownloaded,
            blocksValidated,
            bytesDownloaded,
            downloadRate,
            startTime,
            currentTime,
            state);

        // Assert
        Assert.Equal(currentHeight, progress.CurrentHeight);
        Assert.Equal(targetHeight, progress.TargetHeight);
        Assert.Equal(blocksDownloaded, progress.BlocksDownloaded);
        Assert.Equal(blocksValidated, progress.BlocksValidated);
        Assert.Equal(bytesDownloaded, progress.BytesDownloaded);
        Assert.Equal(downloadRate, progress.DownloadRate);
        Assert.Equal(startTime, progress.StartTime);
        Assert.Equal(currentTime, progress.CurrentTime);
        Assert.Equal(state, progress.State);
    }

    [Fact]
    public void PercentComplete_CalculatesCorrectly()
    {
        // Arrange
        var progress = new SyncProgress(
            100,
            1000,
            0,
            0,
            0,
            0,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            SyncState.DownloadingBlocks);

        // Act
        var percent = progress.PercentComplete;

        // Assert
        Assert.Equal(10.0, percent);
    }

    [Fact]
    public void PercentComplete_WhenTargetIsZero_ReturnsZero()
    {
        // Arrange
        var progress = new SyncProgress(
            0,
            0,
            0,
            0,
            0,
            0,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            SyncState.Idle);

        // Act
        var percent = progress.PercentComplete;

        // Assert
        Assert.Equal(0.0, percent);
    }

    [Fact]
    public void PercentComplete_WhenComplete_ReturnsOneHundred()
    {
        // Arrange
        var progress = new SyncProgress(
            1000,
            1000,
            0,
            0,
            0,
            0,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            SyncState.Synced);

        // Act
        var percent = progress.PercentComplete;

        // Assert
        Assert.Equal(100.0, percent);
    }

    [Fact]
    public void PercentComplete_NeverExceedsOneHundred()
    {
        // Arrange
        var progress = new SyncProgress(
            1500,
            1000,
            0,
            0,
            0,
            0,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            SyncState.Synced);

        // Act
        var percent = progress.PercentComplete;

        // Assert
        Assert.Equal(100.0, percent);
    }

    [Fact]
    public void EstimatedTimeRemaining_WithValidData_CalculatesCorrectly()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddMinutes(-1);
        var currentTime = DateTimeOffset.UtcNow;
        var progress = new SyncProgress(
            100, // Downloaded 100 blocks in 1 minute
            1000, // Need to download 900 more
            0,
            0,
            0,
            10000.0,
            startTime,
            currentTime,
            SyncState.DownloadingBlocks);

        // Act
        var estimatedTime = progress.EstimatedTimeRemaining;

        // Assert
        Assert.NotNull(estimatedTime);
        // Should estimate roughly 9 minutes for remaining 900 blocks
        Assert.True(estimatedTime.Value.TotalMinutes > 8);
        Assert.True(estimatedTime.Value.TotalMinutes < 10);
    }

    [Fact]
    public void EstimatedTimeRemaining_WhenDownloadRateIsZero_ReturnsNull()
    {
        // Arrange
        var progress = new SyncProgress(
            100,
            1000,
            0,
            0,
            0,
            0.0, // No download rate
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            SyncState.DownloadingBlocks);

        // Act
        var estimatedTime = progress.EstimatedTimeRemaining;

        // Assert
        Assert.Null(estimatedTime);
    }

    [Fact]
    public void EstimatedTimeRemaining_WhenSynced_ReturnsNull()
    {
        // Arrange
        var progress = new SyncProgress(
            1000,
            1000,
            0,
            0,
            0,
            10000.0,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            SyncState.Synced);

        // Act
        var estimatedTime = progress.EstimatedTimeRemaining;

        // Assert
        Assert.Null(estimatedTime);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var progress = new SyncProgress(
            100,
            1000,
            50,
            45,
            5000000,
            100000.0,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow,
            SyncState.DownloadingBlocks);

        // Act
        var result = progress.ToString();

        // Assert
        Assert.Contains("100/1000", result);
        Assert.Contains("10.00%", result);
        Assert.Contains("DownloadingBlocks", result);
        Assert.Contains("100000", result);
    }
}
