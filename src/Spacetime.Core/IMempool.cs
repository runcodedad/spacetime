namespace Spacetime.Core;

/// <summary>
/// Provides access to the transaction mempool for block building.
/// </summary>
/// <remarks>
/// The mempool holds pending transactions that have been validated but not yet included in a block.
/// Block builders use this interface to collect transactions for new blocks.
/// </remarks>
public interface IMempool
{
    /// <summary>
    /// Gets a list of pending transactions from the mempool.
    /// </summary>
    /// <param name="maxCount">The maximum number of transactions to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of signed transactions ready for inclusion in a block.</returns>
    /// <remarks>
    /// Implementations should:
    /// - Return transactions in priority order (typically by fee)
    /// - Ensure all returned transactions are signed and valid
    /// - Respect the maxCount limit
    /// - Handle cancellation gracefully
    /// </remarks>
    Task<IReadOnlyList<Transaction>> GetPendingTransactionsAsync(
        int maxCount,
        CancellationToken cancellationToken = default);
}
