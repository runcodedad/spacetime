namespace Spacetime.Consensus;

/// <summary>
/// Configuration for the transaction mempool.
/// </summary>
/// <remarks>
/// These configuration values determine the size limits and eviction policies for the mempool.
/// Values should be set according to system resources and network conditions.
/// </remarks>
public sealed record MempoolConfig
{
    /// <summary>
    /// Gets the maximum number of transactions allowed in the mempool.
    /// </summary>
    /// <remarks>
    /// When the mempool is full and a new higher-fee transaction arrives,
    /// the lowest-fee transaction will be evicted.
    /// Default is 10,000 transactions.
    /// </remarks>
    public int MaxTransactions { get; init; } = 10_000;

    /// <summary>
    /// Gets the maximum number of transactions to return when building a block.
    /// </summary>
    /// <remarks>
    /// This limits the number of transactions that can be included in a single block.
    /// Should typically match or be less than the consensus MaxTransactionsPerBlock.
    /// Default is 10,000 transactions.
    /// </remarks>
    public int MaxTransactionsPerBlock { get; init; } = 10_000;

    /// <summary>
    /// Gets the minimum fee for a transaction to be accepted into the mempool.
    /// </summary>
    /// <remarks>
    /// Transactions with fees below this value will be rejected.
    /// This helps prevent spam and ensures the mempool contains valuable transactions.
    /// Default is 1 to allow most transactions.
    /// </remarks>
    public long MinimumFee { get; init; } = 1;

    /// <summary>
    /// Creates a configuration with default values for production use.
    /// </summary>
    public static MempoolConfig Default => new();

    /// <summary>
    /// Creates a configuration with permissive values for testing.
    /// </summary>
    public static MempoolConfig Permissive => new()
    {
        MaxTransactions = int.MaxValue,
        MaxTransactionsPerBlock = int.MaxValue,
        MinimumFee = 0
    };
}
