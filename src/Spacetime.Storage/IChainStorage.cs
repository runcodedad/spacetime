namespace Spacetime.Storage;

/// <summary>
/// Main interface for blockchain storage operations.
/// </summary>
/// <remarks>
/// Provides atomic operations for storing and retrieving blockchain data including
/// blocks, transactions, and account state. All write operations are atomic to ensure
/// consistency of the blockchain state.
/// </remarks>
public interface IChainStorage : IDisposable
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
    void CommitBatch(IWriteBatch batch);

    /// <summary>
    /// Compacts the database to reclaim space and improve performance.
    /// </summary>
    void Compact();

    /// <summary>
    /// Checks database integrity and detects corruption.
    /// </summary>
    /// <returns>True if database is healthy, false if corruption is detected.</returns>
    bool CheckIntegrity();
}
