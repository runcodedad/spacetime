namespace Spacetime.Core.Tests;

public class GenesisConfigsTests
{
    [Fact]
    public void Mainnet_HasValidConfiguration()
    {
        // Act
        var config = GenesisConfigs.Mainnet;

        // Assert
        Assert.NotNull(config);
        Assert.Equal("spacetime-mainnet-v1", config.NetworkId);
        Assert.True(config.InitialDifficulty > 0);
        Assert.Equal(0, config.InitialEpoch);
        Assert.Equal(30, config.EpochDurationSeconds);
        Assert.Equal(30, config.TargetBlockTime);
        Assert.NotNull(config.PreminedAllocations);
        
        // Validate doesn't throw
        config.Validate();
    }

    [Fact]
    public void Testnet_HasValidConfiguration()
    {
        // Act
        var config = GenesisConfigs.Testnet;

        // Assert
        Assert.NotNull(config);
        Assert.Equal("spacetime-testnet-v1", config.NetworkId);
        Assert.True(config.InitialDifficulty > 0);
        Assert.Equal(0, config.InitialEpoch);
        Assert.Equal(30, config.EpochDurationSeconds);
        Assert.Equal(30, config.TargetBlockTime);
        Assert.NotNull(config.PreminedAllocations);
        
        // Validate doesn't throw
        config.Validate();
    }

    [Fact]
    public void Development_HasValidConfiguration()
    {
        // Act
        var config = GenesisConfigs.Development;

        // Assert
        Assert.NotNull(config);
        Assert.Equal("spacetime-devnet-local", config.NetworkId);
        Assert.True(config.InitialDifficulty > 0);
        Assert.Equal(0, config.InitialEpoch);
        Assert.Equal(10, config.EpochDurationSeconds);
        Assert.Equal(10, config.TargetBlockTime);
        Assert.NotNull(config.PreminedAllocations);
        
        // Validate doesn't throw
        config.Validate();
    }

    [Fact]
    public void Mainnet_HasHigherDifficultyThanTestnet()
    {
        // Act
        var mainnet = GenesisConfigs.Mainnet;
        var testnet = GenesisConfigs.Testnet;

        // Assert
        Assert.True(mainnet.InitialDifficulty > testnet.InitialDifficulty);
    }

    [Fact]
    public void Testnet_HasHigherDifficultyThanDevelopment()
    {
        // Act
        var testnet = GenesisConfigs.Testnet;
        var development = GenesisConfigs.Development;

        // Assert
        Assert.True(testnet.InitialDifficulty > development.InitialDifficulty);
    }

    [Fact]
    public void Development_HasFasterBlockTimeThanProduction()
    {
        // Act
        var mainnet = GenesisConfigs.Mainnet;
        var development = GenesisConfigs.Development;

        // Assert
        Assert.True(development.TargetBlockTime < mainnet.TargetBlockTime);
        Assert.True(development.EpochDurationSeconds < mainnet.EpochDurationSeconds);
    }

    [Fact]
    public void CreateCustom_WithMinimalParameters_CreatesValidConfig()
    {
        // Act
        var config = GenesisConfigs.CreateCustom("my-network");

        // Assert
        Assert.NotNull(config);
        Assert.Equal("my-network", config.NetworkId);
        Assert.True(config.InitialTimestamp > 0);
        Assert.Equal(1000, config.InitialDifficulty);
        Assert.Equal(0, config.InitialEpoch);
        Assert.Equal(30, config.EpochDurationSeconds);
        Assert.Equal(30, config.TargetBlockTime);
        Assert.NotNull(config.PreminedAllocations);
        Assert.Empty(config.PreminedAllocations);
        
        // Validate doesn't throw
        config.Validate();
    }

    [Fact]
    public void CreateCustom_WithAllParameters_CreatesValidConfig()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var premine = new Dictionary<string, long> { ["test-key"] = 1000 };

        // Act
        var config = GenesisConfigs.CreateCustom(
            networkId: "my-network",
            timestamp: timestamp,
            difficulty: 5000,
            epochDuration: 45,
            targetBlockTime: 45,
            preminedAllocations: premine,
            description: "Custom test network");

        // Assert
        Assert.Equal("my-network", config.NetworkId);
        Assert.Equal(timestamp, config.InitialTimestamp);
        Assert.Equal(5000, config.InitialDifficulty);
        Assert.Equal(0, config.InitialEpoch);
        Assert.Equal(45, config.EpochDurationSeconds);
        Assert.Equal(45, config.TargetBlockTime);
        Assert.Single(config.PreminedAllocations);
        Assert.Equal("Custom test network", config.Description);
        
        // Validate doesn't throw
        config.Validate();
    }

    [Fact]
    public void CreateCustom_WithNullNetworkId_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => GenesisConfigs.CreateCustom(null!));
    }
}
