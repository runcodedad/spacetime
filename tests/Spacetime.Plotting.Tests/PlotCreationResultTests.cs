using System.Security.Cryptography;

namespace Spacetime.Plotting.Tests;

public class PlotCreationResultTests
{
    [Fact]
    public void Constructor_WithValidHeader_CreatesResult()
    {
        // Arrange
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var header = new PlotHeader(plotSeed, 1000, 32, 10, merkleRoot);

        // Act
        var result = new PlotCreationResult(header);

        // Assert
        Assert.NotNull(result);
        Assert.Same(header, result.Header);
        Assert.Null(result.CacheFilePath);
    }

    [Fact]
    public void Constructor_WithHeaderAndCachePath_CreatesResult()
    {
        // Arrange
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var header = new PlotHeader(plotSeed, 1000, 32, 10, merkleRoot);
        var cachePath = "/path/to/cache.cache";

        // Act
        var result = new PlotCreationResult(header, cachePath);

        // Assert
        Assert.NotNull(result);
        Assert.Same(header, result.Header);
        Assert.Equal(cachePath, result.CacheFilePath);
    }

    [Fact]
    public void Constructor_WithNullHeader_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PlotCreationResult(null!));
    }
}
