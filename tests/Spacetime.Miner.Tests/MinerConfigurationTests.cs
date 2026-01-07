namespace Spacetime.Miner.Tests;

public class MinerConfigurationTests
{
    [Fact]
    public void Default_ReturnsValidConfiguration()
    {
        // Act
        var config = MinerConfiguration.Default();

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.PlotDirectory);
        Assert.NotNull(config.PlotMetadataPath);
        Assert.NotNull(config.NodeAddress);
        Assert.True(config.NodePort > 0);
        Assert.NotNull(config.PrivateKeyPath);
        Assert.NotNull(config.NetworkId);
        Assert.True(config.MaxConcurrentProofs > 0);
        Assert.True(config.ProofGenerationTimeoutSeconds > 0);
        Assert.True(config.ConnectionRetryIntervalSeconds > 0);
        Assert.True(config.MaxConnectionRetries > 0);
    }

    [Fact]
    public void Configuration_WithRequiredProperties_IsValid()
    {
        // Arrange & Act
        var config = new MinerConfiguration
        {
            PlotDirectory = "/test/plots",
            PlotMetadataPath = "/test/metadata.json",
            NodeAddress = "192.168.1.1",
            NodePort = 9000,
            PrivateKeyPath = "/test/key.dat",
            NetworkId = "testnet",
            ChainStoragePath = "/test/storage"
        };

        // Assert
        Assert.Equal("/test/plots", config.PlotDirectory);
        Assert.Equal("/test/metadata.json", config.PlotMetadataPath);
        Assert.Equal("192.168.1.1", config.NodeAddress);
        Assert.Equal(9000, config.NodePort);
        Assert.Equal("/test/key.dat", config.PrivateKeyPath);
        Assert.Equal("testnet", config.NetworkId);
    }

    [Fact]
    public void Configuration_WithOptionalProperties_UsesDefaults()
    {
        // Arrange & Act
        var config = new MinerConfiguration
        {
            PlotDirectory = "/test/plots",
            PlotMetadataPath = "/test/metadata.json",
            NodeAddress = "192.168.1.1",
            NodePort = 9000,
            PrivateKeyPath = "/test/key.dat",
            NetworkId = "testnet",
            ChainStoragePath = "/test/storage"
        };

        // Assert - check default values
        Assert.Equal(1, config.MaxConcurrentProofs);
        Assert.Equal(60, config.ProofGenerationTimeoutSeconds);
        Assert.Equal(5, config.ConnectionRetryIntervalSeconds);
        Assert.Equal(10, config.MaxConnectionRetries);
        Assert.True(config.EnablePerformanceMonitoring);
    }

    [Fact]
    public void Configuration_WithCustomOptionalProperties_UsesCustomValues()
    {
        // Arrange & Act
        var config = new MinerConfiguration
        {
            PlotDirectory = "/test/plots",
            PlotMetadataPath = "/test/metadata.json",
            NodeAddress = "192.168.1.1",
            NodePort = 9000,
            PrivateKeyPath = "/test/key.dat",
            NetworkId = "testnet",
            MaxConcurrentProofs = 4,
            ProofGenerationTimeoutSeconds = 120,
            ConnectionRetryIntervalSeconds = 10,
            MaxConnectionRetries = 5,
            EnablePerformanceMonitoring = false,
            ChainStoragePath = "/test/storage"
        };

        // Assert
        Assert.Equal(4, config.MaxConcurrentProofs);
        Assert.Equal(120, config.ProofGenerationTimeoutSeconds);
        Assert.Equal(10, config.ConnectionRetryIntervalSeconds);
        Assert.Equal(5, config.MaxConnectionRetries);
        Assert.False(config.EnablePerformanceMonitoring);
    }
}
