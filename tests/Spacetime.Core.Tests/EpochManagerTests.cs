using System.Security.Cryptography;

namespace Spacetime.Core.Tests;

public class EpochManagerTests
{
    private const int SmallDelayMs = 10;
    private const int MediumDelayMs = 100;
    private const int EpochExpiryWaitMs = 1100; // Slightly longer than 1 second epoch
    [Fact]
    public void Constructor_WithConfig_InitializesEpochManager()
    {
        // Arrange
        var config = new EpochConfig(10);

        // Act
        var manager = new EpochManager(config);

        // Assert
        Assert.Equal(0, manager.CurrentEpoch);
        Assert.Equal(ChallengeDerivation.ChallengeSize, manager.CurrentChallenge.Length);
        Assert.True(manager.EpochStartTime <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EpochManager(null!));
    }

    [Fact]
    public void Constructor_Parameterless_UsesDefaultConfig()
    {
        // Act
        var manager = new EpochManager();

        // Assert
        Assert.Equal(0, manager.CurrentEpoch);
        Assert.Equal(ChallengeDerivation.ChallengeSize, manager.CurrentChallenge.Length);
    }

    [Fact]
    public async Task AdvanceEpochAsync_IncrementsEpochNumber()
    {
        // Arrange
        var config = new EpochConfig(10);
        var manager = new EpochManager(config);
        var blockHash = RandomNumberGenerator.GetBytes(32);

        // Act
        await manager.AdvanceEpochAsync(blockHash);

        // Assert
        Assert.Equal(1, manager.CurrentEpoch);
    }

    [Fact]
    public async Task AdvanceEpochAsync_UpdatesChallenge()
    {
        // Arrange
        var config = new EpochConfig(10);
        var manager = new EpochManager(config);
        var blockHash = RandomNumberGenerator.GetBytes(32);
        var previousChallenge = manager.CurrentChallenge.ToArray();

        // Act
        await manager.AdvanceEpochAsync(blockHash);

        // Assert
        Assert.NotEqual(previousChallenge, manager.CurrentChallenge.ToArray());
    }

    [Fact]
    public async Task AdvanceEpochAsync_UpdatesEpochStartTime()
    {
        // Arrange
        var config = new EpochConfig(10);
        var manager = new EpochManager(config);
        var blockHash = RandomNumberGenerator.GetBytes(32);
        var previousStartTime = manager.EpochStartTime;

        // Wait a small amount to ensure time difference
        await Task.Delay(SmallDelayMs);

        // Act
        await manager.AdvanceEpochAsync(blockHash);

        // Assert
        Assert.True(manager.EpochStartTime > previousStartTime);
    }

    [Fact]
    public async Task AdvanceEpochAsync_WithInvalidHashSize_ThrowsArgumentException()
    {
        // Arrange
        var manager = new EpochManager();
        var invalidHash = RandomNumberGenerator.GetBytes(16);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            manager.AdvanceEpochAsync(invalidHash));
        Assert.Contains("must be 32 bytes", exception.Message);
    }

    [Fact]
    public async Task AdvanceEpochAsync_DerivesChallengeCorrectly()
    {
        // Arrange
        var manager = new EpochManager();
        var blockHash = RandomNumberGenerator.GetBytes(32);

        // Act
        await manager.AdvanceEpochAsync(blockHash);

        // Assert
        var expectedChallenge = ChallengeDerivation.DeriveChallenge(blockHash, 1);
        Assert.Equal(expectedChallenge, manager.CurrentChallenge.ToArray());
    }

    [Fact]
    public async Task AdvanceEpochAsync_MultipleTimes_IncrementsCorrectly()
    {
        // Arrange
        var manager = new EpochManager();
        var blockHash = RandomNumberGenerator.GetBytes(32);

        // Act
        await manager.AdvanceEpochAsync(blockHash);
        await manager.AdvanceEpochAsync(blockHash);
        await manager.AdvanceEpochAsync(blockHash);

        // Assert
        Assert.Equal(3, manager.CurrentEpoch);
    }

    [Fact]
    public void TimeRemainingInEpoch_WhenNotExpired_ReturnsPositiveTime()
    {
        // Arrange
        var config = new EpochConfig(10);
        var manager = new EpochManager(config);

        // Act
        var remaining = manager.TimeRemainingInEpoch;

        // Assert
        Assert.True(remaining.TotalSeconds > 0);
        Assert.True(remaining.TotalSeconds <= 10);
    }

    [Fact]
    public async Task TimeRemainingInEpoch_AfterDelay_Decreases()
    {
        // Arrange
        var config = new EpochConfig(10);
        var manager = new EpochManager(config);
        var initialRemaining = manager.TimeRemainingInEpoch;

        // Act
        await Task.Delay(MediumDelayMs);
        var laterRemaining = manager.TimeRemainingInEpoch;

        // Assert
        Assert.True(laterRemaining < initialRemaining);
    }

    [Fact]
    public void IsEpochExpired_ImmediatelyAfterCreation_ReturnsFalse()
    {
        // Arrange
        var config = new EpochConfig(10);
        var manager = new EpochManager(config);

        // Act
        var isExpired = manager.IsEpochExpired;

        // Assert
        Assert.False(isExpired);
    }

    [Fact]
    public async Task IsEpochExpired_AfterDurationPasses_ReturnsTrue()
    {
        // Arrange
        var config = new EpochConfig(1); // 1 second duration
        var manager = new EpochManager(config);

        // Act
        await Task.Delay(EpochExpiryWaitMs); // Wait slightly longer than epoch duration
        var isExpired = manager.IsEpochExpired;

        // Assert
        Assert.True(isExpired);
    }

    [Fact]
    public void ValidateChallengeForEpoch_WithValidChallenge_ReturnsTrue()
    {
        // Arrange
        var manager = new EpochManager();
        var blockHash = RandomNumberGenerator.GetBytes(32);
        const long epochNumber = 10;
        var challenge = ChallengeDerivation.DeriveChallenge(blockHash, epochNumber);

        // Act
        var isValid = manager.ValidateChallengeForEpoch(challenge, epochNumber, blockHash);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateChallengeForEpoch_WithInvalidChallenge_ReturnsFalse()
    {
        // Arrange
        var manager = new EpochManager();
        var blockHash = RandomNumberGenerator.GetBytes(32);
        var wrongChallenge = RandomNumberGenerator.GetBytes(32);

        // Act
        var isValid = manager.ValidateChallengeForEpoch(wrongChallenge, 10, blockHash);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateChallengeForEpoch_WithInvalidChallengeSize_ReturnsFalse()
    {
        // Arrange
        var manager = new EpochManager();
        var blockHash = RandomNumberGenerator.GetBytes(32);
        var invalidChallenge = RandomNumberGenerator.GetBytes(16);

        // Act
        var isValid = manager.ValidateChallengeForEpoch(invalidChallenge, 10, blockHash);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateChallengeForEpoch_WithInvalidHashSize_ReturnsFalse()
    {
        // Arrange
        var manager = new EpochManager();
        var invalidHash = RandomNumberGenerator.GetBytes(16);
        var challenge = RandomNumberGenerator.GetBytes(32);

        // Act
        var isValid = manager.ValidateChallengeForEpoch(challenge, 10, invalidHash);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateChallengeForEpoch_WithNegativeEpochNumber_ReturnsFalse()
    {
        // Arrange
        var manager = new EpochManager();
        var blockHash = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);

        // Act
        var isValid = manager.ValidateChallengeForEpoch(challenge, -1, blockHash);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Reset_WithValidParameters_ResetsState()
    {
        // Arrange
        var manager = new EpochManager();
        var challenge = RandomNumberGenerator.GetBytes(32);
        const long epochNumber = 5;
        var startTime = DateTimeOffset.UtcNow.AddMinutes(-1);

        // Act
        manager.Reset(epochNumber, challenge, startTime);

        // Assert
        Assert.Equal(epochNumber, manager.CurrentEpoch);
        Assert.Equal(challenge, manager.CurrentChallenge.ToArray());
        Assert.Equal(startTime, manager.EpochStartTime);
    }

    [Fact]
    public void Reset_WithNegativeEpochNumber_ThrowsArgumentException()
    {
        // Arrange
        var manager = new EpochManager();
        var challenge = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            manager.Reset(-1, challenge, DateTimeOffset.UtcNow));
        Assert.Contains("must be non-negative", exception.Message);
    }

    [Fact]
    public void Reset_WithInvalidChallengeSize_ThrowsArgumentException()
    {
        // Arrange
        var manager = new EpochManager();
        var invalidChallenge = RandomNumberGenerator.GetBytes(16);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            manager.Reset(5, invalidChallenge, DateTimeOffset.UtcNow));
        Assert.Contains("must be 32 bytes", exception.Message);
    }

    [Fact]
    public async Task ConcurrentAdvanceEpoch_ThreadSafe()
    {
        // Arrange
        var manager = new EpochManager();
        var blockHash = RandomNumberGenerator.GetBytes(32);
        var tasks = new List<Task>();

        // Act
        for (var i = 0; i < 10; i++)
        {
            tasks.Add(manager.AdvanceEpochAsync(blockHash));
        }
        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, manager.CurrentEpoch);
    }

    [Fact]
    public async Task ConcurrentPropertyAccess_ThreadSafe()
    {
        // Arrange
        var manager = new EpochManager();
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();

        // Act
        for (var i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var _ = manager.CurrentEpoch;
                    var __ = manager.CurrentChallenge;
                    var ___ = manager.EpochStartTime;
                    var ____ = manager.TimeRemainingInEpoch;
                    var _____ = manager.IsEpochExpired;
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }
        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
    }
}
