namespace Spacetime.Plotting.Tests;

public class CacheFriendlyScanStrategyTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesStrategy()
    {
        // Arrange & Act
        var strategy = new CacheFriendlyScanStrategy(blockSize: 1000, leavesPerBlock: 500);

        // Assert
        Assert.Equal(1000, strategy.BlockSize);
        Assert.Equal(500, strategy.LeavesPerBlock);
        Assert.Equal("CacheFriendly(block=1000,scan=500)", strategy.Name);
    }

    [Fact]
    public void Constructor_WithZeroBlockSize_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new CacheFriendlyScanStrategy(blockSize: 0));
    }

    [Fact]
    public void Constructor_WithNegativeBlockSize_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new CacheFriendlyScanStrategy(blockSize: -1));
    }

    [Fact]
    public void Constructor_WithZeroLeavesPerBlock_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new CacheFriendlyScanStrategy(blockSize: 1000, leavesPerBlock: 0));
    }

    [Fact]
    public void Constructor_WithLeavesPerBlockExceedingBlockSize_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new CacheFriendlyScanStrategy(blockSize: 1000, leavesPerBlock: 1001));
    }

    [Fact]
    public void GetIndicesToScan_WithSingleFullBlock_ScansAllLeaves()
    {
        // Arrange
        var strategy = new CacheFriendlyScanStrategy(blockSize: 1000, leavesPerBlock: 1000);
        var totalLeaves = 1000L;

        // Act
        var indices = strategy.GetIndicesToScan(totalLeaves).ToArray();

        // Assert
        Assert.Equal(1000, indices.Length);
        for (long i = 0; i < totalLeaves; i++)
        {
            Assert.Contains(i, indices);
        }
    }

    [Fact]
    public void GetIndicesToScan_WithMultipleBlocks_ScansAcrossAllBlocks()
    {
        // Arrange
        var strategy = new CacheFriendlyScanStrategy(blockSize: 100, leavesPerBlock: 100);
        var totalLeaves = 300L; // 3 blocks

        // Act
        var indices = strategy.GetIndicesToScan(totalLeaves).ToArray();

        // Assert
        Assert.Equal(300, indices.Length);
        
        // Verify we have indices from each block
        Assert.Contains(50L, indices); // Block 0
        Assert.Contains(150L, indices); // Block 1
        Assert.Contains(250L, indices); // Block 2
    }

    [Fact]
    public void GetIndicesToScan_WithSampling_SamplesEvenlyWithinBlocks()
    {
        // Arrange
        var strategy = new CacheFriendlyScanStrategy(blockSize: 100, leavesPerBlock: 10);
        var totalLeaves = 200L; // 2 blocks

        // Act
        var indices = strategy.GetIndicesToScan(totalLeaves).ToArray();

        // Assert
        Assert.Equal(20, indices.Length); // 10 per block * 2 blocks
        
        // First block should have indices around 0-99
        var firstBlockIndices = indices.Where(i => i < 100).ToArray();
        Assert.Equal(10, firstBlockIndices.Length);
        Assert.Equal(0, firstBlockIndices.First());
        
        // Second block should have indices around 100-199
        var secondBlockIndices = indices.Where(i => i >= 100).ToArray();
        Assert.Equal(10, secondBlockIndices.Length);
        Assert.Equal(100, secondBlockIndices.First());
    }

    [Fact]
    public void GetIndicesToScan_WithPartialLastBlock_HandlesCorrectly()
    {
        // Arrange
        var strategy = new CacheFriendlyScanStrategy(blockSize: 100, leavesPerBlock: 100);
        var totalLeaves = 250L; // 2 full blocks + 1 partial (50 leaves)

        // Act
        var indices = strategy.GetIndicesToScan(totalLeaves).ToArray();

        // Assert
        Assert.Equal(250, indices.Length);
        
        // Verify last index is within bounds
        Assert.True(indices.All(i => i < totalLeaves));
    }

    [Fact]
    public void GetIndicesToScan_WithZeroLeaves_ThrowsArgumentException()
    {
        // Arrange
        var strategy = new CacheFriendlyScanStrategy();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            strategy.GetIndicesToScan(0).ToArray());
    }

    [Fact]
    public void GetIndicesToScan_WithNegativeLeaves_ThrowsArgumentException()
    {
        // Arrange
        var strategy = new CacheFriendlyScanStrategy();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            strategy.GetIndicesToScan(-1).ToArray());
    }

    [Fact]
    public void GetScanCount_WithFullBlocks_ReturnsCorrectCount()
    {
        // Arrange
        var strategy = new CacheFriendlyScanStrategy(blockSize: 100, leavesPerBlock: 50);
        var totalLeaves = 300L; // 3 blocks

        // Act
        var count = strategy.GetScanCount(totalLeaves);

        // Assert
        Assert.Equal(150, count); // 50 per block * 3 blocks
    }

    [Fact]
    public void GetScanCount_WithPartialLastBlock_ReturnsCorrectCount()
    {
        // Arrange
        var strategy = new CacheFriendlyScanStrategy(blockSize: 100, leavesPerBlock: 50);
        var totalLeaves = 250L; // 2 full blocks + 1 partial (50 leaves)

        // Act
        var count = strategy.GetScanCount(totalLeaves);

        // Assert
        Assert.Equal(150, count); // 50 + 50 + 50
    }

    [Fact]
    public void GetScanCount_WithVerySmallLastBlock_LimitsToActualSize()
    {
        // Arrange
        var strategy = new CacheFriendlyScanStrategy(blockSize: 100, leavesPerBlock: 50);
        var totalLeaves = 210L; // 2 full blocks + 1 small (10 leaves)

        // Act
        var count = strategy.GetScanCount(totalLeaves);

        // Assert
        Assert.Equal(110, count); // 50 + 50 + 10
    }

    [Fact]
    public void CreateForL2Cache_UsesOptimalBlockSize()
    {
        // Arrange & Act
        var strategy = CacheFriendlyScanStrategy.CreateForL2Cache();

        // Assert
        Assert.Equal(8192, strategy.BlockSize); // 8192 * 32 bytes = 256KB
        Assert.Equal(8192, strategy.LeavesPerBlock);
    }

    [Fact]
    public void CreateForL3Cache_UsesOptimalBlockSize()
    {
        // Arrange & Act
        var strategy = CacheFriendlyScanStrategy.CreateForL3Cache();

        // Assert
        Assert.Equal(32768, strategy.BlockSize); // 32768 * 32 bytes = 1MB
        Assert.Equal(32768, strategy.LeavesPerBlock);
    }

    [Fact]
    public void CreateSampling_WithValidSamplesPerBlock_CreatesStrategy()
    {
        // Arrange & Act
        var strategy = CacheFriendlyScanStrategy.CreateSampling(samplesPerBlock: 2048);

        // Assert
        Assert.Equal(CacheFriendlyScanStrategy.DefaultBlockSize, strategy.BlockSize);
        Assert.Equal(2048, strategy.LeavesPerBlock);
    }

    [Fact]
    public void CreateSampling_WithZeroSamples_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CacheFriendlyScanStrategy.CreateSampling(0));
    }

    [Fact]
    public void CreateSampling_WithNegativeSamples_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CacheFriendlyScanStrategy.CreateSampling(-1));
    }

    [Fact]
    public void GetIndicesToScan_ProducesDeterministicResults()
    {
        // Arrange
        var strategy = new CacheFriendlyScanStrategy(blockSize: 100, leavesPerBlock: 50);
        var totalLeaves = 500L;

        // Act
        var indices1 = strategy.GetIndicesToScan(totalLeaves).ToArray();
        var indices2 = strategy.GetIndicesToScan(totalLeaves).ToArray();

        // Assert
        Assert.Equal(indices1, indices2);
    }

    [Fact]
    public void GetIndicesToScan_WithDefaultBlockSize_ScansInSequence()
    {
        // Arrange
        var strategy = new CacheFriendlyScanStrategy();
        var totalLeaves = CacheFriendlyScanStrategy.DefaultBlockSize * 2L;

        // Act
        var indices = strategy.GetIndicesToScan(totalLeaves).ToArray();

        // Assert
        // Should scan all leaves in sequence when leavesPerBlock == blockSize
        for (long i = 0; i < totalLeaves; i++)
        {
            Assert.Equal(i, indices[i]);
        }
    }
}
