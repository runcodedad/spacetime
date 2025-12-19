namespace Spacetime.Plotting;

/// <summary>
/// Scans plot leaves in blocks for better CPU cache utilization.
/// </summary>
/// <remarks>
/// This strategy groups leaf reads into cache-aligned blocks, which improves
/// performance on modern CPUs by reducing cache misses. The strategy divides
/// the plot into blocks and samples leaves within each block sequentially.
/// 
/// Benefits:
/// - Better CPU cache locality compared to evenly-spaced sampling
/// - Reduced memory access latency
/// - Improved throughput for medium-sized plots
/// 
/// Best used for:
/// - Medium to large plots (1-100 GB)
/// - Systems with limited CPU cache
/// - When sampling strategy is too sparse
/// </remarks>
public sealed class CacheFriendlyScanStrategy : IScanningStrategy
{
    /// <summary>
    /// Default block size that fits well in L2 cache (typically 256KB per core).
    /// At 32 bytes per leaf, this is 8192 leaves.
    /// </summary>
    public const int DefaultBlockSize = 8192;

    private readonly int _blockSize;
    private readonly int _leavesPerBlock;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheFriendlyScanStrategy"/> class.
    /// </summary>
    /// <param name="blockSize">Number of leaves per block (must be positive)</param>
    /// <param name="leavesPerBlock">Number of leaves to scan within each block (must be positive and <= blockSize)</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public CacheFriendlyScanStrategy(int blockSize = DefaultBlockSize, int leavesPerBlock = DefaultBlockSize)
    {
        if (blockSize <= 0)
        {
            throw new ArgumentException("Block size must be positive", nameof(blockSize));
        }

        if (leavesPerBlock <= 0)
        {
            throw new ArgumentException("Leaves per block must be positive", nameof(leavesPerBlock));
        }

        if (leavesPerBlock > blockSize)
        {
            throw new ArgumentException(
                "Leaves per block cannot exceed block size",
                nameof(leavesPerBlock));
        }

        _blockSize = blockSize;
        _leavesPerBlock = leavesPerBlock;
    }

    /// <inheritdoc/>
    public string Name => $"CacheFriendly(block={_blockSize},scan={_leavesPerBlock})";

    /// <summary>
    /// Gets the configured block size.
    /// </summary>
    public int BlockSize => _blockSize;

    /// <summary>
    /// Gets the configured leaves per block.
    /// </summary>
    public int LeavesPerBlock => _leavesPerBlock;

    /// <inheritdoc/>
    public IEnumerable<long> GetIndicesToScan(long totalLeaves)
    {
        if (totalLeaves <= 0)
        {
            throw new ArgumentException("Total leaves must be positive", nameof(totalLeaves));
        }

        // Calculate number of blocks
        var blockCount = (totalLeaves + _blockSize - 1) / _blockSize;

        for (long blockIndex = 0; blockIndex < blockCount; blockIndex++)
        {
            var blockStart = blockIndex * _blockSize;
            var blockEnd = Math.Min(blockStart + _blockSize, totalLeaves);
            var actualBlockSize = blockEnd - blockStart;

            // Determine how many leaves to scan in this block
            var leavesToScanInBlock = Math.Min(_leavesPerBlock, actualBlockSize);

            // Scan evenly distributed leaves within the block
            if (leavesToScanInBlock >= actualBlockSize)
            {
                // Scan all leaves in the block
                for (long i = blockStart; i < blockEnd; i++)
                {
                    yield return i;
                }
            }
            else
            {
                // Sample evenly within the block
                var step = (double)actualBlockSize / leavesToScanInBlock;
                for (var i = 0; i < leavesToScanInBlock; i++)
                {
                    var index = blockStart + (long)(i * step);
                    yield return index;
                }
            }
        }
    }

    /// <inheritdoc/>
    public long GetScanCount(long totalLeaves)
    {
        if (totalLeaves <= 0)
        {
            throw new ArgumentException("Total leaves must be positive", nameof(totalLeaves));
        }

        var blockCount = (totalLeaves + _blockSize - 1) / _blockSize;
        long totalToScan = 0;

        for (long blockIndex = 0; blockIndex < blockCount; blockIndex++)
        {
            var blockStart = blockIndex * _blockSize;
            var blockEnd = Math.Min(blockStart + _blockSize, totalLeaves);
            var actualBlockSize = blockEnd - blockStart;

            totalToScan += Math.Min(_leavesPerBlock, actualBlockSize);
        }

        return totalToScan;
    }

    /// <summary>
    /// Creates a strategy optimized for L2 cache (typical 256KB-512KB per core).
    /// </summary>
    /// <returns>A cache-friendly strategy for L2 cache</returns>
    public static CacheFriendlyScanStrategy CreateForL2Cache()
    {
        // L2 cache typically 256KB-512KB per core
        // 8192 leaves * 32 bytes = 256KB
        return new CacheFriendlyScanStrategy(blockSize: 8192, leavesPerBlock: 8192);
    }

    /// <summary>
    /// Creates a strategy optimized for L3 cache (typical 2MB-16MB shared).
    /// </summary>
    /// <returns>A cache-friendly strategy for L3 cache</returns>
    public static CacheFriendlyScanStrategy CreateForL3Cache()
    {
        // L3 cache typically 2MB-16MB shared
        // 32768 leaves * 32 bytes = 1MB
        return new CacheFriendlyScanStrategy(blockSize: 32768, leavesPerBlock: 32768);
    }

    /// <summary>
    /// Creates a strategy that samples within cache-friendly blocks.
    /// </summary>
    /// <param name="samplesPerBlock">Number of samples per block</param>
    /// <returns>A sampling strategy with cache-friendly access patterns</returns>
    public static CacheFriendlyScanStrategy CreateSampling(int samplesPerBlock = 1024)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(samplesPerBlock);

        // Use default block size but only scan a subset of leaves per block
        return new CacheFriendlyScanStrategy(
            blockSize: DefaultBlockSize,
            leavesPerBlock: Math.Min(samplesPerBlock, DefaultBlockSize));
    }
}
