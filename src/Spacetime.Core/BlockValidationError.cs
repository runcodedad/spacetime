namespace Spacetime.Core;

/// <summary>
/// Represents a block validation error.
/// </summary>
public sealed class BlockValidationError
{
    /// <summary>
    /// Gets the type of validation error.
    /// </summary>
    public BlockValidationErrorType ErrorType { get; }

    /// <summary>
    /// Gets the detailed error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockValidationError"/> class.
    /// </summary>
    /// <param name="errorType">The type of validation error.</param>
    /// <param name="message">The detailed error message.</param>
    /// <exception cref="ArgumentException">Thrown when message is null or whitespace.</exception>
    public BlockValidationError(BlockValidationErrorType errorType, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Error message cannot be null or whitespace", nameof(message));
        }

        ErrorType = errorType;
        Message = message;
    }

    /// <summary>
    /// Returns a string representation of this error.
    /// </summary>
    public override string ToString() => $"[{ErrorType}] {Message}";
}
