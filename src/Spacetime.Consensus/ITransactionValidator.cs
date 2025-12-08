using Spacetime.Core;

namespace Spacetime.Consensus;

/// <summary>
/// Validates transactions according to consensus rules.
/// </summary>
/// <remarks>
/// The transaction validator performs comprehensive validation including:
/// - Signature verification
/// - Balance and nonce checks
/// - Fee validation
/// - Size limits
/// - Duplicate detection
/// - State consistency
/// 
/// Validation is performed in order of computational cost, with cheaper checks first
/// to fail fast on invalid transactions.
/// </remarks>
public interface ITransactionValidator
{
    /// <summary>
    /// Validates a single transaction against current blockchain state.
    /// </summary>
    /// <param name="transaction">The transaction to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A detailed validation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when transaction is null.</exception>
    Task<TransactionValidationResult> ValidateTransactionAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a transaction in the context of a block being validated.
    /// </summary>
    /// <param name="transaction">The transaction to validate.</param>
    /// <param name="blockContext">Context information about the block containing this transaction.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A detailed validation result.</returns>
    /// <remarks>
    /// This method is used when validating transactions as part of block validation.
    /// It may use cached or tracked state within the block to optimize validation
    /// and detect double-spending within the same block.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when transaction or blockContext is null.</exception>
    Task<TransactionValidationResult> ValidateTransactionInBlockAsync(
        Transaction transaction,
        BlockValidationContext blockContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates multiple transactions as they would appear in a block.
    /// </summary>
    /// <param name="transactions">The transactions to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of validation results, one for each transaction in order.</returns>
    /// <remarks>
    /// This method validates transactions in sequence, tracking account state changes
    /// to detect double-spending and ensure nonce consistency within the transaction set.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when transactions is null.</exception>
    Task<IReadOnlyList<TransactionValidationResult>> ValidateTransactionsAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides context information for validating transactions within a block.
/// </summary>
/// <remarks>
/// This context allows the validator to track state changes within a block
/// and optimize validation by caching account states.
/// </remarks>
public sealed class BlockValidationContext
{
    private readonly Dictionary<byte[], (long balance, long nonce)> _accountStates;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockValidationContext"/> class.
    /// </summary>
    public BlockValidationContext()
    {
        _accountStates = new Dictionary<byte[], (long balance, long nonce)>(
            ByteArrayEqualityComparer.Instance);
    }

    /// <summary>
    /// Gets the tracked account state for an address, if any.
    /// </summary>
    /// <param name="address">The account address.</param>
    /// <returns>The tracked balance and nonce, or null if not tracked.</returns>
    public (long balance, long nonce)? GetTrackedAccountState(ReadOnlySpan<byte> address)
    {
        if (_accountStates.TryGetValue(address.ToArray(), out var state))
        {
            return state;
        }
        return null;
    }

    /// <summary>
    /// Updates the tracked account state for an address.
    /// </summary>
    /// <param name="address">The account address.</param>
    /// <param name="balance">The new balance.</param>
    /// <param name="nonce">The new nonce.</param>
    public void UpdateAccountState(ReadOnlySpan<byte> address, long balance, long nonce)
    {
        _accountStates[address.ToArray()] = (balance, nonce);
    }
}
