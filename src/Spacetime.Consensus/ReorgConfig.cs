namespace Spacetime.Consensus;

/// <summary>
/// Configuration for chain reorganization behavior.
/// </summary>
/// <remarks>
/// This configuration controls safety limits for chain reorganizations to prevent
/// excessive resource usage and ensure stability.
/// </remarks>
public sealed class ReorgConfig
{
    /// <summary>
    /// Default maximum depth for chain reorganizations.
    /// </summary>
    public const int DefaultMaxReorgDepth = 100;

    /// <summary>
    /// Gets or sets the maximum number of blocks that can be reverted during a reorganization.
    /// </summary>
    /// <remarks>
    /// If a reorganization would require reverting more than this many blocks,
    /// it will be rejected. This prevents attacks that attempt to reorganize
    /// very deep chains and protects against excessive resource usage.
    /// </remarks>
    public int MaxReorgDepth { get; init; } = DefaultMaxReorgDepth;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (MaxReorgDepth <= 0)
        {
            throw new ArgumentException(
                "Maximum reorg depth must be positive.",
                nameof(MaxReorgDepth));
        }
    }
}
