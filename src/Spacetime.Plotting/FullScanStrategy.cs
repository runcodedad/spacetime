namespace Spacetime.Plotting;

/// <summary>
/// Scans every leaf in the plot to guarantee finding the best proof.
/// </summary>
/// <remarks>
/// This strategy provides the highest quality proofs by checking every leaf,
/// but may be slow for very large plots. Best used for:
/// - Small to medium plots (< 10 GB)
/// - When finding the absolute best proof is critical
/// - When parallel scanning is available to speed up the process
/// </remarks>
public sealed class FullScanStrategy : IScanningStrategy
{
    /// <summary>
    /// Gets the singleton instance of the full scan strategy.
    /// </summary>
    public static FullScanStrategy Instance { get; } = new();

    private FullScanStrategy()
    {
    }

    /// <inheritdoc/>
    public string Name => "FullScan";

    /// <inheritdoc/>
    public IEnumerable<long> GetIndicesToScan(long totalLeaves)
    {
        if (totalLeaves <= 0)
        {
            throw new ArgumentException("Total leaves must be positive", nameof(totalLeaves));
        }

        for (long i = 0; i < totalLeaves; i++)
        {
            yield return i;
        }
    }
}
