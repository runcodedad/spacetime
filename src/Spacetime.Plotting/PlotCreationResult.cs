namespace Spacetime.Plotting;

/// <summary>
/// Result of plot creation operation.
/// </summary>
public sealed class PlotCreationResult
{
    /// <summary>
    /// Gets the plot header containing metadata about the created plot.
    /// </summary>
    public PlotHeader Header { get; }

    /// <summary>
    /// Gets the path to the cache file if caching was enabled, otherwise null.
    /// </summary>
    public string? CacheFilePath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlotCreationResult"/> class.
    /// </summary>
    /// <param name="header">The plot header</param>
    /// <param name="cacheFilePath">Optional path to the cache file</param>
    public PlotCreationResult(PlotHeader header, string? cacheFilePath = null)
    {
        ArgumentNullException.ThrowIfNull(header);
        Header = header;
        CacheFilePath = cacheFilePath;
    }
}
