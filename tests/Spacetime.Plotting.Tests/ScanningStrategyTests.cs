namespace Spacetime.Plotting.Tests;

public class ScanningStrategyTests
{
    [Fact]
    public void FullScanStrategy_ScansAllLeaves()
    {
        // Arrange
        var strategy = FullScanStrategy.Instance;
        var totalLeaves = 1000L;

        // Act
        var indices = strategy.GetIndicesToScan(totalLeaves).ToArray();

        // Assert
        Assert.Equal(totalLeaves, indices.Length);
        for (long i = 0; i < totalLeaves; i++)
        {
            Assert.Contains(i, indices);
        }
    }

    [Fact]
    public void FullScanStrategy_WithZeroLeaves_ThrowsArgumentException()
    {
        // Arrange
        var strategy = FullScanStrategy.Instance;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            strategy.GetIndicesToScan(0).ToArray());
    }

    [Fact]
    public void FullScanStrategy_WithNegativeLeaves_ThrowsArgumentException()
    {
        // Arrange
        var strategy = FullScanStrategy.Instance;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            strategy.GetIndicesToScan(-1).ToArray());
    }

    [Fact]
    public void FullScanStrategy_HasCorrectName()
    {
        // Arrange
        var strategy = FullScanStrategy.Instance;

        // Act & Assert
        Assert.Equal("FullScan", strategy.Name);
    }

    [Fact]
    public void SamplingScanStrategy_WithSmallerSampleThanTotal_SamplesCorrectNumber()
    {
        // Arrange
        var sampleSize = 100;
        var strategy = new SamplingScanStrategy(sampleSize);
        var totalLeaves = 1000L;

        // Act
        var indices = strategy.GetIndicesToScan(totalLeaves).ToArray();

        // Assert
        Assert.Equal(sampleSize, indices.Length);
        Assert.All(indices, index => Assert.InRange(index, 0, totalLeaves - 1));
        // Check that indices are evenly distributed
        Assert.Equal(0, indices.First());
        Assert.True(indices.Last() > totalLeaves / 2); // Should sample from later part too
    }

    [Fact]
    public void SamplingScanStrategy_WithSampleSizeGreaterThanTotal_ScansAll()
    {
        // Arrange
        var sampleSize = 1000;
        var strategy = new SamplingScanStrategy(sampleSize);
        var totalLeaves = 100L;

        // Act
        var indices = strategy.GetIndicesToScan(totalLeaves).ToArray();

        // Assert
        Assert.Equal(totalLeaves, indices.Length);
        for (long i = 0; i < totalLeaves; i++)
        {
            Assert.Contains(i, indices);
        }
    }

    [Fact]
    public void SamplingScanStrategy_ProducesDeterministicResults()
    {
        // Arrange
        var sampleSize = 100;
        var strategy = new SamplingScanStrategy(sampleSize);
        var totalLeaves = 1000L;

        // Act
        var indices1 = strategy.GetIndicesToScan(totalLeaves).ToArray();
        var indices2 = strategy.GetIndicesToScan(totalLeaves).ToArray();

        // Assert
        Assert.Equal(indices1, indices2);
    }

    [Fact]
    public void SamplingScanStrategy_WithZeroSampleSize_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SamplingScanStrategy(0));
    }

    [Fact]
    public void SamplingScanStrategy_WithNegativeSampleSize_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SamplingScanStrategy(-1));
    }

    [Fact]
    public void SamplingScanStrategy_HasCorrectName()
    {
        // Arrange
        var sampleSize = 42;
        var strategy = new SamplingScanStrategy(sampleSize);

        // Act & Assert
        Assert.Equal("Sampling(42)", strategy.Name);
    }

    [Fact]
    public void SamplingScanStrategy_ExposesCorrectSampleSize()
    {
        // Arrange
        var sampleSize = 42;
        var strategy = new SamplingScanStrategy(sampleSize);

        // Act & Assert
        Assert.Equal(sampleSize, strategy.SampleSize);
    }

    [Fact]
    public void SamplingScanStrategy_WithZeroTotalLeaves_ThrowsArgumentException()
    {
        // Arrange
        var strategy = new SamplingScanStrategy(100);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            strategy.GetIndicesToScan(0).ToArray());
    }
}
