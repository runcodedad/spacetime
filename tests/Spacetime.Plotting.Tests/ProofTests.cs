using System.Security.Cryptography;

namespace Spacetime.Plotting.Tests;

public class ProofTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesProof()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var leafIndex = 42L;
        var siblingHashes = new[] { RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32) };
        var orientationBits = new[] { true, false };
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = RandomNumberGenerator.GetBytes(32);

        // Act
        var proof = new Proof(
            leafValue,
            leafIndex,
            siblingHashes,
            orientationBits,
            merkleRoot,
            challenge,
            score);

        // Assert
        Assert.NotNull(proof);
        Assert.Equal(leafValue, proof.LeafValue);
        Assert.Equal(leafIndex, proof.LeafIndex);
        Assert.Equal(siblingHashes, proof.SiblingHashes);
        Assert.Equal(orientationBits, proof.OrientationBits);
        Assert.Equal(merkleRoot, proof.MerkleRoot);
        Assert.Equal(challenge, proof.Challenge);
        Assert.Equal(score, proof.Score);
    }

    [Fact]
    public void Constructor_WithNullLeafValue_ThrowsArgumentNullException()
    {
        // Arrange
        var siblingHashes = new[] { RandomNumberGenerator.GetBytes(32) };
        var orientationBits = new[] { true };
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Proof(null!, 0, siblingHashes, orientationBits, merkleRoot, challenge, score));
    }

    [Fact]
    public void Constructor_WithInvalidLeafValueSize_ThrowsArgumentException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(16); // Wrong size
        var siblingHashes = new[] { RandomNumberGenerator.GetBytes(32) };
        var orientationBits = new[] { true };
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Proof(leafValue, 0, siblingHashes, orientationBits, merkleRoot, challenge, score));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void Constructor_WithNegativeLeafIndex_ThrowsArgumentException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var siblingHashes = new[] { RandomNumberGenerator.GetBytes(32) };
        var orientationBits = new[] { true };
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Proof(leafValue, -1, siblingHashes, orientationBits, merkleRoot, challenge, score));
        Assert.Contains("non-negative", exception.Message);
    }

    [Fact]
    public void Constructor_WithMismatchedSiblingHashesAndOrientationBits_ThrowsArgumentException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var siblingHashes = new[] { RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32) };
        var orientationBits = new[] { true }; // Mismatched count
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Proof(leafValue, 0, siblingHashes, orientationBits, merkleRoot, challenge, score));
        Assert.Contains("same count", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidSiblingHashSize_ThrowsArgumentException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var siblingHashes = new[] { RandomNumberGenerator.GetBytes(16) }; // Wrong size
        var orientationBits = new[] { true };
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Proof(leafValue, 0, siblingHashes, orientationBits, merkleRoot, challenge, score));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidChallengeSize_ThrowsArgumentException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var siblingHashes = new[] { RandomNumberGenerator.GetBytes(32) };
        var orientationBits = new[] { true };
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(16); // Wrong size
        var score = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Proof(leafValue, 0, siblingHashes, orientationBits, merkleRoot, challenge, score));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidScoreSize_ThrowsArgumentException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var siblingHashes = new[] { RandomNumberGenerator.GetBytes(32) };
        var orientationBits = new[] { true };
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var score = RandomNumberGenerator.GetBytes(16); // Wrong size

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Proof(leafValue, 0, siblingHashes, orientationBits, merkleRoot, challenge, score));
        Assert.Contains("32 bytes", exception.Message);
    }
}
