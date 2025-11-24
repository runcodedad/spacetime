using Xunit;

namespace Spacetime.Common.Tests;

public sealed class ProgressReporterTests
{
    [Fact]
    public void Constructor_WithValidArguments_Succeeds()
    {
        // Arrange
        var progress = new Progress<double>();

        // Act
        var reporter = new ProgressReporter(100, progress);

        // Assert
        Assert.NotNull(reporter);
    }

    [Fact]
    public void ReportItemProcessed_WithSingleItem_ReportsZeroPercentage()
    {
        // Arrange
        var reportedValues = new List<double>();
        var progress = new Progress<double>(value => reportedValues.Add(value));
        var reporter = new ProgressReporter(100, progress);

        // Act
        reporter.ReportItemProcessed();

        // Give progress time to fire (Progress<T> uses SynchronizationContext)
        Thread.Sleep(50);

        // Assert
        Assert.Single(reportedValues);
        Assert.Equal(1, reportedValues[0]);
    }

    [Fact]
    public void ReportItemProcessed_WithAllItems_ReportsOneHundredPercentage()
    {
        // Arrange
        var reportedValues = new List<double>();
        var progress = new Progress<double>(value => reportedValues.Add(value));
        var reporter = new ProgressReporter(10, progress);

        // Act
        for (var i = 0; i < 10; i++)
        {
            reporter.ReportItemProcessed();
        }

        // Give progress time to fire
        Thread.Sleep(50);

        // Assert
        Assert.Contains(100, reportedValues);
    }

    [Fact]
    public void ReportItemProcessed_OnlyReportsWhenPercentageChanges()
    {
        // Arrange
        var reportedValues = new List<double>();
        var progress = new Progress<double>(value => reportedValues.Add(value));
        var reporter = new ProgressReporter(1000, progress);

        // Act - Process 20 items (should report 1% and 2%)
        for (var i = 0; i < 20; i++)
        {
            reporter.ReportItemProcessed();
        }

        // Give progress time to fire
        Thread.Sleep(50);

        // Assert - Should have reported only when percentage changed (1% and 2%)
        Assert.True(reportedValues.Count <= 3, $"Expected at most 3 reports, got {reportedValues.Count}");
        Assert.Contains(1, reportedValues);
        Assert.Contains(2, reportedValues);
    }

    [Fact]
    public void ReportItemProcessed_IsThreadSafe()
    {
        // Arrange
        var reportedValues = new List<double>();
        var lockObj = new object();
        var progress = new Progress<double>(value =>
        {
            lock (lockObj)
            {
                reportedValues.Add(value);
            }
        });
        var reporter = new ProgressReporter(1000, progress);

        // Act - Process items from multiple threads
        var tasks = new List<Task>();
        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (var j = 0; j < 100; j++)
                {
                    reporter.ReportItemProcessed();
                }
            }));
        }

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Task.WaitAll([.. tasks]);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

        // Give progress time to fire
        Thread.Sleep(100);

        // Assert - Should have reported 100%
        Assert.Contains(100, reportedValues);
        
        // Verify all reported values are valid percentages
        lock (lockObj)
        {
            foreach (var value in reportedValues)
            {
                Assert.InRange(value, 0, 100);
            }
        }
    }

    [Fact]
    public void ReportItemProcessed_WithSmallTotalItems_ReportsAllPercentages()
    {
        // Arrange
        var reportedValues = new List<double>();
        var progress = new SynchronousProgress(value => reportedValues.Add(value));
        var reporter = new ProgressReporter(5, progress);

        // Act
        for (var i = 0; i < 5; i++)
        {
            reporter.ReportItemProcessed();
        }

        // Assert - Should report percentage milestones (20%, 40%, 60%, 80%, 100%)
        Assert.Equal(5, reportedValues.Count);
        Assert.Contains(20, reportedValues);
        Assert.Contains(40, reportedValues);
        Assert.Contains(60, reportedValues);
        Assert.Contains(80, reportedValues);
        Assert.Contains(100, reportedValues);
        
        // Verify we got distinct percentage values
        var distinctValues = reportedValues.Distinct().ToList();
        Assert.Equal(reportedValues.Count, distinctValues.Count);
    }

    [Fact]
    public void ReportItemProcessed_WithOneItem_ReportsOneHundredPercentage()
    {
        // Arrange
        var reportedValues = new List<double>();
        var progress = new Progress<double>(value => reportedValues.Add(value));
        var reporter = new ProgressReporter(1, progress);

        // Act
        reporter.ReportItemProcessed();

        // Give progress time to fire
        Thread.Sleep(50);

        // Assert
        Assert.Single(reportedValues);
        Assert.Equal(100, reportedValues[0]);
    }

    [Fact]
    public void ReportItemProcessed_DoesNotReportDuplicatePercentages()
    {
        // Arrange
        var reportedValues = new List<double>();
        var progress = new Progress<double>(value => reportedValues.Add(value));
        var reporter = new ProgressReporter(200, progress);

        // Act - Process 4 items (each represents 2% progress, so percentages are 2%, 4%)
        for (var i = 0; i < 4; i++)
        {
            reporter.ReportItemProcessed();
        }

        // Give progress time to fire
        Thread.Sleep(50);

        // Assert - Should only report 1% and 2%, no duplicates
        var distinctValues = reportedValues.Distinct().ToList();
        Assert.Equal(reportedValues.Count, distinctValues.Count);
    }

    [Theory]
    [InlineData(100, 10, 10)]  // 10 items out of 100 = 10%
    [InlineData(100, 50, 50)]  // 50 items out of 100 = 50%
    [InlineData(100, 99, 99)]  // 99 items out of 100 = 99%
    [InlineData(1000, 250, 25)] // 250 items out of 1000 = 25%
    public void ReportItemProcessed_ReportsCorrectPercentage(long totalItems, int itemsToProcess, int expectedPercentage)
    {
        // Arrange
        var reportedValues = new List<double>();
        var progress = new Progress<double>(value => reportedValues.Add(value));
        var reporter = new ProgressReporter(totalItems, progress);

        // Act
        for (var i = 0; i < itemsToProcess; i++)
        {
            reporter.ReportItemProcessed();
        }

        // Give progress time to fire
        Thread.Sleep(50);

        // Assert
        Assert.Contains(expectedPercentage, reportedValues);
    }

    /// <summary>
    /// Synchronous IProgress implementation for deterministic testing.
    /// </summary>
    private sealed class SynchronousProgress(Action<double> handler) : IProgress<double>
    {
        public void Report(double value) => handler(value);
    }
}
