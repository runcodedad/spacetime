namespace Spacetime.Common;

/// <summary>
/// Helper class for reporting progress during long-running operations.
/// </summary>
/// <remarks>
/// Thread-safe progress reporter that tracks processed items and reports
/// percentage completion only when the percentage value changes, reducing
/// callback overhead.
/// </remarks>
public sealed class ProgressReporter(long totalItems, IProgress<double> progress)
{
    private long _processedItems;
    private int _lastReportedPercentage = -1;

    /// <summary>
    /// Reports that one item has been processed.
    /// </summary>
    /// <remarks>
    /// Thread-safe method that increments the counter and reports progress
    /// when the percentage changes.
    /// </remarks>
    public void ReportItemProcessed()
    {
        var processed = Interlocked.Increment(ref _processedItems);
        var percentage = (int)(processed * 100.0 / totalItems);

        // Only report when percentage changes to reduce overhead
        if (percentage != _lastReportedPercentage)
        {
            _lastReportedPercentage = percentage;
            progress.Report(percentage);
        }
    }
}
