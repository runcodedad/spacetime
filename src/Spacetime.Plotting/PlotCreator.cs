using MerkleTree.Cache;
using MerkleTree.Core;
using MerkleTree.Hashing;

namespace Spacetime.Plotting;

/// <summary>
/// Creates plot files with deterministic leaf values and Merkle tree structure.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PlotCreator"/> class with a custom hash function.
/// </remarks>
public sealed class PlotCreator(IHashFunction hashFunction)
{
    private readonly IHashFunction _hashFunction = hashFunction ?? throw new ArgumentNullException(nameof(hashFunction));

    /// <summary>
    /// Initializes a new instance of the <see cref="PlotCreator"/> class.
    /// </summary>
    public PlotCreator()
        : this(new Sha256HashFunction())
    {
    }

    /// <summary>
    /// Creates a plot file asynchronously.
    /// </summary>
    /// <param name="config">The plot configuration</param>
    /// <param name="progress">Optional progress reporter (reports percentage 0-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created plot header</returns>
    public async Task<PlotHeader> CreatePlotAsync(
        PlotConfiguration config,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        // Ensure output directory exists
        var directory = Path.GetDirectoryName(config.OutputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Build Merkle tree using streaming construction
        var merkleTreeStream = new MerkleTreeStream(_hashFunction);

        CacheConfiguration? cacheConfig = null;
        if (config.IncludeCache)
        {
            var cacheFilePath = $"{config.OutputPath}.cache";
            cacheConfig = new CacheConfiguration(
                filePath: cacheFilePath,
                topLevelsToCache: config.CacheLevels);
        }

        // Generate leaves asynchronously with progress tracking
        var progressTracker = progress != null ? new ProgressReporter(config.LeafCount, progress) : null;
        Action? onLeafGenerated = progressTracker != null ? progressTracker.ReportLeafProcessed : null;
        
        var leaves = LeafGenerator.GenerateLeavesAsync(
            config.MinerPublicKey,
            config.PlotSeed,
            startNonce: 0,
            config.LeafCount,
            onLeafGenerated,
            cancellationToken);

        var metadata = await merkleTreeStream.BuildAsync(leaves, cacheConfig, cancellationToken);

        // Write plot file
        var header = await WritePlotFileAsync(
            config.OutputPath,
            config.MinerPublicKey,
            config.PlotSeed,
            config.LeafCount,
            metadata,
            cancellationToken);

        return header;
    }

    /// <summary>
    /// Writes the plot file with header and leaves.
    /// </summary>
    private static async Task<PlotHeader> WritePlotFileAsync(
        string outputPath,
        byte[] minerPublicKey,
        byte[] plotSeed,
        long leafCount,
        MerkleTreeMetadata metadata,
        CancellationToken cancellationToken)
    {
        using var fileStream = new FileStream(
            outputPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        // Create header
        var header = new PlotHeader(
            plotSeed,
            leafCount,
            LeafGenerator.LeafSize,
            metadata.Height,
            metadata.RootHash);

        header.ComputeChecksum();

        // Write header
        var headerBytes = header.Serialize();
        await fileStream.WriteAsync(headerBytes, cancellationToken);

        // Write leaves
        var leaves = LeafGenerator.GenerateLeavesAsync(
            minerPublicKey,
            plotSeed,
            startNonce: 0,
            leafCount,
            onLeafGenerated: null,
            cancellationToken);

        await foreach (var leaf in leaves.WithCancellation(cancellationToken))
        {
            await fileStream.WriteAsync(leaf, cancellationToken);
        }

        await fileStream.FlushAsync(cancellationToken);

        return header;
    }

    /// <summary>
    /// Helper class for reporting progress.
    /// </summary>
    private sealed class ProgressReporter(long totalLeaves, IProgress<double> progress)
    {
        private long _processedLeaves;
        private int _lastReportedPercentage = -1;

        public void ReportLeafProcessed()
        {
            var processed = Interlocked.Increment(ref _processedLeaves);
            var percentage = (int)(processed * 100.0 / totalLeaves);

            // Only report when percentage changes to reduce overhead
            if (percentage != _lastReportedPercentage)
            {
                _lastReportedPercentage = percentage;
                progress.Report(percentage);
            }
        }
    }
}
