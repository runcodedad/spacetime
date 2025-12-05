using System.Security.Cryptography;
using MerkleTree.Hashing;
using Spacetime.Plotting;

namespace Spacetime.Consensus.Tests;

/// <summary>
/// Unit tests for the ProofValidator class.
/// </summary>
public class ProofValidatorTests
{
    private readonly IHashFunction _hashFunction = new Sha256HashFunction();
    private readonly ProofValidator _validator;

    public ProofValidatorTests()
    {
        _validator = new ProofValidator(_hashFunction);
    }

    #region ComputeScore Tests

    [Fact]
    public void ComputeScore_WithValidInputs_ReturnsCorrectScore()
    {
        // Arrange
        var challenge = RandomNumberGenerator.GetBytes(32);
        var leafValue = RandomNumberGenerator.GetBytes(32);

        // Act
        var score = _validator.ComputeScore(challenge, leafValue);

        // Assert
        Assert.NotNull(score);
        Assert.Equal(32, score.Length);

        // Verify score = H(challenge || leaf)
        var input = new byte[challenge.Length + leafValue.Length];
        challenge.CopyTo(input.AsSpan());
        leafValue.CopyTo(input.AsSpan(challenge.Length));
        var expectedScore = SHA256.HashData(input);
        
        Assert.Equal(expectedScore, score);
    }

    [Fact]
    public void ComputeScore_WithSameInputs_ReturnsSameScore()
    {
        // Arrange
        var challenge = RandomNumberGenerator.GetBytes(32);
        var leafValue = RandomNumberGenerator.GetBytes(32);

        // Act
        var score1 = _validator.ComputeScore(challenge, leafValue);
        var score2 = _validator.ComputeScore(challenge, leafValue);

        // Assert
        Assert.Equal(score1, score2);
    }

    [Fact]
    public void ComputeScore_WithDifferentChallenges_ReturnsDifferentScores()
    {
        // Arrange
        var challenge1 = RandomNumberGenerator.GetBytes(32);
        var challenge2 = RandomNumberGenerator.GetBytes(32);
        var leafValue = RandomNumberGenerator.GetBytes(32);

        // Act
        var score1 = _validator.ComputeScore(challenge1, leafValue);
        var score2 = _validator.ComputeScore(challenge2, leafValue);

        // Assert
        Assert.NotEqual(score1, score2);
    }

    [Fact]
    public void ComputeScore_WithDifferentLeaves_ReturnsDifferentScores()
    {
        // Arrange
        var challenge = RandomNumberGenerator.GetBytes(32);
        var leafValue1 = RandomNumberGenerator.GetBytes(32);
        var leafValue2 = RandomNumberGenerator.GetBytes(32);

        // Act
        var score1 = _validator.ComputeScore(challenge, leafValue1);
        var score2 = _validator.ComputeScore(challenge, leafValue2);

        // Assert
        Assert.NotEqual(score1, score2);
    }

    [Fact]
    public void ComputeScore_WithNullChallenge_ThrowsArgumentNullException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _validator.ComputeScore(null!, leafValue));
    }

    [Fact]
    public void ComputeScore_WithNullLeafValue_ThrowsArgumentNullException()
    {
        // Arrange
        var challenge = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _validator.ComputeScore(challenge, null!));
    }

    [Fact]
    public void ComputeScore_WithInvalidChallengeSize_ThrowsArgumentException()
    {
        // Arrange
        var challenge = RandomNumberGenerator.GetBytes(16); // Wrong size
        var leafValue = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _validator.ComputeScore(challenge, leafValue));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void ComputeScore_WithInvalidLeafValueSize_ThrowsArgumentException()
    {
        // Arrange
        var challenge = RandomNumberGenerator.GetBytes(32);
        var leafValue = RandomNumberGenerator.GetBytes(16); // Wrong size

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _validator.ComputeScore(challenge, leafValue));
        Assert.Contains("32 bytes", exception.Message);
    }

    #endregion

    #region IsScoreBelowTarget Tests

    [Fact]
    public void IsScoreBelowTarget_WithScoreLessThanTarget_ReturnsTrue()
    {
        // Arrange - create score that's clearly less than target
        var score = new byte[32];
        score[0] = 0x01;
        
        var target = new byte[32];
        target[0] = 0xFF;

        // Act
        var result = ProofValidator.IsScoreBelowTarget(score, target);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsScoreBelowTarget_WithScoreEqualToTarget_ReturnsFalse()
    {
        // Arrange
        var score = RandomNumberGenerator.GetBytes(32);
        var target = (byte[])score.Clone();

        // Act
        var result = ProofValidator.IsScoreBelowTarget(score, target);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsScoreBelowTarget_WithScoreGreaterThanTarget_ReturnsFalse()
    {
        // Arrange - create score that's clearly greater than target
        var score = new byte[32];
        score[0] = 0xFF;
        
        var target = new byte[32];
        target[0] = 0x01;

        // Act
        var result = ProofValidator.IsScoreBelowTarget(score, target);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsScoreBelowTarget_WithAllZerosScore_ReturnsTrue()
    {
        // Arrange - all zeros is the lowest possible score
        var score = new byte[32]; // All zeros
        
        var target = new byte[32];
        target[31] = 0x01; // Any non-zero target

        // Act
        var result = ProofValidator.IsScoreBelowTarget(score, target);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsScoreBelowTarget_WithMaxValueScore_ReturnsFalse()
    {
        // Arrange - all FFs is the highest possible score
        var score = new byte[32];
        Array.Fill(score, (byte)0xFF);
        
        var target = new byte[32];
        Array.Fill(target, (byte)0xFF);

        // Act
        var result = ProofValidator.IsScoreBelowTarget(score, target);

        // Assert
        Assert.False(result); // Equal, not less than
    }

    [Fact]
    public void IsScoreBelowTarget_WithNullScore_ThrowsArgumentNullException()
    {
        // Arrange
        var target = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ProofValidator.IsScoreBelowTarget(null!, target));
    }

    [Fact]
    public void IsScoreBelowTarget_WithNullTarget_ThrowsArgumentNullException()
    {
        // Arrange
        var score = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ProofValidator.IsScoreBelowTarget(score, null!));
    }

    [Fact]
    public void IsScoreBelowTarget_WithInvalidScoreSize_ThrowsArgumentException()
    {
        // Arrange
        var score = RandomNumberGenerator.GetBytes(16); // Wrong size
        var target = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ProofValidator.IsScoreBelowTarget(score, target));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void IsScoreBelowTarget_WithInvalidTargetSize_ThrowsArgumentException()
    {
        // Arrange
        var score = RandomNumberGenerator.GetBytes(32);
        var target = RandomNumberGenerator.GetBytes(16); // Wrong size

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ProofValidator.IsScoreBelowTarget(score, target));
        Assert.Contains("32 bytes", exception.Message);
    }

    #endregion

    #region ValidateProof Tests

    [Fact]
    public void ValidateProof_WithValidProof_ReturnsSuccess()
    {
        // Arrange - create a valid proof with correct Merkle path
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = _validator.ComputeScore(challenge, leafValue);
        
        // Create a simple single-leaf Merkle tree (height 0)
        var merkleRoot = SHA256.HashData(leafValue);
        
        var proof = new Proof(
            leafValue: leafValue,
            leafIndex: 0,
            siblingHashes: Array.Empty<byte[]>(),
            orientationBits: Array.Empty<bool>(),
            merkleRoot: merkleRoot,
            challenge: challenge,
            score: score);

        // Act
        var result = _validator.ValidateProof(proof, challenge, merkleRoot);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateProof_WithChallengeMismatch_ReturnsFailure()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var expectedChallenge = RandomNumberGenerator.GetBytes(32);
        var score = _validator.ComputeScore(challenge, leafValue);
        var merkleRoot = SHA256.HashData(leafValue);
        
        var proof = new Proof(
            leafValue: leafValue,
            leafIndex: 0,
            siblingHashes: Array.Empty<byte[]>(),
            orientationBits: Array.Empty<bool>(),
            merkleRoot: merkleRoot,
            challenge: challenge,
            score: score);

        // Act
        var result = _validator.ValidateProof(proof, expectedChallenge, merkleRoot);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
        Assert.Equal(ProofValidationErrorType.ChallengeMismatch, result.Error!.Type);
        Assert.Contains("Challenge mismatch", result.ErrorMessage);
    }

    [Fact]
    public void ValidateProof_WithPlotRootMismatch_ReturnsFailure()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = _validator.ComputeScore(challenge, leafValue);
        var merkleRoot = SHA256.HashData(leafValue);
        var expectedPlotRoot = RandomNumberGenerator.GetBytes(32);
        
        var proof = new Proof(
            leafValue: leafValue,
            leafIndex: 0,
            siblingHashes: Array.Empty<byte[]>(),
            orientationBits: Array.Empty<bool>(),
            merkleRoot: merkleRoot,
            challenge: challenge,
            score: score);

        // Act
        var result = _validator.ValidateProof(proof, challenge, expectedPlotRoot);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
        Assert.Equal(ProofValidationErrorType.PlotRootMismatch, result.Error!.Type);
        Assert.Contains("Plot root mismatch", result.ErrorMessage);
    }

    [Fact]
    public void ValidateProof_WithScoreMismatch_ReturnsFailure()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var incorrectScore = RandomNumberGenerator.GetBytes(32); // Not the real score
        var merkleRoot = SHA256.HashData(leafValue);
        
        var proof = new Proof(
            leafValue: leafValue,
            leafIndex: 0,
            siblingHashes: Array.Empty<byte[]>(),
            orientationBits: Array.Empty<bool>(),
            merkleRoot: merkleRoot,
            challenge: challenge,
            score: incorrectScore);

        // Act
        var result = _validator.ValidateProof(proof, challenge, merkleRoot);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
        Assert.Equal(ProofValidationErrorType.ScoreMismatch, result.Error!.Type);
        Assert.Contains("Score mismatch", result.ErrorMessage);
    }

    [Fact]
    public void ValidateProof_WithScoreAboveTarget_ReturnsFailure()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = _validator.ComputeScore(challenge, leafValue);
        var merkleRoot = SHA256.HashData(leafValue);
        
        // Create a difficulty target that's below the score
        var difficultyTarget = new byte[32];
        difficultyTarget[0] = 0x01; // Very low target
        
        var proof = new Proof(
            leafValue: leafValue,
            leafIndex: 0,
            siblingHashes: Array.Empty<byte[]>(),
            orientationBits: Array.Empty<bool>(),
            merkleRoot: merkleRoot,
            challenge: challenge,
            score: score);

        // Act
        var result = _validator.ValidateProof(proof, challenge, merkleRoot, difficultyTarget);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
        Assert.Equal(ProofValidationErrorType.ScoreAboveTarget, result.Error!.Type);
        Assert.Contains("does not meet difficulty target", result.ErrorMessage);
    }

    [Fact]
    public void ValidateProof_WithScoreBelowTarget_PassesTargetCheck()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = _validator.ComputeScore(challenge, leafValue);
        var merkleRoot = SHA256.HashData(leafValue);
        
        // Create a difficulty target that's above the score
        var difficultyTarget = new byte[32];
        Array.Fill(difficultyTarget, (byte)0xFF); // Very high target
        
        var proof = new Proof(
            leafValue: leafValue,
            leafIndex: 0,
            siblingHashes: Array.Empty<byte[]>(),
            orientationBits: Array.Empty<bool>(),
            merkleRoot: merkleRoot,
            challenge: challenge,
            score: score);

        // Act
        var result = _validator.ValidateProof(proof, challenge, merkleRoot, difficultyTarget);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ValidateProof_WithInvalidMerklePath_ReturnsFailure()
    {
        // Arrange - create proof with incorrect Merkle path
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = _validator.ComputeScore(challenge, leafValue);
        
        // Create a real Merkle root (hash of leaf)
        var merkleRoot = SHA256.HashData(leafValue);
        
        // But provide incorrect sibling hashes
        var invalidSibling = RandomNumberGenerator.GetBytes(32);
        
        var proof = new Proof(
            leafValue: leafValue,
            leafIndex: 0,
            siblingHashes: new[] { invalidSibling },
            orientationBits: new[] { false },
            merkleRoot: merkleRoot,
            challenge: challenge,
            score: score);

        // Act
        var result = _validator.ValidateProof(proof, challenge, merkleRoot, treeHeight: 1);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
        Assert.Equal(ProofValidationErrorType.InvalidMerklePath, result.Error!.Type);
        Assert.Contains("Merkle proof verification failed", result.ErrorMessage);
    }

    [Fact]
    public void ValidateProof_WithNullProof_ThrowsArgumentNullException()
    {
        // Arrange
        var challenge = RandomNumberGenerator.GetBytes(32);
        var plotRoot = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _validator.ValidateProof(null!, challenge, plotRoot));
    }

    [Fact]
    public void ValidateProof_WithNullExpectedChallenge_ThrowsArgumentNullException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = _validator.ComputeScore(challenge, leafValue);
        var merkleRoot = SHA256.HashData(leafValue);
        
        var proof = new Proof(
            leafValue: leafValue,
            leafIndex: 0,
            siblingHashes: Array.Empty<byte[]>(),
            orientationBits: Array.Empty<bool>(),
            merkleRoot: merkleRoot,
            challenge: challenge,
            score: score);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _validator.ValidateProof(proof, null!, merkleRoot));
    }

    [Fact]
    public void ValidateProof_WithNullExpectedPlotRoot_ThrowsArgumentNullException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = _validator.ComputeScore(challenge, leafValue);
        var merkleRoot = SHA256.HashData(leafValue);
        
        var proof = new Proof(
            leafValue: leafValue,
            leafIndex: 0,
            siblingHashes: Array.Empty<byte[]>(),
            orientationBits: Array.Empty<bool>(),
            merkleRoot: merkleRoot,
            challenge: challenge,
            score: score);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _validator.ValidateProof(proof, challenge, null!));
    }

    [Fact]
    public void ValidateProof_WithInvalidExpectedChallengeSize_ThrowsArgumentException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = _validator.ComputeScore(challenge, leafValue);
        var merkleRoot = SHA256.HashData(leafValue);
        var invalidChallenge = RandomNumberGenerator.GetBytes(16);
        
        var proof = new Proof(
            leafValue: leafValue,
            leafIndex: 0,
            siblingHashes: Array.Empty<byte[]>(),
            orientationBits: Array.Empty<bool>(),
            merkleRoot: merkleRoot,
            challenge: challenge,
            score: score);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            _validator.ValidateProof(proof, invalidChallenge, merkleRoot));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void ValidateProof_ValidationChecksInCorrectOrder()
    {
        // Arrange - create proof with multiple errors
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var wrongChallenge = RandomNumberGenerator.GetBytes(32);
        var wrongScore = RandomNumberGenerator.GetBytes(32);
        var merkleRoot = SHA256.HashData(leafValue);
        var wrongPlotRoot = RandomNumberGenerator.GetBytes(32);
        
        var proof = new Proof(
            leafValue: leafValue,
            leafIndex: 0,
            siblingHashes: Array.Empty<byte[]>(),
            orientationBits: Array.Empty<bool>(),
            merkleRoot: merkleRoot,
            challenge: challenge,
            score: wrongScore);

        // Act - should fail on challenge check first
        var result = _validator.ValidateProof(proof, wrongChallenge, wrongPlotRoot);

        // Assert - first error should be challenge mismatch
        Assert.False(result.IsValid);
        Assert.Equal(ProofValidationErrorType.ChallengeMismatch, result.Error!.Type);
    }

    #endregion

    #region ProofValidationResult Tests

    [Fact]
    public void ProofValidationResult_Success_HasCorrectProperties()
    {
        // Act
        var result = ProofValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ProofValidationResult_Failure_HasCorrectProperties()
    {
        // Arrange
        var error = new ProofValidationError(
            ProofValidationErrorType.InvalidMerklePath,
            "Test error message");

        // Act
        var result = ProofValidationResult.Failure(error);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
        Assert.Equal(error, result.Error);
        Assert.Equal("Test error message", result.ErrorMessage);
        Assert.Equal(ProofValidationErrorType.InvalidMerklePath, result.Error.Type);
    }

    [Fact]
    public void ProofValidationResult_Failure_WithNullError_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ProofValidationResult.Failure(null!));
    }

    #endregion

    #region ProofValidationError Tests

    [Fact]
    public void ProofValidationError_WithValidParameters_CreatesError()
    {
        // Arrange & Act
        var error = new ProofValidationError(
            ProofValidationErrorType.ScoreMismatch,
            "Score does not match");

        // Assert
        Assert.Equal(ProofValidationErrorType.ScoreMismatch, error.Type);
        Assert.Equal("Score does not match", error.Message);
    }

    [Fact]
    public void ProofValidationError_WithNullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ProofValidationError(ProofValidationErrorType.InvalidMerklePath, null!));
    }

    #endregion
}
