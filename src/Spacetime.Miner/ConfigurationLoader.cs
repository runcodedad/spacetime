using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Spacetime.Miner;

/// <summary>
/// Loads miner configuration from YAML files and environment variables.
/// </summary>
public sealed class ConfigurationLoader : IConfigurationLoader
{
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationLoader"/> class.
    /// </summary>
    public ConfigurationLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    /// <summary>
    /// Loads configuration from a YAML file.
    /// </summary>
    /// <param name="filePath">Path to the YAML configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when filePath is null or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the YAML is invalid or missing required fields.</exception>
    public async Task<MinerConfiguration> LoadFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}", filePath);
        }

        var yaml = await File.ReadAllTextAsync(filePath, cancellationToken);
        var config = _deserializer.Deserialize<MinerConfiguration>(yaml);

        if (config == null)
        {
            throw new InvalidOperationException("Failed to deserialize configuration file");
        }

        return config;
    }

    /// <summary>
    /// Loads configuration from a YAML file and applies environment variable overrides.
    /// </summary>
    /// <param name="filePath">Path to the YAML configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded configuration with environment variable overrides applied.</returns>
    public async Task<MinerConfiguration> LoadWithEnvironmentOverridesAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var config = await LoadFromFileAsync(filePath, cancellationToken);
        return ApplyEnvironmentOverrides(config);
    }

    /// <summary>
    /// Creates a default configuration file at the specified path.
    /// </summary>
    /// <param name="filePath">Path where the configuration file should be created.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown when filePath is null or whitespace.</exception>
    public async Task CreateDefaultConfigAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var spacetimeDir = Path.Combine(homeDir, ".spacetime");
        var plotsDir = Path.Combine(spacetimeDir, "plots");

        var defaultConfig = MinerConfiguration.Default();

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var yaml = serializer.Serialize(defaultConfig);
        
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(filePath, yaml, cancellationToken);
    }

    /// <summary>
    /// Applies environment variable overrides to the configuration.
    /// </summary>
    /// <remarks>
    /// Environment variables follow the pattern: SPACETIME_MINER_{PROPERTY_NAME}
    /// For example: SPACETIME_MINER_PLOT_DIRECTORY, SPACETIME_MINER_NODE_PORT
    /// </remarks>
    private static MinerConfiguration ApplyEnvironmentOverrides(MinerConfiguration config)
    {
        var plotDirectory = Environment.GetEnvironmentVariable("SPACETIME_MINER_PLOT_DIRECTORY") ?? config.PlotDirectory;
        var plotMetadataPath = Environment.GetEnvironmentVariable("SPACETIME_MINER_PLOT_METADATA_PATH") ?? config.PlotMetadataPath;
        var nodeAddress = Environment.GetEnvironmentVariable("SPACETIME_MINER_NODE_ADDRESS") ?? config.NodeAddress;
        var privateKeyPath = Environment.GetEnvironmentVariable("SPACETIME_MINER_PRIVATE_KEY_PATH") ?? config.PrivateKeyPath;
        var networkId = Environment.GetEnvironmentVariable("SPACETIME_MINER_NETWORK_ID") ?? config.NetworkId;

        var nodePortStr = Environment.GetEnvironmentVariable("SPACETIME_MINER_NODE_PORT");
        var nodePort = nodePortStr != null && int.TryParse(nodePortStr, out var np) ? np : config.NodePort;

        var maxConcurrentProofsStr = Environment.GetEnvironmentVariable("SPACETIME_MINER_MAX_CONCURRENT_PROOFS");
        var maxConcurrentProofs = maxConcurrentProofsStr != null && int.TryParse(maxConcurrentProofsStr, out var mcp) ? mcp : config.MaxConcurrentProofs;

        var proofTimeoutStr = Environment.GetEnvironmentVariable("SPACETIME_MINER_PROOF_GENERATION_TIMEOUT_SECONDS");
        var proofTimeout = proofTimeoutStr != null && int.TryParse(proofTimeoutStr, out var pt) ? pt : config.ProofGenerationTimeoutSeconds;

        var retryIntervalStr = Environment.GetEnvironmentVariable("SPACETIME_MINER_CONNECTION_RETRY_INTERVAL_SECONDS");
        var retryInterval = retryIntervalStr != null && int.TryParse(retryIntervalStr, out var ri) ? ri : config.ConnectionRetryIntervalSeconds;

        var maxRetriesStr = Environment.GetEnvironmentVariable("SPACETIME_MINER_MAX_CONNECTION_RETRIES");
        var maxRetries = maxRetriesStr != null && int.TryParse(maxRetriesStr, out var mr) ? mr : config.MaxConnectionRetries;

        var perfMonitoringStr = Environment.GetEnvironmentVariable("SPACETIME_MINER_ENABLE_PERFORMANCE_MONITORING");
        var perfMonitoring = perfMonitoringStr != null && bool.TryParse(perfMonitoringStr, out var pm) ? pm : config.EnablePerformanceMonitoring;

        return config with
        {
            PlotDirectory = plotDirectory,
            PlotMetadataPath = plotMetadataPath,
            NodeAddress = nodeAddress,
            NodePort = nodePort,
            PrivateKeyPath = privateKeyPath,
            NetworkId = networkId,
            MaxConcurrentProofs = maxConcurrentProofs,
            ProofGenerationTimeoutSeconds = proofTimeout,
            ConnectionRetryIntervalSeconds = retryInterval,
            MaxConnectionRetries = maxRetries,
            EnablePerformanceMonitoring = perfMonitoring
        };
    }
}
