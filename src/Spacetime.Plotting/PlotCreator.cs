using MerkleTree.Cache;
using MerkleTree.Core;
using MerkleTree.Hashing;
using Spacetime.Common;

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

        // Open file stream for writing
        using var fileStream = new FileStream(
            config.OutputPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        // Reserve space for header (we'll write it at the end)
        await fileStream.WriteAsync(new byte[PlotHeader.TotalHeaderSize], cancellationToken);

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
        Action? onLeafGenerated = progressTracker != null ? progressTracker.ReportItemProcessed : null;
        
        // Generate leaves once, write to file AND build Merkle tree
        var leaves = LeafGenerator.GenerateLeavesAsync(
            config.MinerPublicKey,
            config.PlotSeed,
            startNonce: 0,
            config.LeafCount,
            onLeafGenerated,
            cancellationToken);

        var leavesWithWrite = WriteLeavesAsync(leaves, fileStream, cancellationToken);
        var metadata = await merkleTreeStream.BuildAsync(leavesWithWrite, cacheConfig, cancellationToken);

        await fileStream.FlushAsync(cancellationToken);

        // Now write the header at the beginning
        var header = new PlotHeader(
            config.PlotSeed,
            config.LeafCount,
            LeafGenerator.LeafSize,
            metadata.Height,
            metadata.RootHash);

        header.ComputeChecksum();

        fileStream.Seek(0, SeekOrigin.Begin);
        var headerBytes = header.Serialize();
        await fileStream.WriteAsync(headerBytes, cancellationToken);
        await fileStream.FlushAsync(cancellationToken);

        return header;
    }

    /// <summary>
    /// Writes leaves to the file stream while passing them through for Merkle tree construction.
    /// </summary>
    private static async IAsyncEnumerable<byte[]> WriteLeavesAsync(
        IAsyncEnumerable<byte[]> leaves,
        FileStream fileStream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var leaf in leaves.WithCancellation(cancellationToken))
        {
            await fileStream.WriteAsync(leaf, cancellationToken);
            yield return leaf;
        }
    }
}
