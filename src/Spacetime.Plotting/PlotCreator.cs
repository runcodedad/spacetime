using MerkleTree.Cache;
using MerkleTree.Core;
using MerkleTree.Hashing;

namespace Spacetime.Plotting;

/// <summary>
/// Creates plot files with deterministic leaf values and Merkle tree structure.
/// </summary>
public sealed class PlotCreator
{
    private readonly IHashFunction _hashFunction;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlotCreator"/> class.
    /// </summary>
    public PlotCreator()
        : this(new Sha256HashFunction())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlotCreator"/> class with a custom hash function.
    /// </summary>
    public PlotCreator(IHashFunction hashFunction)
    {
        _hashFunction = hashFunction ?? throw new ArgumentNullException(nameof(hashFunction));
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

        // Generate leaves asynchronously
        var leaves = LeafGenerator.GenerateLeavesAsync(
            config.MinerPublicKey,
            config.PlotSeed,
            startNonce: 0,
            config.LeafCount,
            cancellationToken);

        // Build Merkle tree using streaming construction
        var streamBuilder = new MerkleTreeStream(_hashFunction);

        CacheConfiguration? cacheConfig = null;
        if (config.IncludeCache)
        {
            var cacheFilePath = $"{config.OutputPath}.cache";
            cacheConfig = new CacheConfiguration(
                filePath: cacheFilePath,
                topLevelsToCache: config.CacheLevels);
        }

        // Track progress during tree building
        var progressTracker = progress != null ? new ProgressReporter(config.LeafCount, progress) : null;
        var trackedLeaves = TrackProgress(leaves, progressTracker, cancellationToken);

        var metadata = await streamBuilder.BuildAsync(trackedLeaves, cacheConfig, cancellationToken);

        // Write plot file
        await WritePlotFileAsync(
            config.OutputPath,
            config.MinerPublicKey,
            config.PlotSeed,
            config.LeafCount,
            metadata,
            cancellationToken);

        // Create and return header
        var header = new PlotHeader(
            config.PlotSeed,
            config.LeafCount,
            LeafGenerator.LeafSize,
            metadata.Height,
            metadata.RootHash);

        header.ComputeChecksum();

        return header;
    }

    /// <summary>
    /// Wraps an async enumerable with progress tracking.
    /// </summary>
    private async IAsyncEnumerable<byte[]> TrackProgress(
        IAsyncEnumerable<byte[]> source,
        ProgressReporter? progressReporter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        long count = 0;
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            yield return item;
            count++;
            progressReporter?.ReportLeafProcessed();
        }
    }

    /// <summary>
    /// Writes the plot file with header and leaves.
    /// </summary>
    private async Task WritePlotFileAsync(
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
            cancellationToken);

        await foreach (var leaf in leaves.WithCancellation(cancellationToken))
        {
            await fileStream.WriteAsync(leaf, cancellationToken);
        }

        await fileStream.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Helper class for reporting progress.
    /// </summary>
    private sealed class ProgressReporter
    {
        private readonly long _totalLeaves;
        private readonly IProgress<double> _progress;
        private long _processedLeaves;
        private int _lastReportedPercentage = -1;

        public ProgressReporter(long totalLeaves, IProgress<double> progress)
        {
            _totalLeaves = totalLeaves;
            _progress = progress;
        }

        public void ReportLeafProcessed()
        {
            var processed = Interlocked.Increment(ref _processedLeaves);
            var percentage = (int)((processed * 100.0) / _totalLeaves);

            // Only report when percentage changes to reduce overhead
            if (percentage != _lastReportedPercentage)
            {
                _lastReportedPercentage = percentage;
                _progress.Report(percentage);
            }
        }
    }
}
