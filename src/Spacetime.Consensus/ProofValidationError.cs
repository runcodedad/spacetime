namespace Spacetime.Consensus;

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
