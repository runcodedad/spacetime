namespace Spacetime.Core;

/// <summary>
/// Provides predefined genesis configurations for different Spacetime networks.
/// </summary>
/// <remarks>
/// This class contains standard genesis configurations for mainnet, testnet, and development networks.
/// Each configuration defines the initial parameters for its respective network.
/// </remarks>
public static class GenesisConfigs
{
    /// <summary>
    /// Gets the genesis configuration for the Spacetime mainnet.
    /// </summary>
    /// <remarks>
    /// Mainnet configuration with production parameters:
    /// - Higher initial difficulty for security
    /// - No premine allocations
    /// - 30-second target block time
    /// - 30-second epoch duration
    /// Note: Timestamp is set to current time when accessed. For a real mainnet launch,
    /// this should be replaced with a hardcoded timestamp at deployment time.
    /// </remarks>
    public static GenesisConfig Mainnet => new()
    {
        NetworkId = "spacetime-mainnet-v1",
        InitialTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        InitialDifficulty = 1_000_000,
        InitialEpoch = 0,
        EpochDurationSeconds = 30,
        TargetBlockTime = 30,
        PreminedAllocations = new Dictionary<string, long>(),
        Description = "Spacetime mainnet genesis configuration"
    };

    /// <summary>
    /// Gets the genesis configuration for the Spacetime testnet.
    /// </summary>
    /// <remarks>
    /// Testnet configuration with relaxed parameters for testing:
    /// - Lower initial difficulty for easier testing
    /// - Optional premine for test accounts
    /// - 30-second target block time
    /// - 30-second epoch duration
    /// </remarks>
    public static GenesisConfig Testnet => new()
    {
        NetworkId = "spacetime-testnet-v1",
        InitialTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        InitialDifficulty = 10_000,
        InitialEpoch = 0,
        EpochDurationSeconds = 30,
        TargetBlockTime = 30,
        PreminedAllocations = new Dictionary<string, long>(),
        Description = "Spacetime testnet genesis configuration"
    };

    /// <summary>
    /// Gets the genesis configuration for local development.
    /// </summary>
    /// <remarks>
    /// Development configuration with minimal parameters for rapid iteration:
    /// - Very low initial difficulty for instant mining
    /// - Shorter epoch duration for faster testing
    /// - 10-second target block time
    /// - 10-second epoch duration
    /// </remarks>
    public static GenesisConfig Development => new()
    {
        NetworkId = "spacetime-devnet-local",
        InitialTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        InitialDifficulty = 100,
        InitialEpoch = 0,
        EpochDurationSeconds = 10,
        TargetBlockTime = 10,
        PreminedAllocations = new Dictionary<string, long>(),
        Description = "Spacetime local development genesis configuration"
    };

    /// <summary>
    /// Creates a custom genesis configuration with the specified network ID.
    /// </summary>
    /// <param name="networkId">The unique network identifier.</param>
    /// <param name="timestamp">The initial timestamp (Unix epoch seconds). If null, uses current time.</param>
    /// <param name="difficulty">The initial difficulty. Defaults to 1000.</param>
    /// <param name="epochDuration">The epoch duration in seconds. Defaults to 30.</param>
    /// <param name="targetBlockTime">The target block time in seconds. Defaults to 30.</param>
    /// <param name="preminedAllocations">Optional premine allocations. If null, no premine.</param>
    /// <param name="description">Optional description for the configuration.</param>
    /// <returns>A new genesis configuration.</returns>
    public static GenesisConfig CreateCustom(
        string networkId,
        long? timestamp = null,
        long difficulty = 1000,
        int epochDuration = 30,
        int targetBlockTime = 30,
        IReadOnlyDictionary<string, long>? preminedAllocations = null,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(networkId);

        return new GenesisConfig
        {
            NetworkId = networkId,
            InitialTimestamp = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            InitialDifficulty = difficulty,
            InitialEpoch = 0,
            EpochDurationSeconds = epochDuration,
            TargetBlockTime = targetBlockTime,
            PreminedAllocations = preminedAllocations ?? new Dictionary<string, long>(),
            Description = description
        };
    }
}
