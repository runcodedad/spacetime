namespace Spacetime.Network;

/// <summary>
/// Represents the current synchronization progress.
/// </summary>
public sealed class SyncProgress
{
    /// <summary>
    /// Gets the current height of the local chain.
    /// </summary>
    public long CurrentHeight { get; }

    /// <summary>
    /// Gets the target height (best known height from peers).
    /// </summary>
    public long TargetHeight { get; }

    /// <summary>
    /// Gets the number of blocks downloaded.
    /// </summary>
    public long BlocksDownloaded { get; }

    /// <summary>
    /// Gets the number of blocks validated.
    /// </summary>
    public long BlocksValidated { get; }

    /// <summary>
    /// Gets the total number of bytes downloaded.
    /// </summary>
    public long BytesDownloaded { get; }

    /// <summary>
    /// Gets the current download rate in bytes per second.
    /// </summary>
    public double DownloadRate { get; }

    /// <summary>
    /// Gets the timestamp when synchronization started.
    /// </summary>
    public DateTimeOffset StartTime { get; }

    /// <summary>
    /// Gets the current timestamp.
    /// </summary>
    public DateTimeOffset CurrentTime { get; }

    /// <summary>
    /// Gets the synchronization state.
    /// </summary>
    public SyncState State { get; }

    /// <summary>
    /// Gets the percentage complete (0-100).
    /// </summary>
    public double PercentComplete
    {
        get
        {
            if (TargetHeight == 0) return 0;
            return Math.Min(100.0, (double)CurrentHeight / TargetHeight * 100.0);
        }
    }

    /// <summary>
    /// Gets the estimated time remaining.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining
    {
        get
        {
            if (DownloadRate <= 0 || CurrentHeight >= TargetHeight)
                return null;

            var blocksRemaining = TargetHeight - CurrentHeight;
            var elapsed = CurrentTime - StartTime;
            if (elapsed.TotalSeconds <= 0) return null;

            var blocksPerSecond = CurrentHeight / elapsed.TotalSeconds;
            if (blocksPerSecond <= 0) return null;

            var secondsRemaining = blocksRemaining / blocksPerSecond;
            return TimeSpan.FromSeconds(secondsRemaining);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncProgress"/> class.
    /// </summary>
    /// <param name="currentHeight">The current local chain height.</param>
    /// <param name="targetHeight">The target height from peers.</param>
    /// <param name="blocksDownloaded">The number of blocks downloaded.</param>
    /// <param name="blocksValidated">The number of blocks validated.</param>
    /// <param name="bytesDownloaded">The total bytes downloaded.</param>
    /// <param name="downloadRate">The current download rate.</param>
    /// <param name="startTime">The synchronization start time.</param>
    /// <param name="currentTime">The current time.</param>
    /// <param name="state">The synchronization state.</param>
    public SyncProgress(
        long currentHeight,
        long targetHeight,
        long blocksDownloaded,
        long blocksValidated,
        long bytesDownloaded,
        double downloadRate,
        DateTimeOffset startTime,
        DateTimeOffset currentTime,
        SyncState state)
    {
        CurrentHeight = currentHeight;
        TargetHeight = targetHeight;
        BlocksDownloaded = blocksDownloaded;
        BlocksValidated = blocksValidated;
        BytesDownloaded = bytesDownloaded;
        DownloadRate = downloadRate;
        StartTime = startTime;
        CurrentTime = currentTime;
        State = state;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Sync: {CurrentHeight}/{TargetHeight} ({PercentComplete:F2}%) - {State} - {DownloadRate:F0} B/s";
    }
}
