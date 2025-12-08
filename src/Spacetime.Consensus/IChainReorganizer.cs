using Spacetime.Core;

namespace Spacetime.Consensus;

/// <summary>
/// Interface for chain reorganization operations.
/// </summary>
/// <remarks>
/// The chain reorganizer handles switching from one blockchain to another when
/// an alternative chain with higher cumulative difficulty is discovered.
/// </remarks>
public interface IChainReorganizer
{
    /// <summary>
    /// Occurs when a chain reorganization is performed.
    /// </summary>
    event EventHandler<ChainReorgEvent>? ChainReorganized;

    /// <summary>
    /// Attempts to reorganize the chain if the provided alternative chain has higher cumulative difficulty.
    /// </summary>
    /// <param name="alternativeChainTip">The tip block of the alternative chain.</param>
    /// <param name="alternativeChainBlocks">The blocks in the alternative chain, ordered from oldest to newest.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains:
    /// - True if a reorganization was performed.
    /// - False if the alternative chain does not have higher cumulative difficulty.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when reorganization fails.</exception>
    /// <remarks>
    /// This method:
    /// 1. Validates that the alternative chain has higher cumulative difficulty
    /// 2. Finds the fork point between current and alternative chains
    /// 3. Reverts blocks from current chain back to fork point
    /// 4. Applies blocks from alternative chain
    /// 5. Returns orphaned transactions to the mempool
    /// 6. Emits a reorg event for monitoring
    /// </remarks>
    Task<bool> TryReorganizeAsync(
        Block alternativeChainTip,
        IReadOnlyList<Block> alternativeChainBlocks,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the cumulative difficulty for a chain up to the specified block.
    /// </summary>
    /// <param name="blockHash">The hash of the block to compute cumulative difficulty for.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The cumulative difficulty of the chain up to and including the specified block.</returns>
    /// <exception cref="ArgumentException">Thrown when the block hash is invalid.</exception>
    Task<long> GetCumulativeDifficultyAsync(
        ReadOnlyMemory<byte> blockHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the common ancestor (fork point) between the current chain and an alternative chain.
    /// </summary>
    /// <param name="alternativeChainBlocks">The blocks in the alternative chain.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The height of the fork point, or -1 if no common ancestor exists.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when alternativeChainBlocks is null.</exception>
    Task<long> FindForkPointAsync(
        IReadOnlyList<Block> alternativeChainBlocks,
        CancellationToken cancellationToken = default);
}
