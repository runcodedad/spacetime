using System.Security.Cryptography;

namespace Spacetime.Core.Tests;

public class GenesisConfigTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesConfig()
    {
        // Arrange & Act
        var config = new GenesisConfig
        {
            NetworkId = "test-network",
            InitialTimestamp = 1000,
            InitialDifficulty = 5000,
            InitialEpoch = 0,
            EpochDurationSeconds = 30,
            TargetBlockTime = 30,
            PreminedAllocations = new Dictionary<string, long>()
        };

        // Assert
        Assert.Equal("test-network", config.NetworkId);
        Assert.Equal(1000, config.InitialTimestamp);
        Assert.Equal(5000, config.InitialDifficulty);
        Assert.Equal(0, config.InitialEpoch);
        Assert.Equal(30, config.EpochDurationSeconds);
        Assert.Equal(30, config.TargetBlockTime);
        Assert.NotNull(config.PreminedAllocations);
        Assert.Empty(config.PreminedAllocations);
    }

    [Fact]
    public void Constructor_WithPremine_CreatesConfig()
    {
        // Arrange
        var publicKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(33));
        var premine = new Dictionary<string, long> { [publicKey] = 1000000 };

        // Act
        var config = new GenesisConfig
        {
            NetworkId = "test-network",
            InitialTimestamp = 1000,
            InitialDifficulty = 5000,
            InitialEpoch = 0,
            EpochDurationSeconds = 30,
            TargetBlockTime = 30,
            PreminedAllocations = premine
        };

        // Assert
        Assert.Single(config.PreminedAllocations);
        Assert.Equal(1000000, config.PreminedAllocations[publicKey]);
    }

    [Fact]
    public void Validate_WithValidConfig_DoesNotThrow()
    {
        // Arrange
        var config = new GenesisConfig
        {
            NetworkId = "test-network",
            InitialTimestamp = 1000,
            InitialDifficulty = 5000,
            InitialEpoch = 0,
            EpochDurationSeconds = 30,
            TargetBlockTime = 30,
            PreminedAllocations = new Dictionary<string, long>()
        };

        // Act & Assert
        config.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithEmptyNetworkId_ThrowsException()
    {
        // Arrange
        var config = new GenesisConfig
        {
            NetworkId = "",
            InitialTimestamp = 1000,
            InitialDifficulty = 5000,
            InitialEpoch = 0,
            EpochDurationSeconds = 30,
            TargetBlockTime = 30,
            PreminedAllocations = new Dictionary<string, long>()
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("NetworkId", exception.Message);
    }

    [Fact]
    public void Validate_WithNegativeTimestamp_ThrowsException()
    {
        // Arrange
        var config = new GenesisConfig
        {
            NetworkId = "test-network",
            InitialTimestamp = -1,
            InitialDifficulty = 5000,
            InitialEpoch = 0,
            EpochDurationSeconds = 30,
            TargetBlockTime = 30,
            PreminedAllocations = new Dictionary<string, long>()
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("InitialTimestamp", exception.Message);
    }

    [Fact]
    public void Validate_WithZeroDifficulty_ThrowsException()
    {
        // Arrange
        var config = new GenesisConfig
        {
            NetworkId = "test-network",
            InitialTimestamp = 1000,
            InitialDifficulty = 0,
            InitialEpoch = 0,
            EpochDurationSeconds = 30,
            TargetBlockTime = 30,
            PreminedAllocations = new Dictionary<string, long>()
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("InitialDifficulty", exception.Message);
    }

    [Fact]
    public void Validate_WithNegativeEpoch_ThrowsException()
    {
        // Arrange
        var config = new GenesisConfig
        {
            NetworkId = "test-network",
            InitialTimestamp = 1000,
            InitialDifficulty = 5000,
            InitialEpoch = -1,
            EpochDurationSeconds = 30,
            TargetBlockTime = 30,
            PreminedAllocations = new Dictionary<string, long>()
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("InitialEpoch", exception.Message);
    }

    [Fact]
    public void Validate_WithZeroEpochDuration_ThrowsException()
    {
        // Arrange
        var config = new GenesisConfig
        {
            NetworkId = "test-network",
            InitialTimestamp = 1000,
            InitialDifficulty = 5000,
            InitialEpoch = 0,
            EpochDurationSeconds = 0,
            TargetBlockTime = 30,
            PreminedAllocations = new Dictionary<string, long>()
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("EpochDurationSeconds", exception.Message);
    }

    [Fact]
    public void Validate_WithZeroTargetBlockTime_ThrowsException()
    {
        // Arrange
        var config = new GenesisConfig
        {
            NetworkId = "test-network",
            InitialTimestamp = 1000,
            InitialDifficulty = 5000,
            InitialEpoch = 0,
            EpochDurationSeconds = 30,
            TargetBlockTime = 0,
            PreminedAllocations = new Dictionary<string, long>()
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("TargetBlockTime", exception.Message);
    }

    [Fact]
    public void Validate_WithNegativePremineAllocation_ThrowsException()
    {
        // Arrange
        var publicKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(33));
        var config = new GenesisConfig
        {
            NetworkId = "test-network",
            InitialTimestamp = 1000,
            InitialDifficulty = 5000,
            InitialEpoch = 0,
            EpochDurationSeconds = 30,
            TargetBlockTime = 30,
            PreminedAllocations = new Dictionary<string, long> { [publicKey] = -1 }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("allocation", exception.Message);
    }

    [Fact]
    public void Validate_WithEmptyPremineKey_ThrowsException()
    {
        // Arrange
        var config = new GenesisConfig
        {
            NetworkId = "test-network",
            InitialTimestamp = 1000,
            InitialDifficulty = 5000,
            InitialEpoch = 0,
            EpochDurationSeconds = 30,
            TargetBlockTime = 30,
            PreminedAllocations = new Dictionary<string, long> { [""] = 1000 }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("key", exception.Message);
    }
}
