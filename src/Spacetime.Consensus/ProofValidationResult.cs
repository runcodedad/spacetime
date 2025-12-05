namespace Spacetime.Consensus;

/// <summary>
/// Represents the result of proof validation.
/// </summary>
public sealed class ProofValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the proof is valid.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the validation error, if any.
    /// </summary>
    public ProofValidationError? Error { get; }

    /// <summary>
    /// Gets the error message describing why validation failed.
    /// </summary>
    public string? ErrorMessage => Error?.Message;

    private ProofValidationResult(bool isValid, ProofValidationError? error)
    {
        IsValid = isValid;
        Error = error;
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ProofValidationResult Success() => new(true, null);

    /// <summary>
    /// Creates a failed validation result with an error.
    /// </summary>
    public static ProofValidationResult Failure(ProofValidationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(false, error);
    }
}
