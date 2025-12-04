using System.Security.Cryptography;

namespace Spacetime.Core.Tests;

public class ChallengeDerivationTests
{
    [Fact]
    public void DeriveChallenge_WithValidInputs_ReturnsChallenge()
    {
        // Arrange
        var blockHash = RandomNumberGenerator.GetBytes(32);
        const long epochNumber = 10;

        // Act
        var challenge = ChallengeDerivation.DeriveChallenge(blockHash, epochNumber);

        // Assert
        Assert.NotNull(challenge);
        Assert.Equal(ChallengeDerivation.ChallengeSize, challenge.Length);
    }

    [Fact]
    public void DeriveChallenge_WithSameInputs_ReturnsSameChallenge()
    {
        // Arrange
        var blockHash = RandomNumberGenerator.GetBytes(32);
        const long epochNumber = 10;

        // Act
        var challenge1 = ChallengeDerivation.DeriveChallenge(blockHash, epochNumber);
        var challenge2 = ChallengeDerivation.DeriveChallenge(blockHash, epochNumber);

        // Assert
        Assert.Equal(challenge1, challenge2);
    }

    [Fact]
    public void DeriveChallenge_WithDifferentBlockHash_ReturnsDifferentChallenge()
    {
        // Arrange
        var blockHash1 = RandomNumberGenerator.GetBytes(32);
        var blockHash2 = RandomNumberGenerator.GetBytes(32);
        const long epochNumber = 10;

        // Act
        var challenge1 = ChallengeDerivation.DeriveChallenge(blockHash1, epochNumber);
        var challenge2 = ChallengeDerivation.DeriveChallenge(blockHash2, epochNumber);

        // Assert
        Assert.NotEqual(challenge1, challenge2);
    }

    [Fact]
    public void DeriveChallenge_WithDifferentEpochNumber_ReturnsDifferentChallenge()
    {
        // Arrange
        var blockHash = RandomNumberGenerator.GetBytes(32);

        // Act
        var challenge1 = ChallengeDerivation.DeriveChallenge(blockHash, 10);
        var challenge2 = ChallengeDerivation.DeriveChallenge(blockHash, 11);

        // Assert
        Assert.NotEqual(challenge1, challenge2);
    }

    [Fact]
    public void DeriveChallenge_WithInvalidHashSize_ThrowsArgumentException()
    {
        // Arrange
        var invalidHash = RandomNumberGenerator.GetBytes(16);
        const long epochNumber = 10;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ChallengeDerivation.DeriveChallenge(invalidHash, epochNumber));
        Assert.Contains("must be 32 bytes", exception.Message);
    }

    [Fact]
    public void DeriveChallenge_WithNegativeEpochNumber_ThrowsArgumentException()
    {
        // Arrange
        var blockHash = RandomNumberGenerator.GetBytes(32);
        const long invalidEpochNumber = -1;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ChallengeDerivation.DeriveChallenge(blockHash, invalidEpochNumber));
        Assert.Contains("must be non-negative", exception.Message);
    }

    [Fact]
    public void DeriveGenesisChallenge_WithValidNetworkId_ReturnsChallenge()
    {
        // Arrange
        const string networkId = "mainnet";

        // Act
        var challenge = ChallengeDerivation.DeriveGenesisChallenge(networkId);

        // Assert
        Assert.NotNull(challenge);
        Assert.Equal(ChallengeDerivation.ChallengeSize, challenge.Length);
    }

    [Fact]
    public void DeriveGenesisChallenge_WithSameNetworkId_ReturnsSameChallenge()
    {
        // Arrange
        const string networkId = "mainnet";

        // Act
        var challenge1 = ChallengeDerivation.DeriveGenesisChallenge(networkId);
        var challenge2 = ChallengeDerivation.DeriveGenesisChallenge(networkId);

        // Assert
        Assert.Equal(challenge1, challenge2);
    }

    [Fact]
    public void DeriveGenesisChallenge_WithDifferentNetworkId_ReturnsDifferentChallenge()
    {
        // Act
        var mainnetChallenge = ChallengeDerivation.DeriveGenesisChallenge("mainnet");
        var testnetChallenge = ChallengeDerivation.DeriveGenesisChallenge("testnet");

        // Assert
        Assert.NotEqual(mainnetChallenge, testnetChallenge);
    }

    [Fact]
    public void DeriveGenesisChallenge_WithNullNetworkId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ChallengeDerivation.DeriveGenesisChallenge(null!));
    }

    [Fact]
    public void DeriveGenesisChallenge_WithEmptyNetworkId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ChallengeDerivation.DeriveGenesisChallenge(""));
        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public void DeriveGenesisChallenge_WithWhitespaceNetworkId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ChallengeDerivation.DeriveGenesisChallenge("   "));
        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public void VerifyChallenge_WithValidChallenge_ReturnsTrue()
    {
        // Arrange
        var blockHash = RandomNumberGenerator.GetBytes(32);
        const long epochNumber = 10;
        var expectedChallenge = ChallengeDerivation.DeriveChallenge(blockHash, epochNumber);

        // Act
        var isValid = ChallengeDerivation.VerifyChallenge(expectedChallenge, blockHash, epochNumber);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyChallenge_WithInvalidChallenge_ReturnsFalse()
    {
        // Arrange
        var blockHash = RandomNumberGenerator.GetBytes(32);
        const long epochNumber = 10;
        var wrongChallenge = RandomNumberGenerator.GetBytes(32);

        // Act
        var isValid = ChallengeDerivation.VerifyChallenge(wrongChallenge, blockHash, epochNumber);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifyChallenge_WithWrongEpochNumber_ReturnsFalse()
    {
        // Arrange
        var blockHash = RandomNumberGenerator.GetBytes(32);
        var challenge = ChallengeDerivation.DeriveChallenge(blockHash, 10);

        // Act
        var isValid = ChallengeDerivation.VerifyChallenge(challenge, blockHash, 11);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifyChallenge_WithInvalidChallengeSize_ThrowsArgumentException()
    {
        // Arrange
        var blockHash = RandomNumberGenerator.GetBytes(32);
        var invalidChallenge = RandomNumberGenerator.GetBytes(16);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ChallengeDerivation.VerifyChallenge(invalidChallenge, blockHash, 10));
        Assert.Contains("must be 32 bytes", exception.Message);
    }

    [Fact]
    public void VerifyGenesisChallenge_WithValidChallenge_ReturnsTrue()
    {
        // Arrange
        const string networkId = "mainnet";
        var expectedChallenge = ChallengeDerivation.DeriveGenesisChallenge(networkId);

        // Act
        var isValid = ChallengeDerivation.VerifyGenesisChallenge(expectedChallenge, networkId);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyGenesisChallenge_WithInvalidChallenge_ReturnsFalse()
    {
        // Arrange
        const string networkId = "mainnet";
        var wrongChallenge = RandomNumberGenerator.GetBytes(32);

        // Act
        var isValid = ChallengeDerivation.VerifyGenesisChallenge(wrongChallenge, networkId);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifyGenesisChallenge_WithWrongNetworkId_ReturnsFalse()
    {
        // Arrange
        var mainnetChallenge = ChallengeDerivation.DeriveGenesisChallenge("mainnet");

        // Act
        var isValid = ChallengeDerivation.VerifyGenesisChallenge(mainnetChallenge, "testnet");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifyGenesisChallenge_WithInvalidChallengeSize_ThrowsArgumentException()
    {
        // Arrange
        var invalidChallenge = RandomNumberGenerator.GetBytes(16);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ChallengeDerivation.VerifyGenesisChallenge(invalidChallenge, "mainnet"));
        Assert.Contains("must be 32 bytes", exception.Message);
    }
}
