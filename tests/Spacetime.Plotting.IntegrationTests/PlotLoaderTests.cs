using System.Security.Cryptography;
using MerkleTree.Hashing;

namespace Spacetime.Plotting.Tests;

public class PlotLoaderTests
{
    private readonly IHashFunction _hashFunction = new Sha256HashFunction();

    [Fact]
    public async Task LoadAsync_WithValidPlotFile_LoadsSuccessfully()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            var result = await creator.CreatePlotAsync(config);
            var createdHeader = result.Header;

            // Act
            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Assert
            Assert.NotNull(loader);
            Assert.Equal(createdHeader.LeafCount, loader.LeafCount);
            Assert.Equal(createdHeader.LeafSize, loader.LeafSize);
            Assert.Equal(createdHeader.TreeHeight, loader.TreeHeight);
            Assert.True(createdHeader.PlotSeed.SequenceEqual(loader.PlotSeed));
            Assert.True(createdHeader.MerkleRoot.SequenceEqual(loader.MerkleRoot));
            Assert.Equal(outputPath, loader.FilePath);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.plot");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await PlotLoader.LoadAsync(nonExistentPath, _hashFunction));
    }

    [Fact]
    public async Task LoadAsync_WithNullFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await PlotLoader.LoadAsync(null!, _hashFunction));
    }

    [Fact]
    public async Task LoadAsync_WithNullHashFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await PlotLoader.LoadAsync(tempFile, null!));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_WithTruncatedHeader_ThrowsInvalidOperationException()
    {
        // Arrange
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create a file with incomplete header
            await File.WriteAllBytesAsync(outputPath, new byte[50]); // Less than header size

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await PlotLoader.LoadAsync(outputPath, _hashFunction));
            
            Assert.Contains("too small", exception.Message);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_WithInvalidMagicBytes_ThrowsInvalidOperationException()
    {
        // Arrange
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create a file with invalid magic bytes
            var invalidData = new byte[PlotHeader.TotalHeaderSize];
            invalidData[0] = 0xFF; // Invalid magic byte
            await File.WriteAllBytesAsync(outputPath, invalidData);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await PlotLoader.LoadAsync(outputPath, _hashFunction));
            
            Assert.Contains("parse plot header", exception.Message);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_WithCorruptedChecksum_ThrowsInvalidOperationException()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            // Corrupt the checksum
            using (var fs = File.OpenWrite(outputPath))
            {
                fs.Seek(PlotHeader.HeaderSize, SeekOrigin.Begin);
                fs.WriteByte(0xFF);
            }

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await PlotLoader.LoadAsync(outputPath, _hashFunction));
            
            Assert.Contains("parse plot header", exception.Message);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_WithTruncatedPlotData_ThrowsInvalidOperationException()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            // Truncate the file (remove some leaves)
            using (var fs = File.OpenWrite(outputPath))
            {
                fs.SetLength(PlotHeader.TotalHeaderSize + 100); // Only 100 bytes of leaf data
            }

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await PlotLoader.LoadAsync(outputPath, _hashFunction));
            
            Assert.Contains("truncated", exception.Message);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ReadLeafAsync_WithValidIndex_ReturnsCorrectLeaf()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act - Read first, middle, and last leaf
            var firstLeaf = await loader.ReadLeafAsync(0);
            var middleLeaf = await loader.ReadLeafAsync(loader.LeafCount / 2);
            var lastLeaf = await loader.ReadLeafAsync(loader.LeafCount - 1);

            // Assert
            Assert.NotNull(firstLeaf);
            Assert.Equal(LeafGenerator.LeafSize, firstLeaf.Length);
            Assert.NotNull(middleLeaf);
            Assert.Equal(LeafGenerator.LeafSize, middleLeaf.Length);
            Assert.NotNull(lastLeaf);
            Assert.Equal(LeafGenerator.LeafSize, lastLeaf.Length);

            // Verify leaves match expected values
            var expectedFirstLeaf = LeafGenerator.GenerateLeaf(minerKey, plotSeed, 0);
            var expectedMiddleLeaf = LeafGenerator.GenerateLeaf(minerKey, plotSeed, loader.LeafCount / 2);
            var expectedLastLeaf = LeafGenerator.GenerateLeaf(minerKey, plotSeed, loader.LeafCount - 1);

            Assert.Equal(expectedFirstLeaf, firstLeaf);
            Assert.Equal(expectedMiddleLeaf, middleLeaf);
            Assert.Equal(expectedLastLeaf, lastLeaf);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ReadLeafAsync_WithNegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await loader.ReadLeafAsync(-1));
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ReadLeafAsync_WithIndexBeyondCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await loader.ReadLeafAsync(loader.LeafCount));
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ReadLeafAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);
            await loader.DisposeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await loader.ReadLeafAsync(0));
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ReadLeavesAsync_WithValidRange_ReturnsCorrectLeaves()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act - Read 10 consecutive leaves
            const int count = 10;
            var leaves = await loader.ReadLeavesAsync(0, count);

            // Assert
            Assert.NotNull(leaves);
            Assert.Equal(count, leaves.Length);

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(LeafGenerator.LeafSize, leaves[i].Length);
                var expectedLeaf = LeafGenerator.GenerateLeaf(minerKey, plotSeed, i);
                Assert.Equal(expectedLeaf, leaves[i]);
            }
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ReadLeavesAsync_WithInvalidRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act & Assert - Range exceeds leaf count
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await loader.ReadLeavesAsync(loader.LeafCount - 5, 10));
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task VerifyMerkleRootAsync_WithValidPlot_ReturnsTrue()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act
            var isValid = await loader.VerifyMerkleRootAsync();

            // Assert
            Assert.True(isValid);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task VerifyMerkleRootAsync_WithCorruptedLeaf_ReturnsFalse()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            // Corrupt a leaf in the middle
            using (var fs = File.OpenWrite(outputPath))
            {
                var corruptPosition = PlotHeader.TotalHeaderSize + (LeafGenerator.LeafSize * 100);
                fs.Seek(corruptPosition, SeekOrigin.Begin);
                fs.WriteByte(0xFF);
                fs.WriteByte(0xFF);
            }

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act
            var isValid = await loader.VerifyMerkleRootAsync();

            // Assert
            Assert.False(isValid);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task VerifyMerkleRootAsync_ReportsProgress()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            var progressReports = new List<double>();
            var progress = new Progress<double>(p => progressReports.Add(p));

            // Act
            await loader.VerifyMerkleRootAsync(progress);

            // Assert
            Assert.NotEmpty(progressReports);
            Assert.True(progressReports.First() >= 0);
            Assert.True(progressReports.Last() <= 100);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task MultipleReads_FromSameLoader_WorkCorrectly()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act - Read same leaf multiple times
            var leaf1 = await loader.ReadLeafAsync(42);
            var leaf2 = await loader.ReadLeafAsync(42);
            var leaf3 = await loader.ReadLeafAsync(100);
            var leaf4 = await loader.ReadLeafAsync(42);

            // Assert
            Assert.Equal(leaf1, leaf2);
            Assert.Equal(leaf1, leaf4);
            Assert.NotEqual(leaf1, leaf3);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act - Dispose multiple times
            await loader.DisposeAsync();
            await loader.DisposeAsync();
            await loader.DisposeAsync();

            // Assert - No exception should be thrown
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_AllowsSharedReading()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            // Act - Open same file with two loaders
            await using var loader1 = await PlotLoader.LoadAsync(outputPath, _hashFunction);
            await using var loader2 = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            var leaf1 = await loader1.ReadLeafAsync(10);
            var leaf2 = await loader2.ReadLeafAsync(10);

            // Assert - Both loaders can read the same file
            Assert.Equal(leaf1, leaf2);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }
}
