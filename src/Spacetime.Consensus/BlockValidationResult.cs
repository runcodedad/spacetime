namespace Spacetime.Consensus;

/// <summary>
/// Represents the result of block validation.
/// </summary>
public sealed class BlockValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the block is valid.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IReadOnlyList<BlockValidationError> Errors { get; }

    /// <summary>
    /// Gets the primary error message if validation failed.
    /// </summary>
    public string? ErrorMessage => Errors.Count > 0 ? Errors[0].Message : null;

    private BlockValidationResult(bool isValid, IReadOnlyList<BlockValidationError> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static BlockValidationResult Success() => 
        new(true, Array.Empty<BlockValidationError>());

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <exception cref="ArgumentNullException">Thrown when error is null.</exception>
    public static BlockValidationResult Failure(BlockValidationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(false, new[] { error });
    }

    /// <summary>
    /// Creates a failed validation result with multiple errors.
    /// </summary>
    /// <param name="errors">The list of validation errors.</param>
    /// <exception cref="ArgumentNullException">Thrown when errors is null.</exception>
    /// <exception cref="ArgumentException">Thrown when errors is empty.</exception>
    public static BlockValidationResult Failure(IReadOnlyList<BlockValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        
        if (errors.Count == 0)
        {
            throw new ArgumentException("Must provide at least one error", nameof(errors));
        }

        return new(false, errors);
    }
}
