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

/// <summary>
/// Represents a proof validation error with detailed information.
/// </summary>
public sealed class ProofValidationError
{
    /// <summary>
    /// Gets the type of validation error.
    /// </summary>
    public ProofValidationErrorType Type { get; }

    /// <summary>
    /// Gets a detailed error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProofValidationError"/> class.
    /// </summary>
    public ProofValidationError(ProofValidationErrorType type, string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Type = type;
        Message = message;
    }
}

/// <summary>
/// Specifies the type of proof validation error.
/// </summary>
public enum ProofValidationErrorType
{
    /// <summary>
    /// The Merkle proof path is invalid.
    /// </summary>
    InvalidMerklePath,

    /// <summary>
    /// The leaf value does not match the expected value.
    /// </summary>
    InvalidLeafValue,

    /// <summary>
    /// The plot root does not match the known plot identity.
    /// </summary>
    PlotRootMismatch,

    /// <summary>
    /// The score does not match the recalculated value.
    /// </summary>
    ScoreMismatch,

    /// <summary>
    /// The score does not meet the difficulty target.
    /// </summary>
    ScoreAboveTarget,

    /// <summary>
    /// The challenge does not match the expected challenge.
    /// </summary>
    ChallengeMismatch
}
