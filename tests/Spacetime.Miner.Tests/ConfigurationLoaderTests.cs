using YamlDotNet.Core;

namespace Spacetime.Miner.Tests;

public class ConfigurationLoaderTests
{
    [Fact]
    public async Task LoadFromFileAsync_WithValidYaml_ReturnsConfiguration()
    {
        // Arrange
        var loader = new ConfigurationLoader();
        var tempFile = Path.GetTempFileName();
        var yaml = @"
plotDirectory: /test/plots
plotMetadataPath: /test/metadata.json
nodeAddress: 192.168.1.1
nodePort: 9000
privateKeyPath: /test/key.dat
networkId: testnet
maxConcurrentProofs: 2
proofGenerationTimeoutSeconds: 120
connectionRetryIntervalSeconds: 10
maxConnectionRetries: 5
enablePerformanceMonitoring: false
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml);

            // Act
            var config = await loader.LoadFromFileAsync(tempFile);

            // Assert
            Assert.NotNull(config);
            Assert.Equal("/test/plots", config.PlotDirectory);
            Assert.Equal("/test/metadata.json", config.PlotMetadataPath);
            Assert.Equal("192.168.1.1", config.NodeAddress);
            Assert.Equal(9000, config.NodePort);
            Assert.Equal("/test/key.dat", config.PrivateKeyPath);
            Assert.Equal("testnet", config.NetworkId);
            Assert.Equal(2, config.MaxConcurrentProofs);
            Assert.Equal(120, config.ProofGenerationTimeoutSeconds);
            Assert.Equal(10, config.ConnectionRetryIntervalSeconds);
            Assert.Equal(5, config.MaxConnectionRetries);
            Assert.False(config.EnablePerformanceMonitoring);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task LoadFromFileAsync_WithMissingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var loader = new ConfigurationLoader();
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.yaml");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => loader.LoadFromFileAsync(nonExistentFile));
    }

    [Fact]
    public async Task LoadFromFileAsync_WithNullFilePath_ThrowsArgumentNullException()
    {
        // Arrange
        var loader = new ConfigurationLoader();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => loader.LoadFromFileAsync(null!));
    }

    [Fact]
    public async Task LoadFromFileAsync_WithMissingRequiredField_ThrowsInvalidOperationException()
    {
        // Arrange
        var loader = new ConfigurationLoader();
        var tempFile = Path.GetTempFileName();
        var yaml = @"
        nodeAddress: 192.168.1.1
        nodePort: 9000
        ";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => loader.LoadFromFileAsync(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task LoadWithEnvironmentOverridesAsync_AppliesEnvironmentVariables()
    {
        // Arrange
        var loader = new ConfigurationLoader();
        var tempFile = Path.GetTempFileName();
        var yaml = @"
plotDirectory: /test/plots
plotMetadataPath: /test/metadata.json
nodeAddress: 127.0.0.1
nodePort: 8333
privateKeyPath: /test/key.dat
networkId: testnet
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml);

            // Set environment variables
            Environment.SetEnvironmentVariable("SPACETIME_MINER_NODE_ADDRESS", "192.168.1.100");
            Environment.SetEnvironmentVariable("SPACETIME_MINER_NODE_PORT", "9999");
            Environment.SetEnvironmentVariable("SPACETIME_MINER_NETWORK_ID", "mainnet");

            // Act
            var config = await loader.LoadWithEnvironmentOverridesAsync(tempFile);

            // Assert
            Assert.Equal("192.168.1.100", config.NodeAddress);
            Assert.Equal(9999, config.NodePort);
            Assert.Equal("mainnet", config.NetworkId);
            // Other values should remain from file
            Assert.Equal("/test/plots", config.PlotDirectory);
            Assert.Equal("/test/key.dat", config.PrivateKeyPath);
        }
        finally
        {
            // Clean up
            Environment.SetEnvironmentVariable("SPACETIME_MINER_NODE_ADDRESS", null);
            Environment.SetEnvironmentVariable("SPACETIME_MINER_NODE_PORT", null);
            Environment.SetEnvironmentVariable("SPACETIME_MINER_NETWORK_ID", null);

            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task CreateDefaultConfigAsync_CreatesValidConfigFile()
    {
        // Arrange
        var loader = new ConfigurationLoader();
        var tempFile = Path.Combine(Path.GetTempPath(), $"config_{Guid.NewGuid()}.yaml");

        try
        {
            // Act
            await loader.CreateDefaultConfigAsync(tempFile);

            // Assert
            Assert.True(File.Exists(tempFile));

            // Verify we can load the created config
            var config = await loader.LoadFromFileAsync(tempFile);
            Assert.NotNull(config);
            Assert.NotNull(config.PlotDirectory);
            Assert.NotNull(config.NodeAddress);
            Assert.True(config.NodePort > 0);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task CreateDefaultConfigAsync_WithNullFilePath_ThrowsArgumentNullException()
    {
        // Arrange
        var loader = new ConfigurationLoader();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => loader.CreateDefaultConfigAsync(null!));
    }

    [Fact]
    public async Task CreateDefaultConfigAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var loader = new ConfigurationLoader();
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_dir_{Guid.NewGuid()}");
        var tempFile = Path.Combine(tempDir, "config.yaml");

        try
        {
            // Act
            await loader.CreateDefaultConfigAsync(tempFile);

            // Assert
            Assert.True(Directory.Exists(tempDir));
            Assert.True(File.Exists(tempFile));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task LoadFromFileAsync_WithDefaultValues_UsesDefaults()
    {
        // Arrange
        var loader = new ConfigurationLoader();
        var tempFile = Path.GetTempFileName();
        var yaml = @"
plotDirectory: /test/plots
plotMetadataPath: /test/metadata.json
nodeAddress: 127.0.0.1
nodePort: 8333
privateKeyPath: /test/key.dat
networkId: testnet
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml);

            // Act
            var config = await loader.LoadFromFileAsync(tempFile);

            // Assert - check default values
            Assert.Equal(1, config.MaxConcurrentProofs);
            Assert.Equal(60, config.ProofGenerationTimeoutSeconds);
            Assert.Equal(5, config.ConnectionRetryIntervalSeconds);
            Assert.Equal(10, config.MaxConnectionRetries);
            Assert.True(config.EnablePerformanceMonitoring);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
