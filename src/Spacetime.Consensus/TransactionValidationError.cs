namespace Spacetime.Consensus;

/// <summary>
/// Represents a transaction validation error with detailed information.
/// </summary>
/// <param name="Type">The type of validation error.</param>
/// <param name="Message">A detailed error message.</param>
public sealed record TransactionValidationError(
    TransactionValidationErrorType Type,
    string Message);
