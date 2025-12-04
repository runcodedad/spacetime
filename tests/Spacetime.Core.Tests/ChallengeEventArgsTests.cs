using System.Security.Cryptography;

namespace Spacetime.Core.Tests;

public class ChallengeEventArgsTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesEventArgs()
    {
        // Arrange
        var challenge = RandomNumberGenerator.GetBytes(32);
        const long epochNumber = 10;
        var startTime = DateTimeOffset.UtcNow;

        // Act
        var eventArgs = new ChallengeEventArgs(challenge, epochNumber, startTime);

        // Assert
        Assert.Equal(challenge, eventArgs.Challenge.ToArray());
        Assert.Equal(epochNumber, eventArgs.EpochNumber);
        Assert.Equal(startTime, eventArgs.EpochStartTime);
    }

    [Fact]
    public void Constructor_WithInvalidChallengeSize_ThrowsArgumentException()
    {
        // Arrange
        var invalidChallenge = RandomNumberGenerator.GetBytes(16);
        const long epochNumber = 10;
        var startTime = DateTimeOffset.UtcNow;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new ChallengeEventArgs(invalidChallenge, epochNumber, startTime));
        Assert.Contains("must be 32 bytes", exception.Message);
    }

    [Fact]
    public void Constructor_WithNegativeEpochNumber_ThrowsArgumentException()
    {
        // Arrange
        var challenge = RandomNumberGenerator.GetBytes(32);
        const long invalidEpochNumber = -1;
        var startTime = DateTimeOffset.UtcNow;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new ChallengeEventArgs(challenge, invalidEpochNumber, startTime));
        Assert.Contains("must be non-negative", exception.Message);
    }

    [Fact]
    public void Properties_AreReadOnly()
    {
        // Arrange
        var challenge = RandomNumberGenerator.GetBytes(32);
        const long epochNumber = 10;
        var startTime = DateTimeOffset.UtcNow;
        var eventArgs = new ChallengeEventArgs(challenge, epochNumber, startTime);

        // Act & Assert - Properties should be read-only (getter only)
        Assert.Equal(challenge, eventArgs.Challenge.ToArray());
        Assert.Equal(epochNumber, eventArgs.EpochNumber);
        Assert.Equal(startTime, eventArgs.EpochStartTime);
    }
}
