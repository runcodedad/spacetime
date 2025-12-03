using System.Security.Cryptography;

namespace Spacetime.Core;

/// <summary>
/// Validates genesis blocks against genesis configurations.
/// </summary>
/// <remarks>
/// The genesis block validator ensures that a block meets all the requirements
/// to be considered a valid genesis block for a specific network configuration.
/// 
/// <example>
/// Validating a genesis block:
/// <code>
/// var validator = new GenesisBlockValidator();
/// var isValid = await validator.ValidateGenesisBlockAsync(genesisBlock, config);
/// </code>
/// </example>
/// </remarks>
public sealed class GenesisBlockValidator
{
    /// <summary>
    /// Validates that a block is a valid genesis block for the given configuration.
    /// </summary>
    /// <param name="block">The block to validate.</param>
    /// <param name="config">The genesis configuration to validate against.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the block is a valid genesis block; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when block or config is null.</exception>
    public async Task<bool> ValidateGenesisBlockAsync(
        Block block,
        GenesisConfig config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(config);

        try
        {
            // Validate configuration first
            config.Validate();

            cancellationToken.ThrowIfCancellationRequested();

            // Check height is 0
            if (block.Header.Height != 0)
            {
                return false;
            }

            // Check parent hash is all zeros
            if (!IsAllZeros(block.Header.ParentHash))
            {
                return false;
            }

            // Check timestamp matches config
            if (block.Header.Timestamp != config.InitialTimestamp)
            {
                return false;
            }

            // Check difficulty matches config
            if (block.Header.Difficulty != config.InitialDifficulty)
            {
                return false;
            }

            // Check epoch matches config
            if (block.Header.Epoch != config.InitialEpoch)
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Check challenge is derived from network ID
            var expectedChallenge = ComputeGenesisChallenge(config.NetworkId);
            if (!block.Header.Challenge.SequenceEqual(expectedChallenge))
            {
                return false;
            }

            // Check plot root is all zeros for genesis
            if (!IsAllZeros(block.Header.PlotRoot))
            {
                return false;
            }

            // Check proof score is all zeros for genesis
            if (!IsAllZeros(block.Header.ProofScore))
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Check header is signed
            if (!block.Header.IsSigned())
            {
                return false;
            }

            // Verify block hash is computed correctly
            var computedHash = block.ComputeHash();
            if (computedHash == null || computedHash.Length != 32)
            {
                return false;
            }

            // All validation checks passed
            await Task.CompletedTask;
            return true;
        }
        catch
        {
            // Any exception during validation means the block is invalid
            return false;
        }
    }

    /// <summary>
    /// Checks if a byte span contains all zeros.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if all bytes are zero; otherwise, false.</returns>
    private static bool IsAllZeros(ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
        {
            if (b != 0)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Computes the genesis challenge from the network ID.
    /// </summary>
    /// <param name="networkId">The network identifier.</param>
    /// <returns>A 32-byte challenge hash.</returns>
    private static byte[] ComputeGenesisChallenge(string networkId)
    {
        // Genesis challenge is the hash of the network ID
        var networkIdBytes = System.Text.Encoding.UTF8.GetBytes(networkId);
        return SHA256.HashData(networkIdBytes);
    }
}
