namespace Spacetime.Consensus;

/// <summary>
/// Enumeration of transaction validation error types.
/// </summary>
public enum TransactionValidationErrorType
{
    /// <summary>
    /// Transaction signature is invalid or missing.
    /// </summary>
    InvalidSignature,

    /// <summary>
    /// Sender does not have sufficient balance for amount + fee.
    /// </summary>
    InsufficientBalance,

    /// <summary>
    /// Transaction nonce does not match expected nonce for sender account.
    /// </summary>
    InvalidNonce,

    /// <summary>
    /// Transaction fee is below minimum required fee.
    /// </summary>
    FeeTooLow,

    /// <summary>
    /// Transaction fee exceeds maximum allowed fee.
    /// </summary>
    FeeTooHigh,

    /// <summary>
    /// Transaction size exceeds maximum allowed size.
    /// </summary>
    TransactionTooLarge,

    /// <summary>
    /// Duplicate transaction detected (same hash already exists).
    /// </summary>
    DuplicateTransaction,

    /// <summary>
    /// Transaction failed basic validation rules (e.g., sender equals recipient, amount invalid).
    /// </summary>
    BasicValidationFailed,

    /// <summary>
    /// Transaction version is not supported.
    /// </summary>
    UnsupportedVersion,

    /// <summary>
    /// Sender and recipient addresses are identical.
    /// </summary>
    SelfTransfer,

    /// <summary>
    /// Other validation error.
    /// </summary>
    Other
}
