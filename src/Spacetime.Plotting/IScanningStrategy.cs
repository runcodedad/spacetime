namespace Spacetime.Plotting;

/// <summary>
/// Defines a strategy for scanning a plot to find leaves for proof generation.
/// </summary>
/// <remarks>
/// Different strategies trade off between completeness and performance:
/// - Full scan: checks every leaf (guarantees best proof)
/// - Sampling: checks a subset of leaves (faster for large plots)
/// </remarks>
public interface IScanningStrategy
{
    /// <summary>
    /// Gets the name of the scanning strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Determines which leaf indices should be checked during scanning.
    /// </summary>
    /// <param name="totalLeaves">The total number of leaves in the plot</param>
    /// <returns>An enumerable of leaf indices to check</returns>
    IEnumerable<long> GetIndicesToScan(long totalLeaves);

    /// <summary>
    /// Gets the number of leaves that will be scanned without enumerating.
    /// </summary>
    /// <param name="totalLeaves">The total number of leaves in the plot</param>
    /// <returns>The count of leaves to scan</returns>
    long GetScanCount(long totalLeaves);
}
