namespace Spacetime.Consensus;

/// <summary>
/// Configuration for transaction validation rules.
/// </summary>
/// <remarks>
/// These configuration values determine the limits and requirements for valid transactions.
/// Values should be set according to network consensus rules.
/// </remarks>
public sealed record TransactionValidationConfig
{
    /// <summary>
    /// Gets the minimum allowed transaction fee.
    /// </summary>
    /// <remarks>
    /// Transactions with fees below this value will be rejected.
    /// Default is 1 to prevent spam while allowing micropayments.
    /// Set to 0 to allow zero-fee transactions.
    /// </remarks>
    public long MinimumFee { get; init; } = 1;

    /// <summary>
    /// Gets the maximum allowed transaction fee.
    /// </summary>
    /// <remarks>
    /// Transactions with fees above this value will be rejected.
    /// This prevents accidental overpayment due to input errors.
    /// Default is 1,000,000 units.
    /// </remarks>
    public long MaximumFee { get; init; } = 1_000_000;

    /// <summary>
    /// Gets the maximum number of transactions allowed in a single block.
    /// </summary>
    /// <remarks>
    /// This limits block validation time and prevents DoS attacks.
    /// Default is 10,000 transactions per block.
    /// </remarks>
    public int MaxTransactionsPerBlock { get; init; } = 10_000;

    /// <summary>
    /// Gets a value indicating whether to check for duplicate transactions in the index.
    /// </summary>
    /// <remarks>
    /// When enabled, validation will check if a transaction with the same hash
    /// already exists in the transaction index. This prevents replay attacks
    /// across blocks but requires access to the transaction index.
    /// Default is true.
    /// </remarks>
    public bool CheckDuplicateTransactions { get; init; } = true;

    /// <summary>
    /// Creates a configuration with default values for production use.
    /// </summary>
    public static TransactionValidationConfig Default => new();

    /// <summary>
    /// Creates a configuration with permissive values for testing.
    /// </summary>
    public static TransactionValidationConfig Permissive => new()
    {
        MinimumFee = 0,
        MaximumFee = long.MaxValue,
        MaxTransactionsPerBlock = int.MaxValue,
        CheckDuplicateTransactions = false
    };
}
