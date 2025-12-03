namespace Spacetime.Core;

/// <summary>
/// Represents the configuration for a genesis block.
/// </summary>
/// <remarks>
/// Genesis configuration defines the initial parameters for a blockchain network.
/// Different networks (mainnet, testnet, devnet) will have different genesis configurations.
/// 
/// <example>
/// Creating a testnet genesis configuration:
/// <code>
/// var config = new GenesisConfig(
///     NetworkId: "testnet-v1",
///     InitialTimestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
///     InitialDifficulty: 1000,
///     InitialEpoch: 0,
///     EpochDurationSeconds: 30,
///     TargetBlockTime: 30,
///     PreminedAllocations: new Dictionary&lt;string, long&gt;
///     {
///         ["miner-public-key-1"] = 1000000,
///         ["miner-public-key-2"] = 500000
///     });
/// </code>
/// </example>
/// </remarks>
public sealed record GenesisConfig
{
    /// <summary>
    /// Gets the unique identifier for this network.
    /// </summary>
    /// <remarks>
    /// Examples: "mainnet", "testnet-v1", "devnet-local"
    /// </remarks>
    public required string NetworkId { get; init; }

    /// <summary>
    /// Gets the initial timestamp for the genesis block (Unix epoch seconds).
    /// </summary>
    public required long InitialTimestamp { get; init; }

    /// <summary>
    /// Gets the initial difficulty target for the network.
    /// </summary>
    public required long InitialDifficulty { get; init; }

    /// <summary>
    /// Gets the initial epoch number (typically 0 for genesis).
    /// </summary>
    public required long InitialEpoch { get; init; }

    /// <summary>
    /// Gets the duration of each epoch in seconds.
    /// </summary>
    public required int EpochDurationSeconds { get; init; }

    /// <summary>
    /// Gets the target time between blocks in seconds.
    /// </summary>
    public required int TargetBlockTime { get; init; }

    /// <summary>
    /// Gets the premine allocations mapping public keys to initial balances.
    /// </summary>
    /// <remarks>
    /// The key is a hex-encoded compressed public key (33 bytes).
    /// The value is the initial balance in the smallest unit.
    /// An empty dictionary means no premine.
    /// </remarks>
    public required IReadOnlyDictionary<string, long> PreminedAllocations { get; init; }

    /// <summary>
    /// Gets an optional description for this genesis configuration.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Validates that the genesis configuration has valid parameters.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(NetworkId))
        {
            throw new InvalidOperationException("NetworkId cannot be null or empty");
        }

        if (InitialTimestamp < 0)
        {
            throw new InvalidOperationException("InitialTimestamp must be non-negative");
        }

        if (InitialDifficulty <= 0)
        {
            throw new InvalidOperationException("InitialDifficulty must be positive");
        }

        if (InitialEpoch < 0)
        {
            throw new InvalidOperationException("InitialEpoch must be non-negative");
        }

        if (EpochDurationSeconds <= 0)
        {
            throw new InvalidOperationException("EpochDurationSeconds must be positive");
        }

        if (TargetBlockTime <= 0)
        {
            throw new InvalidOperationException("TargetBlockTime must be positive");
        }

        ArgumentNullException.ThrowIfNull(PreminedAllocations);

        foreach (var allocation in PreminedAllocations)
        {
            if (string.IsNullOrWhiteSpace(allocation.Key))
            {
                throw new InvalidOperationException("Premine allocation key cannot be null or empty");
            }

            if (allocation.Value < 0)
            {
                throw new InvalidOperationException($"Premine allocation for {allocation.Key} must be non-negative");
            }
        }
    }
}
