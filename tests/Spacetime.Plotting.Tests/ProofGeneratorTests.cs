using System.Security.Cryptography;
using MerkleTree.Hashing;

namespace Spacetime.Plotting.Tests;

public class ProofGeneratorTests
{
    private readonly IHashFunction _hashFunction = new Sha256HashFunction();

    [Fact]
    public async Task GenerateProofAsync_WithValidPlot_ReturnsValidProof()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create a small plot
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act
            var proof = await generator.GenerateProofAsync(
                loader,
                challenge,
                FullScanStrategy.Instance);

            // Assert
            Assert.NotNull(proof);
            Assert.Equal(32, proof!.LeafValue.Length);
            Assert.InRange(proof.LeafIndex, 0, loader.LeafCount - 1);
            Assert.Equal(32, proof.Score.Length);
            Assert.Equal(challenge, proof.Challenge);
            Assert.Equal(loader.MerkleRoot.ToArray(), proof.MerkleRoot);
            Assert.NotEmpty(proof.SiblingHashes);
            Assert.NotEmpty(proof.OrientationBits);
            Assert.Equal(proof.SiblingHashes.Count, proof.OrientationBits.Count);
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
    public async Task GenerateProofAsync_WithSamplingStrategy_ReturnsValidProof()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create a small plot
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act - sample only 1000 leaves
            var strategy = new SamplingScanStrategy(1000);
            var proof = await generator.GenerateProofAsync(
                loader,
                challenge,
                strategy);

            // Assert
            Assert.NotNull(proof);
            Assert.Equal(32, proof!.LeafValue.Length);
            Assert.InRange(proof.LeafIndex, 0, loader.LeafCount - 1);
            Assert.Equal(32, proof.Score.Length);
            Assert.Equal(challenge, proof.Challenge);
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
    public async Task GenerateProofAsync_WithDifferentChallenges_ProducesDifferentProofs()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var challenge1 = RandomNumberGenerator.GetBytes(32);
        var challenge2 = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create a small plot
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act
            var proof1 = await generator.GenerateProofAsync(
                loader,
                challenge1,
                FullScanStrategy.Instance);

            var proof2 = await generator.GenerateProofAsync(
                loader,
                challenge2,
                FullScanStrategy.Instance);

            // Assert
            Assert.NotNull(proof1);
            Assert.NotNull(proof2);
            // Different challenges should produce different scores (very likely)
            Assert.NotEqual(proof1!.Score, proof2!.Score);
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
    public async Task GenerateProofAsync_WithNullPlotLoader_ThrowsArgumentNullException()
    {
        // Arrange
        var generator = new ProofGenerator(_hashFunction);
        var challenge = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await generator.GenerateProofAsync(
                null!,
                challenge,
                FullScanStrategy.Instance));
    }

    [Fact]
    public async Task GenerateProofAsync_WithNullChallenge_ThrowsArgumentNullException()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await generator.GenerateProofAsync(
                    loader,
                    null!,
                    FullScanStrategy.Instance));
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
    public async Task GenerateProofAsync_WithInvalidChallengeSize_ThrowsArgumentException()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(16); // Wrong size
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await generator.GenerateProofAsync(
                    loader,
                    challenge,
                    FullScanStrategy.Instance));
            Assert.Contains("32 bytes", exception.Message);
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
    public async Task GenerateProofFromMultiplePlotsAsync_ReturnsProofWithBestScore()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        
        var plot1Path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");
        var plot2Path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create two small plots with different seeds
            var plotSeed1 = RandomNumberGenerator.GetBytes(32);
            var plotSeed2 = RandomNumberGenerator.GetBytes(32);
            
            var config1 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed1, plot1Path);
            var config2 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed2, plot2Path);
            
            await creator.CreatePlotAsync(config1);
            await creator.CreatePlotAsync(config2);

            var loader1 = await PlotLoader.LoadAsync(plot1Path, _hashFunction);
            var loader2 = await PlotLoader.LoadAsync(plot2Path, _hashFunction);

            try
            {
                // Act
                var proof = await generator.GenerateProofFromMultiplePlotsAsync(
                    new[] { loader1, loader2 },
                    challenge,
                    FullScanStrategy.Instance);

                // Assert
                Assert.NotNull(proof);
                Assert.Equal(32, proof!.Score.Length);
                Assert.Equal(challenge, proof.Challenge);
            }
            finally
            {
                await loader1.DisposeAsync();
                await loader2.DisposeAsync();
            }
        }
        finally
        {
            if (File.Exists(plot1Path)) File.Delete(plot1Path);
            if (File.Exists(plot2Path)) File.Delete(plot2Path);
        }
    }

    [Fact]
    public async Task GenerateProofFromMultiplePlotsAsync_WithEmptyList_ThrowsArgumentException()
    {
        // Arrange
        var generator = new ProofGenerator(_hashFunction);
        var challenge = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await generator.GenerateProofFromMultiplePlotsAsync(
                Array.Empty<PlotLoader>(),
                challenge,
                FullScanStrategy.Instance));
    }

    [Fact]
    public async Task GenerateProofAsync_WithProgressReporter_ReportsProgress()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        var progressReports = new List<double>();
        var progress = new Progress<double>(p => progressReports.Add(p));

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act
            await generator.GenerateProofAsync(
                loader,
                challenge,
                FullScanStrategy.Instance,
                progress);

            // Assert
            Assert.NotEmpty(progressReports);
            Assert.Contains(100.0, progressReports); // Should report completion
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
    public void Constructor_WithNullHashFunction_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProofGenerator(null!));
    }
}
