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
    /// Creates a default configuration for testing.
    /// </summary>
    public static MinerConfiguration Default()
    {
        return new MinerConfiguration
        {
            PlotDirectory = "./plots",
            PlotMetadataPath = "./plots_metadata.json",
            NodeAddress = "127.0.0.1",
            NodePort = 8333,
            PrivateKeyPath = "./miner_key.dat",
            NetworkId = "testnet",
            MaxConcurrentProofs = 1,
            ProofGenerationTimeoutSeconds = 60,
            ConnectionRetryIntervalSeconds = 5,
            MaxConnectionRetries = 10,
            EnablePerformanceMonitoring = true
        };
    }
}
