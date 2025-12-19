namespace Spacetime.Plotting;

/// <summary>
/// Configuration for plot scanning behavior during proof generation.
/// </summary>
/// <remarks>
/// This configuration controls how aggressively the proof generator scans plots
/// and when it can terminate early. Supports both quality-based and time-based
/// early termination strategies.
/// </remarks>
public sealed class ScanningConfiguration
{
    /// <summary>
    /// Gets the default configuration with no early termination.
    /// </summary>
    public static ScanningConfiguration Default { get; } = new();

    /// <summary>
    /// Gets whether early termination is enabled.
    /// </summary>
    /// <remarks>
    /// When true, scanning will stop if a proof meeting the quality threshold is found.
    /// This can significantly speed up proof generation but may not find the absolute best proof.
    /// </remarks>
    public bool EnableEarlyTermination { get; init; }

    /// <summary>
    /// Gets the quality threshold for early termination (leading zero bits required).
    /// </summary>
    /// <remarks>
    /// If a proof score has this many leading zero bits, scanning will terminate early.
    /// Higher values mean stricter quality requirements. Common values:
    /// - 8 bits: Very relaxed (1/256 proofs qualify)
    /// - 16 bits: Relaxed (1/65,536 proofs qualify)
    /// - 24 bits: Moderate (1/16,777,216 proofs qualify)
    /// - 32 bits: Strict (1/4,294,967,296 proofs qualify)
    /// Only used when EnableEarlyTermination is true.
    /// </remarks>
    public int QualityThresholdBits { get; init; }

    /// <summary>
    /// Gets the maximum number of leaves to scan before giving up.
    /// </summary>
    /// <remarks>
    /// If set, scanning will terminate after checking this many leaves even if
    /// no proof meeting the quality threshold was found. This provides a hard
    /// time limit for proof generation. A value of 0 means no limit.
    /// </remarks>
    public long MaxLeavesToScan { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScanningConfiguration"/> class.
    /// </summary>
    /// <param name="enableEarlyTermination">Whether to enable early termination</param>
    /// <param name="qualityThresholdBits">Quality threshold in leading zero bits (0-256)</param>
    /// <param name="maxLeavesToScan">Maximum leaves to scan (0 for unlimited)</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public ScanningConfiguration(
        bool enableEarlyTermination = false,
        int qualityThresholdBits = 16,
        long maxLeavesToScan = 0)
    {
        if (qualityThresholdBits < 0 || qualityThresholdBits > 256)
        {
            throw new ArgumentException(
                "Quality threshold bits must be between 0 and 256",
                nameof(qualityThresholdBits));
        }

        if (maxLeavesToScan < 0)
        {
            throw new ArgumentException(
                "Max leaves to scan must be non-negative",
                nameof(maxLeavesToScan));
        }

        EnableEarlyTermination = enableEarlyTermination;
        QualityThresholdBits = qualityThresholdBits;
        MaxLeavesToScan = maxLeavesToScan;
    }

    /// <summary>
    /// Creates a configuration with early termination enabled for fast mining.
    /// </summary>
    /// <param name="qualityThresholdBits">Required leading zero bits (default: 16)</param>
    /// <returns>A configuration optimized for fast proof generation</returns>
    public static ScanningConfiguration CreateFastMode(int qualityThresholdBits = 16)
    {
        return new ScanningConfiguration(
            enableEarlyTermination: true,
            qualityThresholdBits: qualityThresholdBits,
            maxLeavesToScan: 0);
    }

    /// <summary>
    /// Creates a configuration with time-limited scanning.
    /// </summary>
    /// <param name="maxLeaves">Maximum number of leaves to scan</param>
    /// <returns>A configuration with scan limit</returns>
    public static ScanningConfiguration CreateTimeLimited(long maxLeaves)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLeaves);

        return new ScanningConfiguration(
            enableEarlyTermination: false,
            qualityThresholdBits: 0,
            maxLeavesToScan: maxLeaves);
    }

    /// <summary>
    /// Checks if a score meets the quality threshold for early termination.
    /// </summary>
    /// <param name="score">The proof score to check</param>
    /// <returns>True if the score has enough leading zero bits</returns>
    public bool MeetsQualityThreshold(ReadOnlySpan<byte> score)
    {
        if (!EnableEarlyTermination)
        {
            return false;
        }

        var requiredBits = QualityThresholdBits;
        var bitsFound = 0;

        foreach (var b in score)
        {
            if (b == 0)
            {
                bitsFound += 8;
                if (bitsFound >= requiredBits)
                {
                    return true;
                }
            }
            else
            {
                // Count leading zeros in the byte
                var leadingZeros = CountLeadingZeros(b);
                bitsFound += leadingZeros;
                
                if (bitsFound >= requiredBits)
                {
                    return true;
                }
                
                // Found a non-zero bit, stop counting
                break;
            }
        }

        return bitsFound >= requiredBits;
    }

    /// <summary>
    /// Counts the number of leading zero bits in a byte.
    /// </summary>
    private static int CountLeadingZeros(byte value)
    {
        if (value == 0)
        {
            return 8;
        }

        var count = 0;
        var mask = 0x80;

        while ((value & mask) == 0)
        {
            count++;
            mask >>= 1;
        }

        return count;
    }
}
