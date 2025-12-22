using System.Collections.Concurrent;
using System.Security.Cryptography;
using MerkleTree.Hashing;
using Spacetime.Common;

namespace Spacetime.Plotting.IntegrationTests;

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
            
            var config1 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed1, plot1Path, true, 5);
            var config2 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed2, plot2Path, true, 5);
            
            var result1 = await creator.CreatePlotAsync(config1);
            var result2 = await creator.CreatePlotAsync(config2);

            var loader1 = await PlotLoader.LoadAsync(plot1Path, _hashFunction);
            var loader2 = await PlotLoader.LoadAsync(plot2Path, _hashFunction);

            try
            {
                var options = new[]
                {
                    new ProofGenerationOptions(loader1, result1.CacheFilePath),
                    new ProofGenerationOptions(loader2, result2.CacheFilePath)
                };

                // Act
                var proof = await generator.GenerateProofFromMultiplePlotsAsync(
                    options,
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
            if (File.Exists(plot1Path))
            {
                File.Delete(plot1Path);
            }

            if (File.Exists(plot2Path))
            {
                File.Delete(plot2Path);
            }
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
                Array.Empty<ProofGenerationOptions>(),
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

        var progressReports = new ConcurrentBag<double>();
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
                progress: progress);

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
    public async Task GenerateProofAsync_WithCacheFiles_PassesCacheFiles()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");
        PlotCreationResult? plotCreationResult = null;

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath, true, 5);
            plotCreationResult = await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act
            await generator.GenerateProofAsync(
                loader,
                challenge,
                FullScanStrategy.Instance,
                cacheFilePath: plotCreationResult.CacheFilePath
            );

            // Assert
            Assert.True(File.Exists(plotCreationResult.CacheFilePath));
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            if (plotCreationResult != null && File.Exists(plotCreationResult?.CacheFilePath))
            {
                File.Delete(plotCreationResult.CacheFilePath);
            }
        }
    }

    [Fact]
    public void Constructor_WithNullHashFunction_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProofGenerator(null!));
    }

    [Fact]
    public async Task GenerateProofFromMultiplePlotsAsync_WithNullPlotLoaderInOptions_ThrowsArgumentException()
    {
        // Arrange
        var generator = new ProofGenerator(_hashFunction);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var options = new[] { new ProofGenerationOptions(null!, null) };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await generator.GenerateProofFromMultiplePlotsAsync(
                options,
                challenge,
                FullScanStrategy.Instance));
    }

    [Fact]
    public async Task GenerateProofFromMultiplePlotsAsync_WithMultiplePlots_SelectsBestScore()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        
        var plot1Path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");
        var plot2Path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");
        var plot3Path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create three small plots with different seeds
            var plotSeed1 = RandomNumberGenerator.GetBytes(32);
            var plotSeed2 = RandomNumberGenerator.GetBytes(32);
            var plotSeed3 = RandomNumberGenerator.GetBytes(32);
            
            var config1 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed1, plot1Path, true, 5);
            var config2 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed2, plot2Path, true, 5);
            var config3 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed3, plot3Path, true, 5);
            
            var result1 = await creator.CreatePlotAsync(config1);
            var result2 = await creator.CreatePlotAsync(config2);
            var result3 = await creator.CreatePlotAsync(config3);

            var loader1 = await PlotLoader.LoadAsync(plot1Path, _hashFunction);
            var loader2 = await PlotLoader.LoadAsync(plot2Path, _hashFunction);
            var loader3 = await PlotLoader.LoadAsync(plot3Path, _hashFunction);

            try
            {
                var options = new[]
                {
                    new ProofGenerationOptions(loader1, result1.CacheFilePath),
                    new ProofGenerationOptions(loader2, result2.CacheFilePath),
                    new ProofGenerationOptions(loader3, result3.CacheFilePath)
                };

                // Generate individual proofs to compare
                var proof1 = await generator.GenerateProofAsync(loader1, challenge, FullScanStrategy.Instance, result1.CacheFilePath);
                var proof2 = await generator.GenerateProofAsync(loader2, challenge, FullScanStrategy.Instance, result2.CacheFilePath);
                var proof3 = await generator.GenerateProofAsync(loader3, challenge, FullScanStrategy.Instance, result3.CacheFilePath);

                // Act - generate from multiple plots
                var bestProof = await generator.GenerateProofFromMultiplePlotsAsync(
                    options,
                    challenge,
                    FullScanStrategy.Instance);

                // Assert - should get the best (lowest) score
                Assert.NotNull(bestProof);
                
                // Find which proof has the best score
                var individualProofs = new[] { proof1!, proof2!, proof3! };
                var expectedBest = individualProofs.OrderBy<Proof, byte[]>(p => p.Score, new ByteArrayComparer()).First();
                
                Assert.Equal(expectedBest.Score, bestProof!.Score);
            }
            finally
            {
                await loader1.DisposeAsync();
                await loader2.DisposeAsync();
                await loader3.DisposeAsync();
            }
        }
        finally
        {
            if (File.Exists(plot1Path))
            {
                File.Delete(plot1Path);
            }

            if (File.Exists(plot2Path))
            {
                File.Delete(plot2Path);
            }

            if (File.Exists(plot3Path))
            {
                File.Delete(plot3Path);
            }
        }
    }

    [Fact]
    public async Task GenerateProofAsync_WithNullStrategy_ThrowsArgumentNullException()
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
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await generator.GenerateProofAsync(
                    loader,
                    challenge,
                    null!));
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
    public async Task GenerateProofFromMultiplePlotsAsync_WithNullChallenge_ThrowsArgumentNullException()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath, true, 5);
            var result = await creator.CreatePlotAsync(config);

            var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            try
            {
                var options = new[] { new ProofGenerationOptions(loader, result.CacheFilePath) };

                // Act & Assert
                await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                    await generator.GenerateProofFromMultiplePlotsAsync(
                        options,
                        null!,
                        FullScanStrategy.Instance));
            }
            finally
            {
                await loader.DisposeAsync();
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
    public async Task GenerateProofFromMultiplePlotsAsync_WithNullStrategy_ThrowsArgumentNullException()
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
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath, true, 5);
            var result = await creator.CreatePlotAsync(config);

            var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            try
            {
                var options = new[] { new ProofGenerationOptions(loader, result.CacheFilePath) };

                // Act & Assert
                await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                    await generator.GenerateProofFromMultiplePlotsAsync(
                        options,
                        challenge,
                        null!));
            }
            finally
            {
                await loader.DisposeAsync();
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
    public async Task GenerateProofFromMultiplePlotsAsync_WithInvalidChallengeSize_ThrowsArgumentException()
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
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath, true, 5);
            var result = await creator.CreatePlotAsync(config);

            var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            try
            {
                var options = new[] { new ProofGenerationOptions(loader, result.CacheFilePath) };

                // Act & Assert
                var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                    await generator.GenerateProofFromMultiplePlotsAsync(
                        options,
                        challenge,
                        FullScanStrategy.Instance));
                Assert.Contains("32 bytes", exception.Message);
            }
            finally
            {
                await loader.DisposeAsync();
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
    public async Task GeneratedProof_VerifiesCorrectly_WithMerkleLibrary()
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
            
            // Verify the Merkle proof using the MerkleTree library
            var merkleProof = new MerkleTree.Proofs.MerkleProof(
                proof!.LeafValue,
                proof.LeafIndex,
                (int)loader.TreeHeight,
                proof.SiblingHashes.ToArray(),
                proof.OrientationBits.ToArray());

            var isValid = merkleProof.Verify(loader.MerkleRoot.ToArray(), _hashFunction);
            Assert.True(isValid, "Merkle proof should verify correctly");
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
    public async Task GeneratedProof_ScoreMatchesConsensusRules()
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

            // Manually compute score = H(challenge || leaf) to verify consensus rules
            var input = new byte[challenge.Length + proof!.LeafValue.Length];
            challenge.CopyTo(input.AsSpan());
            proof.LeafValue.CopyTo(input.AsSpan(challenge.Length));
            var expectedScore = _hashFunction.ComputeHash(input);

            Assert.Equal(expectedScore, proof.Score);
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
    public async Task FullScan_FindsBetterOrEqualScore_ThanSampling()
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

            // Act - generate proof with both strategies
            var fullScanProof = await generator.GenerateProofAsync(
                loader,
                challenge,
                FullScanStrategy.Instance);

            var samplingProof = await generator.GenerateProofAsync(
                loader,
                challenge,
                new SamplingScanStrategy(1000));

            // Assert
            Assert.NotNull(fullScanProof);
            Assert.NotNull(samplingProof);

            // Full scan should find a score that is less than or equal to sampling
            // (lower score is better)
            var comparison = ScoreComparer.Compare(fullScanProof!.Score, samplingProof!.Score);
            Assert.True(comparison <= 0, "Full scan should find equal or better score than sampling");
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
    public async Task ProofGeneration_WithCachedPlot_WorksCorrectly()
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
            // Create a plot with caching enabled
            var config = new PlotConfiguration(
                PlotConfiguration.MinPlotSize,
                minerKey,
                plotSeed,
                outputPath,
                includeCache: true,
                cacheLevels: 5);
            
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Act
            var proof = await generator.GenerateProofAsync(
                loader,
                challenge,
                FullScanStrategy.Instance);

            // Assert
            Assert.NotNull(proof);
            Assert.Equal(32, proof!.Score.Length);
            
            // Verify the proof
            var merkleProof = new MerkleTree.Proofs.MerkleProof(
                proof.LeafValue,
                proof.LeafIndex,
                (int)loader.TreeHeight,
                proof.SiblingHashes.ToArray(),
                proof.OrientationBits.ToArray());

            var isValid = merkleProof.Verify(loader.MerkleRoot.ToArray(), _hashFunction);
            Assert.True(isValid, "Proof from cached plot should verify correctly");
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            var cacheFile = $"{outputPath}.cache";
            if (File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
            }
        }
    }

    [Fact]
    public async Task ParallelProofGeneration_FindsBestScoreAcrossMultiplePlots()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        
        var plot1Path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");
        var plot2Path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");
        var plot3Path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create three plots with different seeds
            var plotSeed1 = RandomNumberGenerator.GetBytes(32);
            var plotSeed2 = RandomNumberGenerator.GetBytes(32);
            var plotSeed3 = RandomNumberGenerator.GetBytes(32);
            
            var config1 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed1, plot1Path, true, 5);
            var config2 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed2, plot2Path, true, 5);
            var config3 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed3, plot3Path, true, 5);
            
            var result1 = await creator.CreatePlotAsync(config1);
            var result2 = await creator.CreatePlotAsync(config2);
            var result3 = await creator.CreatePlotAsync(config3);

            var loader1 = await PlotLoader.LoadAsync(plot1Path, _hashFunction);
            var loader2 = await PlotLoader.LoadAsync(plot2Path, _hashFunction);
            var loader3 = await PlotLoader.LoadAsync(plot3Path, _hashFunction);

            try
            {
                var options = new[]
                {
                    new ProofGenerationOptions(loader1, result1.CacheFilePath),
                    new ProofGenerationOptions(loader2, result2.CacheFilePath),
                    new ProofGenerationOptions(loader3, result3.CacheFilePath)
                };

                // Act - generate proofs from all plots in parallel
                var bestProof = await generator.GenerateProofFromMultiplePlotsAsync(
                    options,
                    challenge,
                    FullScanStrategy.Instance);

                // Also generate individual proofs to verify best selection
                var proof1 = await generator.GenerateProofAsync(loader1, challenge, FullScanStrategy.Instance, result1.CacheFilePath);
                var proof2 = await generator.GenerateProofAsync(loader2, challenge, FullScanStrategy.Instance, result2.CacheFilePath);
                var proof3 = await generator.GenerateProofAsync(loader3, challenge, FullScanStrategy.Instance, result3.CacheFilePath);

                // Assert
                Assert.NotNull(bestProof);
                Assert.NotNull(proof1);
                Assert.NotNull(proof2);
                Assert.NotNull(proof3);

                // Best proof should have lowest score
                Assert.True(
                    ScoreComparer.Compare(bestProof!.Score, proof1!.Score) <= 0 &&
                    ScoreComparer.Compare(bestProof.Score, proof2!.Score) <= 0 &&
                    ScoreComparer.Compare(bestProof.Score, proof3!.Score) <= 0,
                    "Parallel generation should select the proof with the best (lowest) score");
            }
            finally
            {
                await loader1.DisposeAsync();
                await loader2.DisposeAsync();
                await loader3.DisposeAsync();
            }
        }
        finally
        {
            if (File.Exists(plot1Path))
            {
                File.Delete(plot1Path);
            }

            if (File.Exists(plot2Path))
            {
                File.Delete(plot2Path);
            }

            if (File.Exists(plot3Path))
            {
                File.Delete(plot3Path);
            }
        }
    }

    // Helper class for comparing byte arrays
    private class ByteArrayComparer : IComparer<byte[]>
    {
        public int Compare(byte[]? x, byte[]? y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            for (var i = 0; i < Math.Min(x.Length, y.Length); i++)
            {
                var diff = x[i] - y[i];
                if (diff != 0)
                {
                    return diff;
                }
            }
            return x.Length - y.Length;
        }
    }
}
