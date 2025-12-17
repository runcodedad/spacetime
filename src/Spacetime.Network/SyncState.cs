namespace Spacetime.Network;

/// <summary>
/// Represents the current state of block synchronization.
/// </summary>
public enum SyncState
{
    /// <summary>
    /// Not synchronizing.
    /// </summary>
    Idle,

    /// <summary>
    /// Discovering peers and negotiating protocol.
    /// </summary>
    Discovering,

    /// <summary>
    /// Downloading block headers.
    /// </summary>
    DownloadingHeaders,

    /// <summary>
    /// Downloading block bodies in parallel.
    /// </summary>
    DownloadingBlocks,

    /// <summary>
    /// Validating downloaded blocks.
    /// </summary>
    Validating,

    /// <summary>
    /// Synchronization completed, node is synced.
    /// </summary>
    Synced,

    /// <summary>
    /// Synchronization failed due to error.
    /// </summary>
    Failed,

    /// <summary>
    /// Synchronization was cancelled.
    /// </summary>
    Cancelled
}
