using System.Security.Cryptography;
using MerkleTree.Hashing;
using Spacetime.Plotting;

namespace Spacetime.Consensus.Tests;

/// <summary>
/// Integration tests for proof validation with the MerkleTree library.
/// </summary>
public class ProofValidatorIntegrationTests
{
    private readonly IHashFunction _hashFunction = new Sha256HashFunction();
    private readonly ProofValidator _validator;

    public ProofValidatorIntegrationTests()
    {
        _validator = new ProofValidator(_hashFunction);
    }

    [Fact]
    public async Task ValidateProof_WithGeneratedProof_AcceptsValidProof()
    {
        // Arrange - create a plot and generate a real proof
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

            // Generate a valid proof
            var proof = await generator.GenerateProofAsync(
                loader,
                challenge,
                FullScanStrategy.Instance);

            Assert.NotNull(proof);

            // Act - validate the proof
            var result = _validator.ValidateProof(
                proof!,
                challenge,
                loader.MerkleRoot.ToArray(),
                treeHeight: (int)loader.TreeHeight);

            // Assert
            Assert.True(result.IsValid, $"Proof should be valid but got error: {result.ErrorMessage}");
            Assert.Null(result.Error);
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
    public async Task ValidateProof_WithGeneratedProofAndDifficultyTarget_WorksCorrectly()
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

            // Generate a valid proof
            var proof = await generator.GenerateProofAsync(
                loader,
                challenge,
                FullScanStrategy.Instance);

            Assert.NotNull(proof);

            // Create a very high difficulty target (easy to meet)
            var easyTarget = new byte[32];
            Array.Fill(easyTarget, (byte)0xFF);

            // Act - validate with easy target
            var result = _validator.ValidateProof(
                proof!,
                challenge,
                loader.MerkleRoot.ToArray(),
                difficultyTarget: easyTarget,
                treeHeight: (int)loader.TreeHeight);

            // Assert
            Assert.True(result.IsValid, $"Proof should pass with easy target but got: {result.ErrorMessage}");
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
    public async Task ValidateProof_WithImpossibleDifficultyTarget_RejectsProof()
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

            // Generate a valid proof
            var proof = await generator.GenerateProofAsync(
                loader,
                challenge,
                FullScanStrategy.Instance);

            Assert.NotNull(proof);

            // Create an impossible difficulty target (all zeros - nothing can be below this)
            var impossibleTarget = new byte[32]; // All zeros

            // Act - validate with impossible target
            var result = _validator.ValidateProof(
                proof!,
                challenge,
                loader.MerkleRoot.ToArray(),
                difficultyTarget: impossibleTarget,
                treeHeight: (int)loader.TreeHeight);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(ProofValidationErrorType.ScoreAboveTarget, result.Error!.Type);
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
    public async Task ValidateProof_WithTamperedLeafValue_RejectsProof()
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

            // Generate a valid proof
            var proof = await generator.GenerateProofAsync(
                loader,
                challenge,
                FullScanStrategy.Instance);

            Assert.NotNull(proof);

            // Tamper with the leaf value
            var tamperedLeaf = (byte[])proof!.LeafValue.Clone();
            tamperedLeaf[0] ^= 0xFF; // Flip bits

            var tamperedProof = new Proof(
                tamperedLeaf,
                proof.LeafIndex,
                proof.SiblingHashes,
                proof.OrientationBits,
                proof.MerkleRoot,
                proof.Challenge,
                proof.Score); // Keep original score - will cause mismatch

            // Act - validate the tampered proof
            var result = _validator.ValidateProof(
                tamperedProof,
                challenge,
                loader.MerkleRoot.ToArray(),
                treeHeight: (int)loader.TreeHeight);

            // Assert - should fail on score mismatch or Merkle verification
            Assert.False(result.IsValid);
            Assert.True(
                result.Error!.Type == ProofValidationErrorType.ScoreMismatch ||
                result.Error.Type == ProofValidationErrorType.InvalidMerklePath);
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
    public async Task ValidateProof_WithTamperedScore_RejectsProof()
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

            // Generate a valid proof
            var proof = await generator.GenerateProofAsync(
                loader,
                challenge,
                FullScanStrategy.Instance);

            Assert.NotNull(proof);

            // Tamper with the score to make it artificially better
            var tamperedScore = new byte[32]; // All zeros = best possible score
            
            var tamperedProof = new Proof(
                proof!.LeafValue,
                proof.LeafIndex,
                proof.SiblingHashes,
                proof.OrientationBits,
                proof.MerkleRoot,
                proof.Challenge,
                tamperedScore);

            // Act - validate the tampered proof
            var result = _validator.ValidateProof(
                tamperedProof,
                challenge,
                loader.MerkleRoot.ToArray(),
                treeHeight: (int)loader.TreeHeight);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(ProofValidationErrorType.ScoreMismatch, result.Error!.Type);
            Assert.Contains("Score mismatch", result.ErrorMessage);
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
    public async Task ValidateProof_WithWrongChallenge_RejectsProof()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var wrongChallenge = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create a small plot
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Generate a proof with the correct challenge
            var proof = await generator.GenerateProofAsync(
                loader,
                challenge,
                FullScanStrategy.Instance);

            Assert.NotNull(proof);

            // Act - try to validate with a different challenge
            var result = _validator.ValidateProof(
                proof!,
                wrongChallenge,
                loader.MerkleRoot.ToArray(),
                treeHeight: (int)loader.TreeHeight);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(ProofValidationErrorType.ChallengeMismatch, result.Error!.Type);
            Assert.Contains("Challenge mismatch", result.ErrorMessage);
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
    public async Task ValidateProof_WithWrongPlotRoot_RejectsProof()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var wrongPlotRoot = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create a small plot
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Generate a valid proof
            var proof = await generator.GenerateProofAsync(
                loader,
                challenge,
                FullScanStrategy.Instance);

            Assert.NotNull(proof);

            // Act - try to validate with a different plot root
            var result = _validator.ValidateProof(
                proof!,
                challenge,
                wrongPlotRoot,
                treeHeight: (int)loader.TreeHeight);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(ProofValidationErrorType.PlotRootMismatch, result.Error!.Type);
            Assert.Contains("Plot root mismatch", result.ErrorMessage);
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
    public async Task ValidateProof_WithTamperedMerklePath_RejectsProof()
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
            // Create a plot with multiple leaves to have a real Merkle path
            var config = new PlotConfiguration(
                PlotConfiguration.MinPlotSize * 2, // More leaves = deeper tree
                minerKey,
                plotSeed,
                outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Generate a valid proof
            var proof = await generator.GenerateProofAsync(
                loader,
                challenge,
                FullScanStrategy.Instance);

            Assert.NotNull(proof);

            // Only tamper if there's a Merkle path to tamper with
            if (proof!.SiblingHashes.Count > 0)
            {
                // Tamper with a sibling hash
                var tamperedSiblings = proof.SiblingHashes.ToList();
                tamperedSiblings[0] = RandomNumberGenerator.GetBytes(32);

                var tamperedProof = new Proof(
                    proof.LeafValue,
                    proof.LeafIndex,
                    tamperedSiblings,
                    proof.OrientationBits,
                    proof.MerkleRoot,
                    proof.Challenge,
                    proof.Score);

                // Act - validate the tampered proof
                var result = _validator.ValidateProof(
                    tamperedProof,
                    challenge,
                    loader.MerkleRoot.ToArray(),
                    treeHeight: (int)loader.TreeHeight);

                // Assert
                Assert.False(result.IsValid);
                Assert.Equal(ProofValidationErrorType.InvalidMerklePath, result.Error!.Type);
                Assert.Contains("Merkle proof verification failed", result.ErrorMessage);
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
    public async Task ValidateProof_MultipleProofsFromSamePlot_AllValidate()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create a plot
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Generate proofs for different challenges
            var challenge1 = RandomNumberGenerator.GetBytes(32);
            var challenge2 = RandomNumberGenerator.GetBytes(32);
            var challenge3 = RandomNumberGenerator.GetBytes(32);

            var proof1 = await generator.GenerateProofAsync(loader, challenge1, FullScanStrategy.Instance);
            var proof2 = await generator.GenerateProofAsync(loader, challenge2, FullScanStrategy.Instance);
            var proof3 = await generator.GenerateProofAsync(loader, challenge3, FullScanStrategy.Instance);

            Assert.NotNull(proof1);
            Assert.NotNull(proof2);
            Assert.NotNull(proof3);

            // Act - validate all proofs
            var result1 = _validator.ValidateProof(proof1!, challenge1, loader.MerkleRoot.ToArray(), treeHeight: (int)loader.TreeHeight);
            var result2 = _validator.ValidateProof(proof2!, challenge2, loader.MerkleRoot.ToArray(), treeHeight: (int)loader.TreeHeight);
            var result3 = _validator.ValidateProof(proof3!, challenge3, loader.MerkleRoot.ToArray(), treeHeight: (int)loader.TreeHeight);

            // Assert
            Assert.True(result1.IsValid, $"Proof 1 should be valid: {result1.ErrorMessage}");
            Assert.True(result2.IsValid, $"Proof 2 should be valid: {result2.ErrorMessage}");
            Assert.True(result3.IsValid, $"Proof 3 should be valid: {result3.ErrorMessage}");
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
