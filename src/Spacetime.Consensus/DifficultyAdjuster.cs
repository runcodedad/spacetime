using System.Numerics;

namespace Spacetime.Consensus;

/// <summary>
/// Implements the difficulty adjustment algorithm for the Spacetime blockchain.
/// </summary>
/// <remarks>
/// <para>
/// The difficulty adjustment mechanism maintains target block time by periodically
/// recalculating difficulty based on actual block times. The algorithm:
/// </para>
/// <list type="number">
/// <item>Calculates rolling average block time over adjustment interval</item>
/// <item>Compares average to target block time</item>
/// <item>Adjusts difficulty proportionally with dampening</item>
/// <item>Enforces minimum and maximum difficulty bounds</item>
/// </list>
/// <para>
/// <strong>Difficulty System:</strong>
/// </para>
/// <para>
/// The blockchain uses a two-level difficulty system:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <strong>Difficulty Integer</strong>: A positive integer where higher values = more difficult.
/// This is human-readable and stored in block headers.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Difficulty Target</strong> (32-byte hash): The maximum score value that can be considered
/// valid. Lower targets = more difficult. Derived from difficulty integer using DifficultyToTarget().
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>Adjustment Formula:</strong>
/// </para>
/// <code>
/// actualTime = timestamp[height] - timestamp[height - interval]
/// targetTime = interval * targetBlockTime
/// ratio = actualTime / targetTime
/// 
/// newDifficulty = currentDifficulty * targetTime / actualTime
/// // Apply dampening to smooth adjustments
/// newDifficulty = currentDifficulty + (newDifficulty - currentDifficulty) / dampeningFactor
/// // Enforce bounds
/// newDifficulty = max(min(newDifficulty, maxDifficulty), minDifficulty)
/// </code>
/// <example>
/// Using the difficulty adjuster:
/// <code>
/// var config = DifficultyAdjustmentConfig.Default();
/// var adjuster = new DifficultyAdjuster(config);
/// 
/// // Calculate new difficulty at adjustment point
/// long newDifficulty = adjuster.CalculateNextDifficulty(
///     currentDifficulty: 1000,
///     currentHeight: 100,
///     currentTimestamp: 1000,
///     intervalStartTimestamp: 0);
/// 
/// // Convert difficulty to 32-byte target for validation
/// byte[] target = DifficultyAdjuster.DifficultyToTarget(newDifficulty);
/// </code>
/// </example>
/// </remarks>
public sealed class DifficultyAdjuster
{
    private readonly DifficultyAdjustmentConfig _config;

    /// <summary>
    /// Maximum value for a 256-bit number (2^256 - 1).
    /// </summary>
    private static readonly BigInteger _maxTarget = BigInteger.Pow(2, 256) - 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="DifficultyAdjuster"/> class.
    /// </summary>
    /// <param name="config">The difficulty adjustment configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    public DifficultyAdjuster(DifficultyAdjustmentConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
    }

    /// <summary>
    /// Calculates the next difficulty based on actual vs target block times.
    /// </summary>
    /// <param name="currentDifficulty">The current difficulty value.</param>
    /// <param name="currentHeight">The current block height.</param>
    /// <param name="currentTimestamp">The timestamp of the current block.</param>
    /// <param name="intervalStartTimestamp">The timestamp of the block at the start of the adjustment interval.</param>
    /// <returns>The new difficulty value, bounded by min/max limits.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters have invalid values.</exception>
    /// <remarks>
    /// <para>
    /// This method should be called when:
    /// - currentHeight % adjustmentIntervalBlocks == 0
    /// - There is a complete interval of blocks to analyze
    /// </para>
    /// <para>
    /// The algorithm:
    /// 1. Calculates actual time taken for the interval
    /// 2. Calculates expected time based on target block time
    /// 3. Adjusts difficulty proportionally with dampening
    /// 4. Enforces minimum and maximum bounds
    /// </para>
    /// </remarks>
    public long CalculateNextDifficulty(
        long currentDifficulty,
        long currentHeight,
        long currentTimestamp,
        long intervalStartTimestamp)
    {
        if (currentDifficulty <= 0)
        {
            throw new ArgumentException("Current difficulty must be positive", nameof(currentDifficulty));
        }

        if (currentHeight < 0)
        {
            throw new ArgumentException("Current height must be non-negative", nameof(currentHeight));
        }

        if (currentTimestamp < 0)
        {
            throw new ArgumentException("Current timestamp must be non-negative", nameof(currentTimestamp));
        }

        if (intervalStartTimestamp < 0)
        {
            throw new ArgumentException("Interval start timestamp must be non-negative", nameof(intervalStartTimestamp));
        }

        if (currentTimestamp < intervalStartTimestamp)
        {
            throw new ArgumentException("Current timestamp must be >= interval start timestamp", nameof(currentTimestamp));
        }

        // Calculate actual time taken for the interval
        var actualTime = currentTimestamp - intervalStartTimestamp;

        // Calculate expected time based on target block time
        var targetTime = (long)_config.AdjustmentIntervalBlocks * _config.TargetBlockTimeSeconds;

        // If actualTime is 0 or very small, clamp it to prevent division by zero or extreme adjustments
        if (actualTime <= 0)
        {
            actualTime = 1;
        }

        // Calculate the raw adjustment: newDifficulty = currentDifficulty * targetTime / actualTime
        // If actualTime < targetTime (blocks too fast), difficulty increases
        // If actualTime > targetTime (blocks too slow), difficulty decreases
        var rawDifficulty = (double)currentDifficulty * targetTime / actualTime;

        // Apply dampening factor to smooth the adjustment
        // newDifficulty = currentDifficulty + (rawDifficulty - currentDifficulty) / dampeningFactor
        var dampenedAdjustment = (rawDifficulty - currentDifficulty) / _config.DampeningFactor;
        var newDifficulty = currentDifficulty + dampenedAdjustment;

        // Round to long and enforce bounds
        var finalDifficulty = (long)Math.Round(newDifficulty);
        finalDifficulty = Math.Max(finalDifficulty, _config.MinimumDifficulty);
        finalDifficulty = Math.Min(finalDifficulty, _config.MaximumDifficulty);

        return finalDifficulty;
    }

    /// <summary>
    /// Determines if difficulty should be adjusted at the given height.
    /// </summary>
    /// <param name="height">The block height to check.</param>
    /// <returns>True if difficulty should be adjusted at this height; otherwise, false.</returns>
    /// <remarks>
    /// Difficulty is adjusted every N blocks where N is the adjustment interval.
    /// The genesis block (height 0) does not trigger an adjustment.
    /// </remarks>
    public bool ShouldAdjustDifficulty(long height)
    {
        if (height <= 0)
        {
            return false;
        }

        return height % _config.AdjustmentIntervalBlocks == 0;
    }

    /// <summary>
    /// Converts a difficulty integer to a 32-byte difficulty target.
    /// </summary>
    /// <param name="difficulty">The difficulty integer (higher = more difficult).</param>
    /// <returns>A 32-byte difficulty target (lower = more difficult).</returns>
    /// <exception cref="ArgumentException">Thrown when difficulty is not positive.</exception>
    /// <remarks>
    /// <para>
    /// The conversion formula is: target = maxTarget / difficulty
    /// where maxTarget = 2^256 - 1
    /// </para>
    /// <para>
    /// Properties:
    /// - Higher difficulty → lower target → harder to mine
    /// - difficulty = 1 → target = maxTarget (easiest)
    /// - difficulty = maxTarget → target ≈ 1 (hardest)
    /// </para>
    /// <para>
    /// The target is returned as a 32-byte array in big-endian format, matching
    /// the format used for score comparison in proof validation.
    /// </para>
    /// </remarks>
    public static byte[] DifficultyToTarget(long difficulty)
    {
        if (difficulty <= 0)
        {
            throw new ArgumentException("Difficulty must be positive", nameof(difficulty));
        }

        // Calculate target = maxTarget / difficulty
        var target = _maxTarget / difficulty;

        // Convert to 32-byte array (big-endian)
        var targetBytes = target.ToByteArray(isUnsigned: true, isBigEndian: true);

        // Ensure exactly 32 bytes
        if (targetBytes.Length < 32)
        {
            // Pad with leading zeros
            var padded = new byte[32];
            Array.Copy(targetBytes, 0, padded, 32 - targetBytes.Length, targetBytes.Length);
            return padded;
        }
        else if (targetBytes.Length > 32)
        {
            // Should not happen with valid difficulty values, but handle gracefully
            // Take the last 32 bytes (least significant)
            var trimmed = new byte[32];
            Array.Copy(targetBytes, targetBytes.Length - 32, trimmed, 0, 32);
            return trimmed;
        }

        return targetBytes;
    }

    /// <summary>
    /// Converts a 32-byte difficulty target to a difficulty integer.
    /// </summary>
    /// <param name="target">The 32-byte difficulty target.</param>
    /// <returns>The difficulty integer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when target is null.</exception>
    /// <exception cref="ArgumentException">Thrown when target has invalid size.</exception>
    /// <remarks>
    /// <para>
    /// The conversion formula is: difficulty = maxTarget / target
    /// </para>
    /// <para>
    /// This is the inverse of DifficultyToTarget(), but note that some precision
    /// may be lost in the round-trip conversion due to integer division.
    /// </para>
    /// <para>
    /// If the target is all zeros or results in difficulty greater than long.MaxValue,
    /// returns long.MaxValue.
    /// </para>
    /// </remarks>
    public static long TargetToDifficulty(byte[] target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (target.Length != 32)
        {
            throw new ArgumentException("Target must be 32 bytes", nameof(target));
        }

        // Convert from big-endian bytes to BigInteger
        var targetValue = new BigInteger(target, isUnsigned: true, isBigEndian: true);

        // Handle edge case: target = 0 → difficulty = max
        if (targetValue.IsZero)
        {
            return long.MaxValue;
        }

        // Calculate difficulty = maxTarget / target
        var difficulty = _maxTarget / targetValue;

        // Ensure difficulty fits in long range
        if (difficulty > long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)difficulty;
    }
}
