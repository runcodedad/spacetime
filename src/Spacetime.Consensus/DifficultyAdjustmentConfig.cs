namespace Spacetime.Consensus;

/// <summary>
/// Configuration parameters for the difficulty adjustment algorithm.
/// </summary>
/// <remarks>
/// <para>
/// The difficulty adjustment mechanism maintains the target block time by periodically
/// adjusting the mining difficulty based on the actual time taken to mine recent blocks.
/// </para>
/// <para>
/// This configuration defines:
/// - How often difficulty adjusts (adjustment interval)
/// - What the target block time should be
/// - How aggressively to adjust (dampening factor)
/// - Upper and lower bounds for difficulty
/// </para>
/// <example>
/// Creating a configuration for 10-second blocks with adjustment every 100 blocks:
/// <code>
/// var config = new DifficultyAdjustmentConfig(
///     targetBlockTimeSeconds: 10,
///     adjustmentIntervalBlocks: 100,
///     dampeningFactor: 4,
///     minimumDifficulty: 1,
///     maximumDifficulty: long.MaxValue);
/// </code>
/// </example>
/// </remarks>
public sealed record DifficultyAdjustmentConfig
{
    /// <summary>
    /// Default target block time in seconds.
    /// </summary>
    public const int DefaultTargetBlockTimeSeconds = 10;

    /// <summary>
    /// Default adjustment interval in blocks.
    /// </summary>
    public const int DefaultAdjustmentIntervalBlocks = 100;

    /// <summary>
    /// Default dampening factor to smooth adjustments.
    /// </summary>
    public const int DefaultDampeningFactor = 4;

    /// <summary>
    /// Minimum allowed difficulty value.
    /// </summary>
    public const long DefaultMinimumDifficulty = 1;

    /// <summary>
    /// Gets the target time between blocks in seconds.
    /// </summary>
    /// <remarks>
    /// The difficulty adjustment algorithm aims to maintain this average block time
    /// by increasing or decreasing the difficulty target.
    /// </remarks>
    public int TargetBlockTimeSeconds { get; init; }

    /// <summary>
    /// Gets the number of blocks between difficulty adjustments.
    /// </summary>
    /// <remarks>
    /// Difficulty is recalculated every N blocks. Smaller intervals allow quicker
    /// response to hash rate changes, but may cause instability. Larger intervals
    /// provide stability but slower adaptation.
    /// </remarks>
    public int AdjustmentIntervalBlocks { get; init; }

    /// <summary>
    /// Gets the dampening factor to smooth difficulty adjustments.
    /// </summary>
    /// <remarks>
    /// Higher values result in smaller, more gradual adjustments.
    /// A factor of 4 means the adjustment is divided by 4, preventing dramatic swings.
    /// Bitcoin uses a factor of 4 for its difficulty adjustment.
    /// </remarks>
    public int DampeningFactor { get; init; }

    /// <summary>
    /// Gets the minimum allowed difficulty value.
    /// </summary>
    /// <remarks>
    /// Difficulty will never be adjusted below this value, ensuring a minimum
    /// level of proof-of-work even if network hash rate drops significantly.
    /// </remarks>
    public long MinimumDifficulty { get; init; }

    /// <summary>
    /// Gets the maximum allowed difficulty value.
    /// </summary>
    /// <remarks>
    /// Difficulty will never be adjusted above this value. This prevents overflow
    /// and sets an upper limit on mining difficulty.
    /// </remarks>
    public long MaximumDifficulty { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DifficultyAdjustmentConfig"/> class.
    /// </summary>
    /// <param name="targetBlockTimeSeconds">The target time between blocks in seconds.</param>
    /// <param name="adjustmentIntervalBlocks">The number of blocks between difficulty adjustments.</param>
    /// <param name="dampeningFactor">The dampening factor to smooth adjustments.</param>
    /// <param name="minimumDifficulty">The minimum allowed difficulty value.</param>
    /// <param name="maximumDifficulty">The maximum allowed difficulty value.</param>
    /// <exception cref="ArgumentException">Thrown when parameters have invalid values.</exception>
    public DifficultyAdjustmentConfig(
        int targetBlockTimeSeconds = DefaultTargetBlockTimeSeconds,
        int adjustmentIntervalBlocks = DefaultAdjustmentIntervalBlocks,
        int dampeningFactor = DefaultDampeningFactor,
        long minimumDifficulty = DefaultMinimumDifficulty,
        long maximumDifficulty = long.MaxValue)
    {
        if (targetBlockTimeSeconds <= 0)
        {
            throw new ArgumentException("Target block time must be positive", nameof(targetBlockTimeSeconds));
        }

        if (adjustmentIntervalBlocks <= 0)
        {
            throw new ArgumentException("Adjustment interval must be positive", nameof(adjustmentIntervalBlocks));
        }

        if (dampeningFactor <= 0)
        {
            throw new ArgumentException("Dampening factor must be positive", nameof(dampeningFactor));
        }

        if (minimumDifficulty <= 0)
        {
            throw new ArgumentException("Minimum difficulty must be positive", nameof(minimumDifficulty));
        }

        if (maximumDifficulty <= 0)
        {
            throw new ArgumentException("Maximum difficulty must be positive", nameof(maximumDifficulty));
        }

        if (minimumDifficulty > maximumDifficulty)
        {
            throw new ArgumentException("Minimum difficulty cannot exceed maximum difficulty", nameof(minimumDifficulty));
        }

        TargetBlockTimeSeconds = targetBlockTimeSeconds;
        AdjustmentIntervalBlocks = adjustmentIntervalBlocks;
        DampeningFactor = dampeningFactor;
        MinimumDifficulty = minimumDifficulty;
        MaximumDifficulty = maximumDifficulty;
    }

    /// <summary>
    /// Creates the default difficulty adjustment configuration.
    /// </summary>
    /// <returns>A new <see cref="DifficultyAdjustmentConfig"/> with default values.</returns>
    public static DifficultyAdjustmentConfig Default() => new();
}
