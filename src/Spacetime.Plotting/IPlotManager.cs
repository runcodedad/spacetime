namespace Spacetime.Plotting;

/// <summary>
/// Manages multiple plot files, including loading, lifecycle management, and proof generation.
/// </summary>
public interface IPlotManager : IAsyncDisposable
{
    /// <summary>
    /// Gets the collection of loaded plot loaders.
    /// </summary>
    IReadOnlyList<PlotLoader> LoadedPlots { get; }

    /// <summary>
    /// Gets the collection of plot metadata for all registered plots.
    /// </summary>
    IReadOnlyList<PlotMetadata> PlotMetadataCollection { get; }

    /// <summary>
    /// Gets the total number of registered plots (including invalid ones).
    /// </summary>
    int TotalPlotCount { get; }

    /// <summary>
    /// Gets the number of valid, loaded plots ready for proof generation.
    /// </summary>
    int ValidPlotCount { get; }

    /// <summary>
    /// Gets the total disk space allocated by all plots in bytes.
    /// </summary>
    long TotalSpaceAllocatedBytes { get; }

    /// <summary>
    /// Adds a single plot file to the manager.
    /// </summary>
    /// <param name="filePath">Path to the plot file.</param>
    /// <param name="cacheFilePath">Optional path to the cache file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The metadata of the added plot, or null if the plot could not be loaded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty.</exception>
    Task<PlotMetadata?> AddPlotAsync(
        string filePath,
        string? cacheFilePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a plot from the manager by its ID.
    /// </summary>
    /// <param name="plotId">The unique identifier of the plot to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the plot was removed; otherwise, false.</returns>
    Task<bool> RemovePlotAsync(
        Guid plotId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a proof from all loaded plots for the given challenge.
    /// </summary>
    /// <param name="challenge">The 32-byte challenge to generate proof for.</param>
    /// <param name="strategy">The scanning strategy to use.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The best proof found across all plots, or null if no valid proof could be generated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when challenge or strategy is null.</exception>
    /// <exception cref="ArgumentException">Thrown when challenge is not 32 bytes.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no valid plots are loaded.</exception>
    Task<Proof?> GenerateProofAsync(
        byte[] challenge,
        IScanningStrategy strategy,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the current plot metadata to disk.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the status of all plots by checking file existence and validity.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of plots whose status changed.</returns>
    Task<int> RefreshStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for a specific plot by its ID.
    /// </summary>
    /// <param name="plotId">The unique identifier of the plot.</param>
    /// <returns>The plot metadata, or null if not found.</returns>
    PlotMetadata? GetPlotMetadata(Guid plotId);

    /// <summary>
    /// Loads plot metadata from persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LoadMetadataAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Raised when a new plot metadata entry is added to the manager.
    /// </summary>
    event EventHandler<PlotChangedEventArgs>? PlotAdded;

    /// <summary>
    /// Raised when a plot metadata entry is removed from the manager.
    /// </summary>
    event EventHandler<PlotChangedEventArgs>? PlotRemoved;
}
