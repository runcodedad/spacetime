namespace Spacetime.Storage;

/// <summary>
/// Main interface for blockchain storage operations.
/// </summary>
/// <remarks>
/// Provides atomic operations for storing and retrieving blockchain data including
/// blocks, transactions, and account state. All write operations are atomic to ensure
/// consistency of the blockchain state.
/// </remarks>
public interface IChainStorage : IAsyncDisposable
{
    /// <summary>
    /// Gets the block storage interface.
    /// </summary>
    IBlockStorage Blocks { get; }

    /// <summary>
    /// Gets the transaction index interface.
    /// </summary>
    ITransactionIndex Transactions { get; }

    /// <summary>
    /// Gets the account storage interface.
    /// </summary>
    IAccountStorage Accounts { get; }

    /// <summary>
    /// Gets the chain metadata storage interface.
    /// </summary>
    IChainMetadata Metadata { get; }

    /// <summary>
    /// Creates a new atomic write batch.
    /// </summary>
    /// <returns>A new write batch for atomic operations.</returns>
    IWriteBatch CreateWriteBatch();

    /// <summary>
    /// Commits a write batch atomically.
    /// </summary>
    /// <param name="batch">The batch to commit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CommitBatchAsync(IWriteBatch batch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compacts the database to reclaim space and improve performance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CompactAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks database integrity and detects corruption.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if database is healthy, false if corruption is detected.</returns>
    Task<bool> CheckIntegrityAsync(CancellationToken cancellationToken = default);
}
