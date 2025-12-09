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
