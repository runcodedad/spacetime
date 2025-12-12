namespace Spacetime.Core;

/// <summary>
/// Provides access to the transaction mempool for block building and transaction management.
/// </summary>
/// <remarks>
/// The mempool holds pending transactions that have been validated but not yet included in a block.
/// Block builders use this interface to collect transactions for new blocks.
/// Transactions are stored in priority order by fee and evicted when the pool reaches capacity.
/// </remarks>
public interface IMempool
{
    /// <summary>
    /// Gets the current number of transactions in the mempool.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Adds a transaction to the mempool.
    /// </summary>
    /// <param name="transaction">The transaction to add.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the transaction was added; false if it already exists or was rejected.</returns>
    /// <remarks>
    /// The transaction should be validated before calling this method.
    /// If the mempool is full, the lowest-fee transaction may be evicted.
    /// Duplicate transactions (same hash) are not added.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when transaction is null.</exception>
    bool AddTransaction(
        Transaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes transactions from the mempool.
    /// </summary>
    /// <param name="transactionHashes">The hashes of transactions to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of transactions that were removed.</returns>
    /// <remarks>
    /// This is typically called after transactions are included in a block.
    /// Transactions that don't exist in the mempool are silently ignored.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when transactionHashes is null.</exception>
    int RemoveTransactions(
        IReadOnlyList<byte[]> transactionHashes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a transaction exists in the mempool.
    /// </summary>
    /// <param name="transactionHash">The hash of the transaction to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the transaction exists in the mempool; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when transactionHash is null.</exception>
    bool ContainsTransaction(
        byte[] transactionHash,
        CancellationToken cancellationToken = default);

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
    IReadOnlyList<Transaction> GetPendingTransactions(
        int maxCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all transactions from the mempool.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <remarks>
    /// This is typically used for testing or during chain reorganization.
    /// </remarks>
    void Clear(CancellationToken cancellationToken = default);
}
