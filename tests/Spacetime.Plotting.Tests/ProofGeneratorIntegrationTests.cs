using System.Security.Cryptography;
using MerkleTree.Hashing;

namespace Spacetime.Plotting.Tests;

/// <summary>
/// Integration tests for proof generation and verification.
/// </summary>
public class ProofGeneratorIntegrationTests
{
    private readonly IHashFunction _hashFunction = new Sha256HashFunction();

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
            var comparison = CompareScores(fullScanProof!.Score, samplingProof!.Score);
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
            
            var config1 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed1, plot1Path);
            var config2 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed2, plot2Path);
            var config3 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed3, plot3Path);
            
            await creator.CreatePlotAsync(config1);
            await creator.CreatePlotAsync(config2);
            await creator.CreatePlotAsync(config3);

            var loader1 = await PlotLoader.LoadAsync(plot1Path, _hashFunction);
            var loader2 = await PlotLoader.LoadAsync(plot2Path, _hashFunction);
            var loader3 = await PlotLoader.LoadAsync(plot3Path, _hashFunction);

            try
            {
                // Act - generate proofs from all plots in parallel
                var bestProof = await generator.GenerateProofFromMultiplePlotsAsync(
                    new[] { loader1, loader2, loader3 },
                    challenge,
                    FullScanStrategy.Instance);

                // Also generate individual proofs to verify best selection
                var proof1 = await generator.GenerateProofAsync(loader1, challenge, FullScanStrategy.Instance);
                var proof2 = await generator.GenerateProofAsync(loader2, challenge, FullScanStrategy.Instance);
                var proof3 = await generator.GenerateProofAsync(loader3, challenge, FullScanStrategy.Instance);

                // Assert
                Assert.NotNull(bestProof);
                Assert.NotNull(proof1);
                Assert.NotNull(proof2);
                Assert.NotNull(proof3);

                // Best proof should have lowest score
                Assert.True(
                    CompareScores(bestProof!.Score, proof1!.Score) <= 0 &&
                    CompareScores(bestProof.Score, proof2!.Score) <= 0 &&
                    CompareScores(bestProof.Score, proof3!.Score) <= 0,
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
            if (File.Exists(plot1Path)) File.Delete(plot1Path);
            if (File.Exists(plot2Path)) File.Delete(plot2Path);
            if (File.Exists(plot3Path)) File.Delete(plot3Path);
        }
    }

    /// <summary>
    /// Helper method to compare two scores (lower is better).
    /// </summary>
    private static int CompareScores(byte[] score1, byte[] score2)
    {
        for (var i = 0; i < score1.Length; i++)
        {
            var diff = score1[i] - score2[i];
            if (diff != 0)
            {
                return diff;
            }
        }
        return 0;
    }
}
