namespace Spacetime.Miner;

/// <summary>
/// Configuration settings for the miner node.
/// </summary>
public sealed record MinerConfiguration
{
    /// <summary>
    /// Gets the path to the directory containing plot files.
    /// </summary>
    public required string PlotDirectory { get; init; }

    /// <summary>
    /// Gets the path to the plot metadata file.
    /// </summary>
    /// <remarks>
    /// This is a single JSON file that stores metadata for all plots managed by this miner.
    /// Each plot file (.plot) does not have its own separate metadata file.
    /// </remarks>
    public required string PlotMetadataPath { get; init; }

    /// <summary>
    /// Gets the address of the full node or validator to connect to.
    /// </summary>
    public required string NodeAddress { get; init; }

    /// <summary>
    /// Gets the port of the full node or validator to connect to.
    /// </summary>
    public required int NodePort { get; init; }

    /// <summary>
    /// Gets the miner's private key file path.
    /// </summary>
    public required string PrivateKeyPath { get; init; }

    /// <summary>
    /// Gets the network ID (e.g., "mainnet", "testnet").
    /// </summary>
    public required string NetworkId { get; init; }

    /// <summary>
    /// Gets the maximum number of concurrent proof generation tasks.
    /// </summary>
    public int MaxConcurrentProofs { get; init; } = 1;

    /// <summary>
    /// Gets the timeout for proof generation in seconds.
    /// </summary>
    public int ProofGenerationTimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// Gets the interval between connection retry attempts in seconds.
    /// </summary>
    public int ConnectionRetryIntervalSeconds { get; init; } = 5;

    /// <summary>
    /// Gets the maximum number of connection retry attempts before giving up.
    /// </summary>
    public int MaxConnectionRetries { get; init; } = 10;

    /// <summary>
    /// Gets whether to enable detailed performance monitoring.
    /// </summary>
    public bool EnablePerformanceMonitoring { get; init; } = true;

    /// <summary>
    /// Validates required fields in the configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException"> 
    /// Thrown when a required field is missing or invalid.
    /// </exception>
    public MinerConfiguration()
    {
        if (string.IsNullOrWhiteSpace(PlotDirectory))
        {
            throw new InvalidOperationException("PlotDirectory is required in configuration");
        }

        if (string.IsNullOrWhiteSpace(PlotMetadataPath))
        {
            throw new InvalidOperationException("PlotMetadataPath is required in configuration");
        }

        if (string.IsNullOrWhiteSpace(NodeAddress))
        {
            throw new InvalidOperationException("NodeAddress is required in configuration");
        }

        if (string.IsNullOrWhiteSpace(PrivateKeyPath))
        {
            throw new InvalidOperationException("PrivateKeyPath is required in configuration");
        }

        if (string.IsNullOrWhiteSpace(NetworkId))
        {
            throw new InvalidOperationException("NetworkId is required in configuration");
        }
    }

    /// <summary>
    /// Creates a default configuration for testing.
    /// </summary>
    public static MinerConfiguration Default()
    {
        // Use cross-platform home directory: ~/.spacetime/
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var spacetimeDir = Path.Combine(homeDir, ".spacetime");
        var plotsDir = Path.Combine(spacetimeDir, "plots");
        
        return new MinerConfiguration
        {
            PlotDirectory = plotsDir,
            PlotMetadataPath = Path.Combine(spacetimeDir, "plots_metadata.json"),
            NodeAddress = "127.0.0.1",
            NodePort = 8333,
            PrivateKeyPath = Path.Combine(spacetimeDir, "miner_key.dat"),
            NetworkId = "testnet",
            MaxConcurrentProofs = 1,
            ProofGenerationTimeoutSeconds = 60,
            ConnectionRetryIntervalSeconds = 5,
            MaxConnectionRetries = 10,
            EnablePerformanceMonitoring = true
        };
    }
}
