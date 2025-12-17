namespace Spacetime.Network;

/// <summary>
/// Defines the interface for block synchronization functionality.
/// </summary>
/// <remarks>
/// The block synchronizer handles initial blockchain download (IBD) and ongoing
/// synchronization with the network. It uses header-first synchronization for
/// efficiency and supports parallel block downloads.
/// </remarks>
public interface IBlockSynchronizer : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the node is currently synchronizing.
    /// </summary>
    bool IsSynchronizing { get; }

    /// <summary>
    /// Gets a value indicating whether the node is in initial blockchain download (IBD) mode.
    /// </summary>
    bool IsInitialBlockDownload { get; }

    /// <summary>
    /// Gets the current synchronization progress.
    /// </summary>
    SyncProgress Progress { get; }

    /// <summary>
    /// Starts the synchronization process.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the synchronization process.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes synchronization after an interruption.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResumeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when synchronization progress is updated.
    /// </summary>
    event EventHandler<SyncProgress>? ProgressUpdated;
}
