namespace Spacetime.Plotting;

/// <summary>
/// Scans a subset of leaves in the plot for faster proof generation on large plots.
/// </summary>
/// <remarks>
/// This strategy trades proof quality for speed by only checking a sample of leaves.
/// Best used for:
/// - Very large plots (> 10 GB)
/// - Time-sensitive proof generation
/// - When "good enough" proofs are acceptable
/// 
/// The strategy uses deterministic sampling based on the total leaf count to ensure
/// consistent results across multiple scans.
/// </remarks>
public sealed class SamplingScanStrategy : IScanningStrategy
{
    private readonly int _sampleSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="SamplingScanStrategy"/> class.
    /// </summary>
    /// <param name="sampleSize">The number of leaves to sample (must be positive)</param>
    public SamplingScanStrategy(int sampleSize)
    {
        if (sampleSize <= 0)
        {
            throw new ArgumentException("Sample size must be positive", nameof(sampleSize));
        }

        _sampleSize = sampleSize;
    }

    /// <inheritdoc/>
    public string Name => $"Sampling({_sampleSize})";

    /// <summary>
    /// Gets the configured sample size.
    /// </summary>
    public int SampleSize => _sampleSize;

    /// <inheritdoc/>
    public IEnumerable<long> GetIndicesToScan(long totalLeaves)
    {
        if (totalLeaves <= 0)
        {
            throw new ArgumentException("Total leaves must be positive", nameof(totalLeaves));
        }

        // If sample size is greater than or equal to total leaves, scan everything
        if (_sampleSize >= totalLeaves)
        {
            for (long i = 0; i < totalLeaves; i++)
            {
                yield return i;
            }
            yield break;
        }

        // Use evenly distributed sampling for deterministic results
        // This ensures we get good coverage across the entire plot
        var step = (double)totalLeaves / _sampleSize;

        for (var i = 0; i < _sampleSize; i++)
        {
            var index = (long)(i * step);
            yield return index;
        }
    }
}
