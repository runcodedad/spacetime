namespace Spacetime.Network;

/// <summary>
/// Configuration for block synchronization.
/// </summary>
public sealed class SyncConfig
{
    /// <summary>
    /// Gets the maximum number of peers to use for synchronization.
    /// </summary>
    public int MaxPeers { get; init; } = 8;

    /// <summary>
    /// Gets the number of parallel block downloads.
    /// </summary>
    public int ParallelDownloads { get; init; } = 4;

    /// <summary>
    /// Gets the maximum number of headers to request in a single message.
    /// </summary>
    public int MaxHeadersPerRequest { get; init; } = 2000;

    /// <summary>
    /// Gets the maximum number of retry attempts for failed downloads.
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Gets the timeout for block download requests in seconds.
    /// </summary>
    public int DownloadTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Gets the number of blocks behind to consider as initial block download.
    /// </summary>
    public long IbdThresholdBlocks { get; init; } = 1000;

    /// <summary>
    /// Gets the interval in milliseconds between progress updates.
    /// </summary>
    public int ProgressUpdateIntervalMs { get; init; } = 1000;

    /// <summary>
    /// Gets a value indicating whether to enable bandwidth throttling.
    /// </summary>
    public bool EnableBandwidthThrottling { get; init; } = true;

    /// <summary>
    /// Gets the maximum bandwidth in bytes per second (0 = unlimited).
    /// </summary>
    public long MaxBandwidthBytesPerSecond { get; init; } = 10_485_760; // 10 MB/s
}
