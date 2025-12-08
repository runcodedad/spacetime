namespace Spacetime.Consensus;

/// <summary>
/// Represents the result of transaction validation.
/// </summary>
/// <remarks>
/// This immutable record provides detailed information about validation success or failure.
/// When validation fails, it includes specific error information to help diagnose issues.
/// </remarks>
public sealed record TransactionValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the transaction is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the validation error if validation failed.
    /// </summary>
    public TransactionValidationError? Error { get; init; }

    /// <summary>
    /// Gets a human-readable error message if validation failed.
    /// </summary>
    public string? ErrorMessage => Error?.Message;

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A validation result indicating success.</returns>
    public static TransactionValidationResult Success() =>
        new() { IsValid = true, Error = null };

    /// <summary>
    /// Creates a failed validation result with detailed error information.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static TransactionValidationResult Failure(TransactionValidationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new TransactionValidationResult { IsValid = false, Error = error };
    }

    /// <summary>
    /// Creates a failed validation result with error type and message.
    /// </summary>
    /// <param name="errorType">The type of validation error.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static TransactionValidationResult Failure(TransactionValidationErrorType errorType, string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Failure(new TransactionValidationError(errorType, message));
    }
}
