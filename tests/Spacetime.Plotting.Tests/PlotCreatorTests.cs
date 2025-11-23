using System.Security.Cryptography;
using MerkleTree.Hashing;

namespace Spacetime.Plotting.Tests;

public class PlotCreatorTests
{
    [Fact]
    public async Task CreatePlotAsync_CreatesValidPlotFile()
    {
        // Arrange
        var creator = new PlotCreator(new Sha256HashFunction());
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Use minimum plot size for testing
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);

            // Act
            var header = await creator.CreatePlotAsync(config);

            // Assert
            Assert.NotNull(header);
            Assert.True(File.Exists(outputPath));
            Assert.Equal(config.LeafCount, header.LeafCount);
            Assert.Equal(LeafGenerator.LeafSize, header.LeafSize);
            Assert.False(header.MerkleRoot.IsEmpty);
            Assert.False(header.Checksum.IsEmpty);
            Assert.True(header.VerifyChecksum());

            // Verify file size (header + leaves)
            var fileInfo = new FileInfo(outputPath);
            var expectedSize = PlotHeader.TotalHeaderSize + (config.LeafCount * LeafGenerator.LeafSize);
            Assert.Equal(expectedSize, fileInfo.Length);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task CreatePlotAsync_WithCache_CreatesPlotAndCacheFiles()
    {
        // Arrange
        var creator = new PlotCreator(new Sha256HashFunction());
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");
        var cacheFilePath = $"{outputPath}.cache";

        try
        {
            // Use minimum plot size for testing
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath, includeCache: true, cacheLevels: 3);

            // Act
            var header = await creator.CreatePlotAsync(config);

            // Assert
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(cacheFilePath));
            Assert.NotNull(header);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            if (File.Exists(cacheFilePath))
            {
                File.Delete(cacheFilePath);
            }
        }
    }

    [Fact]
    public async Task CreatePlotAsync_IsDeterministic()
    {
        // Arrange
        var creator = new PlotCreator(new Sha256HashFunction());
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath1 = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");
        var outputPath2 = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config1 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath1);
            var config2 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath2);

            // Act
            var header1 = await creator.CreatePlotAsync(config1);
            var header2 = await creator.CreatePlotAsync(config2);

            // Assert - Headers should have same merkle root
            Assert.True(header1.MerkleRoot.SequenceEqual(header2.MerkleRoot));
            Assert.Equal(header1.LeafCount, header2.LeafCount);
            Assert.Equal(header1.TreeHeight, header2.TreeHeight);

            // Assert - Files should be identical
            var bytes1 = await File.ReadAllBytesAsync(outputPath1);
            var bytes2 = await File.ReadAllBytesAsync(outputPath2);
            Assert.Equal(bytes1.Length, bytes2.Length);

            // Compare all bytes (deterministic including checksums)
            Assert.Equal(bytes1, bytes2);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath1))
            {
                File.Delete(outputPath1);
            }
            if (File.Exists(outputPath2))
            {
                File.Delete(outputPath2);
            }
        }
    }

    [Fact]
    public async Task CreatePlotAsync_CanReadHeaderFromFile()
    {
        // Arrange
        var creator = new PlotCreator(new Sha256HashFunction());
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);

            // Act - Create plot
            var originalHeader = await creator.CreatePlotAsync(config);

            // Read header from file
            var headerBytes = new byte[PlotHeader.TotalHeaderSize];
            using (var fs = File.OpenRead(outputPath))
            {
                var bytesRead = await fs.ReadAsync(headerBytes.AsMemory(0, PlotHeader.TotalHeaderSize));
                Assert.Equal(PlotHeader.TotalHeaderSize, bytesRead);
            }

            var readHeader = PlotHeader.Deserialize(headerBytes);

            // Assert - Use SequenceEqual for span comparisons
            Assert.True(originalHeader.PlotSeed.SequenceEqual(readHeader.PlotSeed));
            Assert.Equal(originalHeader.LeafCount, readHeader.LeafCount);
            Assert.Equal(originalHeader.LeafSize, readHeader.LeafSize);
            Assert.Equal(originalHeader.TreeHeight, readHeader.TreeHeight);
            Assert.True(originalHeader.MerkleRoot.SequenceEqual(readHeader.MerkleRoot));
            Assert.True(readHeader.VerifyChecksum());
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task CreatePlotAsync_ReportsProgress()
    {
        // Arrange
        var creator = new PlotCreator(new Sha256HashFunction());
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            var progressReports = new List<double>();
            var progress = new Progress<double>(p => progressReports.Add(p));

            // Act
            await creator.CreatePlotAsync(config, progress);

            // Assert
            Assert.NotEmpty(progressReports);
            Assert.True(progressReports.First() >= 0);
            Assert.True(progressReports.Last() <= 100);
            Assert.True(progressReports.Last() > 0); // Some progress should be reported
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task CreatePlotAsync_CancellationWorks()
    {
        // Arrange
        var creator = new PlotCreator(new Sha256HashFunction());
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Use minimum plot size (still large enough to test cancellation)
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            var cts = new CancellationTokenSource();
            
            // Cancel after a short delay
            cts.CancelAfter(TimeSpan.FromMilliseconds(10));

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await creator.CreatePlotAsync(config, cancellationToken: cts.Token);
            });
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task CreatePlotAsync_CreatesOutputDirectory()
    {
        // Arrange
        var creator = new PlotCreator(new Sha256HashFunction());
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var uniqueDir = Path.Combine(Path.GetTempPath(), $"plotdir_{Guid.NewGuid()}");
        var outputPath = Path.Combine(uniqueDir, "test.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);

            // Act
            await creator.CreatePlotAsync(config);

            // Assert
            Assert.True(Directory.Exists(uniqueDir));
            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(uniqueDir))
            {
                Directory.Delete(uniqueDir, recursive: true);
            }
        }
    }
}
