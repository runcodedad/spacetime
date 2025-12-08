namespace Spacetime.Consensus;

/// <summary>
/// Represents a chain reorganization event.
/// </summary>
/// <remarks>
/// This event is emitted when the blockchain switches from one chain to another
/// due to the alternative chain having higher cumulative difficulty.
/// </remarks>
public sealed class ChainReorgEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainReorgEvent"/> class.
    /// </summary>
    /// <param name="forkHeight">The height at which the fork occurred.</param>
    /// <param name="oldTipHash">The hash of the old chain tip before reorganization.</param>
    /// <param name="oldTipHeight">The height of the old chain tip.</param>
    /// <param name="newTipHash">The hash of the new chain tip after reorganization.</param>
    /// <param name="newTipHeight">The height of the new chain tip.</param>
    /// <param name="revertedBlockCount">The number of blocks that were reverted.</param>
    /// <param name="appliedBlockCount">The number of blocks that were applied from the new chain.</param>
    /// <exception cref="ArgumentNullException">Thrown when any hash parameter is null.</exception>
    public ChainReorgEvent(
        long forkHeight,
        ReadOnlyMemory<byte> oldTipHash,
        long oldTipHeight,
        ReadOnlyMemory<byte> newTipHash,
        long newTipHeight,
        int revertedBlockCount,
        int appliedBlockCount)
    {
        if (oldTipHash.Length == 0)
        {
            throw new ArgumentException("Old tip hash cannot be empty.", nameof(oldTipHash));
        }

        if (newTipHash.Length == 0)
        {
            throw new ArgumentException("New tip hash cannot be empty.", nameof(newTipHash));
        }

        ForkHeight = forkHeight;
        OldTipHash = oldTipHash;
        OldTipHeight = oldTipHeight;
        NewTipHash = newTipHash;
        NewTipHeight = newTipHeight;
        RevertedBlockCount = revertedBlockCount;
        AppliedBlockCount = appliedBlockCount;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the height at which the fork occurred.
    /// </summary>
    public long ForkHeight { get; }

    /// <summary>
    /// Gets the hash of the old chain tip before reorganization.
    /// </summary>
    public ReadOnlyMemory<byte> OldTipHash { get; }

    /// <summary>
    /// Gets the height of the old chain tip.
    /// </summary>
    public long OldTipHeight { get; }

    /// <summary>
    /// Gets the hash of the new chain tip after reorganization.
    /// </summary>
    public ReadOnlyMemory<byte> NewTipHash { get; }

    /// <summary>
    /// Gets the height of the new chain tip.
    /// </summary>
    public long NewTipHeight { get; }

    /// <summary>
    /// Gets the number of blocks that were reverted from the old chain.
    /// </summary>
    public int RevertedBlockCount { get; }

    /// <summary>
    /// Gets the number of blocks that were applied from the new chain.
    /// </summary>
    public int AppliedBlockCount { get; }

    /// <summary>
    /// Gets the timestamp when the reorganization occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
