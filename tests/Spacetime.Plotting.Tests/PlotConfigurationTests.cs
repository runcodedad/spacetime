using System.Security.Cryptography;

namespace Spacetime.Plotting.Tests;

public class PlotConfigurationTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesConfiguration()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = "/tmp/test.plot";
        var plotSize = 1024L * 1024 * 1024; // 1 GB

        // Act
        var config = new PlotConfiguration(plotSize, minerKey, plotSeed, outputPath);

        // Assert
        Assert.Equal(plotSize, config.PlotSizeBytes);
        Assert.Equal(minerKey, config.MinerPublicKey);
        Assert.Equal(plotSeed, config.PlotSeed);
        Assert.Equal(outputPath, config.OutputPath);
        Assert.Equal(plotSize / LeafGenerator.LeafSize, config.LeafCount);
        Assert.False(config.IncludeCache);
    }

    [Fact]
    public void Constructor_WithCacheEnabled_SetsCorrectProperties()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = "/tmp/test.plot";
        var plotSize = 1024L * 1024 * 1024;

        // Act
        var config = new PlotConfiguration(plotSize, minerKey, plotSeed, outputPath, includeCache: true, cacheLevels: 7);

        // Assert
        Assert.True(config.IncludeCache);
        Assert.Equal(7, config.CacheLevels);
    }

    [Fact]
    public void Constructor_PlotSizeTooSmall_ThrowsArgumentException()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = "/tmp/test.plot";
        var plotSize = 1024L; // Too small

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new PlotConfiguration(plotSize, minerKey, plotSeed, outputPath));
    }

    [Fact]
    public void Constructor_InvalidMinerKeySize_ThrowsArgumentException()
    {
        // Arrange
        var minerKey = new byte[16]; // Wrong size
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = "/tmp/test.plot";
        var plotSize = 1024L * 1024 * 1024;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new PlotConfiguration(plotSize, minerKey, plotSeed, outputPath));
    }

    [Fact]
    public void Constructor_InvalidPlotSeedSize_ThrowsArgumentException()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = new byte[16]; // Wrong size
        var outputPath = "/tmp/test.plot";
        var plotSize = 1024L * 1024 * 1024;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new PlotConfiguration(plotSize, minerKey, plotSeed, outputPath));
    }

    [Fact]
    public void Constructor_NegativeCacheLevels_ThrowsArgumentException()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = "/tmp/test.plot";
        var plotSize = 1024L * 1024 * 1024;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new PlotConfiguration(plotSize, minerKey, plotSeed, outputPath, true, -1));
    }

    [Fact]
    public void CreateFromGB_CreatesCorrectConfiguration()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = "/tmp/test.plot";

        // Act
        var config = PlotConfiguration.CreateFromGB(1, minerKey, plotSeed, outputPath);

        // Assert
        Assert.Equal(1024L * 1024 * 1024, config.PlotSizeBytes);
        Assert.Equal(1024L * 1024 * 1024 / LeafGenerator.LeafSize, config.LeafCount);
    }

    [Fact]
    public void CreateFromGB_WithLargeSize_CreatesCorrectConfiguration()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = "/tmp/test.plot";

        // Act
        var config = PlotConfiguration.CreateFromGB(100, minerKey, plotSeed, outputPath);

        // Assert
        Assert.Equal(100L * 1024 * 1024 * 1024, config.PlotSizeBytes);
        Assert.Equal(100L * 1024 * 1024 * 1024 / LeafGenerator.LeafSize, config.LeafCount);
    }

    [Fact]
    public void CreateFromGB_WithCacheEnabled_CreatesCorrectConfiguration()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = "/tmp/test.plot";

        // Act
        var config = PlotConfiguration.CreateFromGB(10, minerKey, plotSeed, outputPath, includeCache: true, cacheLevels: 7);

        // Assert
        Assert.Equal(10L * 1024 * 1024 * 1024, config.PlotSizeBytes);
        Assert.True(config.IncludeCache);
        Assert.Equal(7, config.CacheLevels);
    }

    [Fact]
    public void LeafCount_CalculatedCorrectly()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = "/tmp/test.plot";
        var plotSize = PlotConfiguration.MinPlotSize; // Use minimum valid size

        // Act
        var config = new PlotConfiguration(plotSize, minerKey, plotSeed, outputPath);

        // Assert
        Assert.Equal(plotSize / LeafGenerator.LeafSize, config.LeafCount);
    }
}
