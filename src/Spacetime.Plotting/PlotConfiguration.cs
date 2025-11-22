namespace Spacetime.Plotting;

/// <summary>
/// Configuration options for plot creation.
/// </summary>
public sealed class PlotConfiguration
{
    /// <summary>
    /// Minimum plot size in bytes (100 MB)
    /// </summary>
    public const long MinPlotSize = 100L * 1024 * 1024;

    /// <summary>
    /// Maximum plot size in bytes (1 TB)
    /// </summary>
    public const long MaxPlotSize = 1024L * 1024 * 1024 * 1024;

    /// <summary>
    /// Gets the target plot size in bytes.
    /// </summary>
    public long PlotSizeBytes { get; }

    /// <summary>
    /// Gets the number of leaves in the plot.
    /// </summary>
    public long LeafCount { get; }

    /// <summary>
    /// Gets the miner's public key.
    /// </summary>
    public byte[] MinerPublicKey { get; }

    /// <summary>
    /// Gets the plot seed for deterministic generation.
    /// </summary>
    public byte[] PlotSeed { get; }

    /// <summary>
    /// Gets the output file path for the plot.
    /// </summary>
    public string OutputPath { get; }

    /// <summary>
    /// Gets whether to include Merkle cache layers.
    /// </summary>
    public bool IncludeCache { get; }

    /// <summary>
    /// Gets the number of top levels to cache (if caching is enabled).
    /// </summary>
    public int CacheLevels { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlotConfiguration"/> class.
    /// </summary>
    public PlotConfiguration(
        long plotSizeBytes,
        byte[] minerPublicKey,
        byte[] plotSeed,
        string outputPath,
        bool includeCache = false,
        int cacheLevels = 5)
    {
        ArgumentNullException.ThrowIfNull(minerPublicKey);
        ArgumentNullException.ThrowIfNull(plotSeed);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        if (plotSizeBytes < MinPlotSize || plotSizeBytes > MaxPlotSize)
            throw new ArgumentException(
                $"Plot size must be between {MinPlotSize:N0} and {MaxPlotSize:N0} bytes",
                nameof(plotSizeBytes));

        if (minerPublicKey.Length != 32)
            throw new ArgumentException("Miner public key must be 32 bytes", nameof(minerPublicKey));

        if (plotSeed.Length != 32)
            throw new ArgumentException("Plot seed must be 32 bytes", nameof(plotSeed));

        if (cacheLevels < 0)
            throw new ArgumentException("Cache levels must be non-negative", nameof(cacheLevels));

        PlotSizeBytes = plotSizeBytes;
        MinerPublicKey = minerPublicKey;
        PlotSeed = plotSeed;
        OutputPath = outputPath;
        IncludeCache = includeCache;
        CacheLevels = cacheLevels;

        // Calculate leaf count based on plot size
        // Each leaf is 32 bytes
        LeafCount = plotSizeBytes / LeafGenerator.LeafSize;
    }

    /// <summary>
    /// Creates a configuration for a 1 GB plot.
    /// </summary>
    public static PlotConfiguration Create1GB(byte[] minerPublicKey, byte[] plotSeed, string outputPath, bool includeCache = false)
    {
        return new PlotConfiguration(1024L * 1024 * 1024, minerPublicKey, plotSeed, outputPath, includeCache);
    }

    /// <summary>
    /// Creates a configuration for a 10 GB plot.
    /// </summary>
    public static PlotConfiguration Create10GB(byte[] minerPublicKey, byte[] plotSeed, string outputPath, bool includeCache = false)
    {
        return new PlotConfiguration(10L * 1024 * 1024 * 1024, minerPublicKey, plotSeed, outputPath, includeCache);
    }

    /// <summary>
    /// Creates a configuration for a 100 GB plot.
    /// </summary>
    public static PlotConfiguration Create100GB(byte[] minerPublicKey, byte[] plotSeed, string outputPath, bool includeCache = false)
    {
        return new PlotConfiguration(100L * 1024 * 1024 * 1024, minerPublicKey, plotSeed, outputPath, includeCache);
    }
}
