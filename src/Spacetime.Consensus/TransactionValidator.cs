using Spacetime.Core;
using Spacetime.Storage;

namespace Spacetime.Consensus;

/// <summary>
/// Validates transactions according to consensus rules.
/// </summary>
/// <remarks>
/// This implementation performs comprehensive transaction validation including:
/// - Basic structure validation
/// - Signature verification using ECDSA secp256k1
/// - Balance and nonce checks against account state
/// - Fee validation (minimum and maximum limits)
/// - Transaction size validation
/// - Duplicate transaction detection
/// 
/// All validations are performed in order of computational cost, with cheaper checks
/// first to fail fast on invalid transactions.
/// 
/// <example>
/// Using the transaction validator:
/// <code>
/// var config = TransactionValidationConfig.Default;
/// var validator = new TransactionValidator(
///     signatureVerifier,
///     accountStorage,
///     transactionIndex,
///     config);
/// 
/// var result = await validator.ValidateTransactionAsync(transaction);
/// if (!result.IsValid)
/// {
///     Console.WriteLine($"Validation failed: {result.ErrorMessage}");
/// }
/// </code>
/// </example>
/// </remarks>
public sealed class TransactionValidator : ITransactionValidator
{
    private readonly ISignatureVerifier _signatureVerifier;
    private readonly IAccountStorage _accountStorage;
    private readonly ITransactionIndex? _transactionIndex;
    private readonly TransactionValidationConfig _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionValidator"/> class.
    /// </summary>
    /// <param name="signatureVerifier">The signature verifier for ECDSA verification.</param>
    /// <param name="accountStorage">The account storage for balance and nonce checks.</param>
    /// <param name="transactionIndex">Optional transaction index for duplicate detection.</param>
    /// <param name="config">The validation configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when required arguments are null.</exception>
    public TransactionValidator(
        ISignatureVerifier signatureVerifier,
        IAccountStorage accountStorage,
        ITransactionIndex? transactionIndex,
        TransactionValidationConfig config)
    {
        ArgumentNullException.ThrowIfNull(signatureVerifier);
        ArgumentNullException.ThrowIfNull(accountStorage);
        ArgumentNullException.ThrowIfNull(config);

        _signatureVerifier = signatureVerifier;
        _accountStorage = accountStorage;
        _transactionIndex = transactionIndex;
        _config = config;
    }

    /// <inheritdoc />
    public TransactionValidationResult ValidateTransaction(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Validate basic transaction structure and rules
        var basicResult = ValidateBasicStructure(transaction);
        if (!basicResult.IsValid)
        {
            return basicResult;
        }

        // 2. Validate transaction version
        var versionResult = ValidateVersion(transaction);
        if (!versionResult.IsValid)
        {
            return versionResult;
        }

        // 3. Validate fee limits
        var feeResult = ValidateFee(transaction);
        if (!feeResult.IsValid)
        {
            return feeResult;
        }

        // 4. Validate transaction size
        var sizeResult = ValidateSize(transaction);
        if (!sizeResult.IsValid)
        {
            return sizeResult;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 5. Verify signature (cryptographic operation)
        var signatureResult = ValidateSignature(transaction);
        if (!signatureResult.IsValid)
        {
            return signatureResult;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 6. Check for duplicate transaction
        if (_config.CheckDuplicateTransactions)
        {
            var duplicateResult = ValidateDuplicate(transaction, cancellationToken);
            if (!duplicateResult.IsValid)
            {
                return duplicateResult;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 7. Validate against account state (balance and nonce)
        var stateResult = ValidateAccountState(transaction);
        if (!stateResult.IsValid)
        {
            return stateResult;
        }

        return TransactionValidationResult.Success();
    }

    /// <inheritdoc />
    public TransactionValidationResult ValidateTransactionInBlock(
        Transaction transaction,
        BlockValidationContext blockContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(blockContext);
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Validate basic transaction structure and rules
        var basicResult = ValidateBasicStructure(transaction);
        if (!basicResult.IsValid)
        {
            return basicResult;
        }

        // 2. Validate transaction version
        var versionResult = ValidateVersion(transaction);
        if (!versionResult.IsValid)
        {
            return versionResult;
        }

        // 3. Validate fee limits
        var feeResult = ValidateFee(transaction);
        if (!feeResult.IsValid)
        {
            return feeResult;
        }

        // 4. Validate transaction size
        var sizeResult = ValidateSize(transaction);
        if (!sizeResult.IsValid)
        {
            return sizeResult;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 5. Verify signature (cryptographic operation)
        var signatureResult = ValidateSignature(transaction);
        if (!signatureResult.IsValid)
        {
            return signatureResult;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 6. Check for duplicate transaction
        if (_config.CheckDuplicateTransactions)
        {
            var duplicateResult = ValidateDuplicate(transaction, cancellationToken);
            if (!duplicateResult.IsValid)
            {
                return duplicateResult;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 7. Validate against account state with block context
        var stateResult = ValidateAccountStateInBlock(transaction, blockContext);
        if (!stateResult.IsValid)
        {
            return stateResult;
        }

        // 8. Update block context with new account state
        var trackedState = blockContext.GetTrackedAccountState(transaction.Sender);
        
        AccountState senderAccount;
        if (trackedState.HasValue)
        {
            senderAccount = new AccountState(trackedState.Value.balance, trackedState.Value.nonce);
        }
        else
        {
            var senderKey = transaction.Sender.ToArray();
            senderAccount = _accountStorage.GetAccount(senderKey) ?? new AccountState(0, 0);
        }

        var newBalance = senderAccount.Balance - transaction.Amount - transaction.Fee;
        var newNonce = senderAccount.Nonce + 1;
        blockContext.UpdateAccountState(transaction.Sender, newBalance, newNonce);

        return TransactionValidationResult.Success();
    }

    /// <inheritdoc />
    public IReadOnlyList<TransactionValidationResult> ValidateTransactions(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        cancellationToken.ThrowIfCancellationRequested();

        // Check maximum transactions per block limit
        if (transactions.Count > _config.MaxTransactionsPerBlock)
        {
            var errorResult = TransactionValidationResult.Failure(
                TransactionValidationErrorType.Other,
                $"Block contains {transactions.Count} transactions, exceeds maximum of {_config.MaxTransactionsPerBlock}");
            
            // Return the same error for all transactions
            return Enumerable.Repeat(errorResult, transactions.Count).ToList();
        }

        var results = new List<TransactionValidationResult>(transactions.Count);
        var context = new BlockValidationContext();

        foreach (var tx in transactions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = ValidateTransactionInBlock(tx, context, cancellationToken);
            results.Add(result);

            // Stop validating remaining transactions if one fails
            if (!result.IsValid)
            {
                // Add failure results for remaining transactions
                for (int i = results.Count; i < transactions.Count; i++)
                {
                    results.Add(TransactionValidationResult.Failure(
                        TransactionValidationErrorType.Other,
                        "Validation stopped due to previous transaction failure"));
                }
                break;
            }
        }

        return results;
    }

    /// <summary>
    /// Validates basic transaction structure and rules.
    /// </summary>
    private static TransactionValidationResult ValidateBasicStructure(Transaction transaction)
    {
        if (!transaction.ValidateBasicRules())
        {
            return TransactionValidationResult.Failure(
                TransactionValidationErrorType.BasicValidationFailed,
                "Transaction failed basic validation rules");
        }

        return TransactionValidationResult.Success();
    }

    /// <summary>
    /// Validates transaction version.
    /// </summary>
    private static TransactionValidationResult ValidateVersion(Transaction transaction)
    {
        if (transaction.Version != Transaction.CurrentVersion)
        {
            return TransactionValidationResult.Failure(
                TransactionValidationErrorType.UnsupportedVersion,
                $"Unsupported transaction version: {transaction.Version}. Expected: {Transaction.CurrentVersion}");
        }

        return TransactionValidationResult.Success();
    }

    /// <summary>
    /// Validates transaction fee is within acceptable limits.
    /// </summary>
    private TransactionValidationResult ValidateFee(Transaction transaction)
    {
        if (transaction.Fee < _config.MinimumFee)
        {
            return TransactionValidationResult.Failure(
                TransactionValidationErrorType.FeeTooLow,
                $"Transaction fee {transaction.Fee} is below minimum {_config.MinimumFee}");
        }

        if (transaction.Fee > _config.MaximumFee)
        {
            return TransactionValidationResult.Failure(
                TransactionValidationErrorType.FeeTooHigh,
                $"Transaction fee {transaction.Fee} exceeds maximum {_config.MaximumFee}");
        }

        return TransactionValidationResult.Success();
    }

    /// <summary>
    /// Validates transaction size.
    /// </summary>
    private TransactionValidationResult ValidateSize(Transaction transaction)
    {
        // All transactions have a fixed size in the current implementation
        // This check is here for future extensibility if variable-size transactions are added
        var size = Transaction.SerializedSize;
        
        if (size > _config.MaxTransactionSize)
        {
            return TransactionValidationResult.Failure(
                TransactionValidationErrorType.TransactionTooLarge,
                $"Transaction size {size} exceeds maximum {_config.MaxTransactionSize}");
        }

        return TransactionValidationResult.Success();
    }

    /// <summary>
    /// Validates transaction signature.
    /// </summary>
    private TransactionValidationResult ValidateSignature(Transaction transaction)
    {
        try
        {
            var txHash = transaction.ComputeHash();
            var isValid = _signatureVerifier.VerifySignature(
                txHash,
                transaction.Signature.ToArray(),
                transaction.Sender.ToArray());

            if (!isValid)
            {
                return TransactionValidationResult.Failure(
                    TransactionValidationErrorType.InvalidSignature,
                    $"Transaction signature verification failed: {Convert.ToHexString(txHash)}");
            }

            return TransactionValidationResult.Success();
        }
        catch (Exception ex)
        {
            return TransactionValidationResult.Failure(
                TransactionValidationErrorType.InvalidSignature,
                $"Transaction signature verification threw exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates transaction is not a duplicate.
    /// </summary>
    private TransactionValidationResult ValidateDuplicate(
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        if (_transactionIndex == null)
        {
            // If no transaction index is available, skip duplicate check
            return TransactionValidationResult.Success();
        }

        try
        {
            var txHash = transaction.ComputeHash();
            var location = _transactionIndex.GetTransactionLocation(txHash);

            if (location != null)
            {
                return TransactionValidationResult.Failure(
                    TransactionValidationErrorType.DuplicateTransaction,
                    $"Transaction already exists: {Convert.ToHexString(txHash)}");
            }

            return TransactionValidationResult.Success();
        }
        catch
        {
            // If duplicate check fails, don't fail validation
            // This allows the system to continue operating if the index is temporarily unavailable
            return TransactionValidationResult.Success();
        }
    }

    /// <summary>
    /// Validates transaction against account state (balance and nonce).
    /// </summary>
    private TransactionValidationResult ValidateAccountState(Transaction transaction)
    {
        var senderKey = transaction.Sender.ToArray();
        var senderAccount = _accountStorage.GetAccount(senderKey) ?? new AccountState(0, 0);

        // Validate nonce (must match current account nonce)
        if (transaction.Nonce != senderAccount.Nonce)
        {
            return TransactionValidationResult.Failure(
                TransactionValidationErrorType.InvalidNonce,
                $"Transaction nonce {transaction.Nonce} does not match account nonce {senderAccount.Nonce}");
        }

        // Validate sender has sufficient balance
        var totalRequired = transaction.Amount + transaction.Fee;
        if (senderAccount.Balance < totalRequired)
        {
            return TransactionValidationResult.Failure(
                TransactionValidationErrorType.InsufficientBalance,
                $"Sender balance {senderAccount.Balance} is insufficient for amount {transaction.Amount} + fee {transaction.Fee}");
        }

        return TransactionValidationResult.Success();
    }

    /// <summary>
    /// Validates transaction against account state with block context.
    /// </summary>
    private TransactionValidationResult ValidateAccountStateInBlock(
        Transaction transaction,
        BlockValidationContext blockContext)
    {
        var senderKey = transaction.Sender.ToArray();
        
        // Get sender state from context or storage
        AccountState senderAccount;
        var trackedState = blockContext.GetTrackedAccountState(transaction.Sender);
        if (trackedState.HasValue)
        {
            senderAccount = new AccountState(trackedState.Value.balance, trackedState.Value.nonce);
        }
        else
        {
            senderAccount = _accountStorage.GetAccount(senderKey) ?? new AccountState(0, 0);
        }

        // Validate nonce (must match current account nonce)
        if (transaction.Nonce != senderAccount.Nonce)
        {
            return TransactionValidationResult.Failure(
                TransactionValidationErrorType.InvalidNonce,
                $"Transaction nonce {transaction.Nonce} does not match account nonce {senderAccount.Nonce}");
        }

        // Validate sender has sufficient balance
        var totalRequired = transaction.Amount + transaction.Fee;
        if (senderAccount.Balance < totalRequired)
        {
            return TransactionValidationResult.Failure(
                TransactionValidationErrorType.InsufficientBalance,
                $"Sender balance {senderAccount.Balance} is insufficient for amount {transaction.Amount} + fee {transaction.Fee}");
        }

        return TransactionValidationResult.Success();
    }
}
