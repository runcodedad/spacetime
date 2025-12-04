using System.Security.Cryptography;

namespace Spacetime.Core;

/// <summary>
/// Provides deterministic challenge derivation for epochs.
/// </summary>
/// <remarks>
/// Challenges are derived from the previous block's hash to ensure:
/// - Determinism: All nodes derive the same challenge
/// - Uniqueness: Each epoch has a unique challenge (anti-replay)
/// - Unpredictability: Miners cannot pre-compute challenges
/// 
/// For the genesis block or first epoch, a special challenge is derived from the network ID.
/// </remarks>
public static class ChallengeDerivation
{
    /// <summary>
    /// Size of a challenge in bytes (SHA256).
    /// </summary>
    public const int ChallengeSize = 32;

    /// <summary>
    /// Derives a challenge from the previous block hash and epoch number.
    /// </summary>
    /// <param name="previousBlockHash">The hash of the previous block.</param>
    /// <param name="epochNumber">The epoch number.</param>
    /// <returns>A 32-byte challenge unique to this epoch.</returns>
    /// <exception cref="ArgumentNullException">Thrown when previousBlockHash is null.</exception>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid values.</exception>
    /// <remarks>
    /// The challenge is computed as: SHA256(previousBlockHash || epochNumber)
    /// This ensures:
    /// - Each epoch has a unique challenge
    /// - Challenges cannot be predicted until the previous block is known
    /// - All nodes derive the same challenge deterministically
    /// </remarks>
    public static byte[] DeriveChallenge(ReadOnlySpan<byte> previousBlockHash, long epochNumber)
    {
        if (previousBlockHash.Length != ChallengeSize)
        {
            throw new ArgumentException($"Previous block hash must be {ChallengeSize} bytes", nameof(previousBlockHash));
        }

        if (epochNumber < 0)
        {
            throw new ArgumentException("Epoch number must be non-negative", nameof(epochNumber));
        }

        // Combine previous block hash and epoch number
        // Use little-endian explicitly to ensure consistency across different systems
        Span<byte> combined = stackalloc byte[ChallengeSize + sizeof(long)];
        previousBlockHash.CopyTo(combined);
        
        // Write epoch number in little-endian format for cross-platform consistency
        if (BitConverter.IsLittleEndian)
        {
            BitConverter.TryWriteBytes(combined[ChallengeSize..], epochNumber);
        }
        else
        {
            // Convert to little-endian on big-endian systems
            var epochBytes = BitConverter.GetBytes(epochNumber);
            Array.Reverse(epochBytes);
            epochBytes.CopyTo(combined[ChallengeSize..]);
        }

        // Return SHA256 hash as the challenge
        return SHA256.HashData(combined);
    }

    /// <summary>
    /// Derives a genesis challenge from a network ID.
    /// </summary>
    /// <param name="networkId">The network identifier (e.g., "mainnet", "testnet").</param>
    /// <returns>A 32-byte challenge for the genesis epoch.</returns>
    /// <exception cref="ArgumentNullException">Thrown when networkId is null.</exception>
    /// <exception cref="ArgumentException">Thrown when networkId is empty.</exception>
    /// <remarks>
    /// The genesis challenge is computed as: SHA256(networkId)
    /// This ensures different networks have different initial challenges.
    /// </remarks>
    public static byte[] DeriveGenesisChallenge(string networkId)
    {
        ArgumentNullException.ThrowIfNull(networkId);

        if (string.IsNullOrWhiteSpace(networkId))
        {
            throw new ArgumentException("Network ID cannot be empty", nameof(networkId));
        }

        var networkIdBytes = System.Text.Encoding.UTF8.GetBytes(networkId);
        return SHA256.HashData(networkIdBytes);
    }

    /// <summary>
    /// Verifies that a challenge was correctly derived from the previous block hash and epoch number.
    /// </summary>
    /// <param name="challenge">The challenge to verify.</param>
    /// <param name="previousBlockHash">The hash of the previous block.</param>
    /// <param name="epochNumber">The epoch number.</param>
    /// <returns>True if the challenge is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid sizes.</exception>
    public static bool VerifyChallenge(ReadOnlySpan<byte> challenge, ReadOnlySpan<byte> previousBlockHash, long epochNumber)
    {
        if (challenge.Length != ChallengeSize)
        {
            throw new ArgumentException($"Challenge must be {ChallengeSize} bytes", nameof(challenge));
        }

        var expectedChallenge = DeriveChallenge(previousBlockHash, epochNumber);
        return challenge.SequenceEqual(expectedChallenge);
    }

    /// <summary>
    /// Verifies that a genesis challenge was correctly derived from the network ID.
    /// </summary>
    /// <param name="challenge">The challenge to verify.</param>
    /// <param name="networkId">The network identifier.</param>
    /// <returns>True if the challenge is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when networkId is null.</exception>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid values.</exception>
    public static bool VerifyGenesisChallenge(ReadOnlySpan<byte> challenge, string networkId)
    {
        if (challenge.Length != ChallengeSize)
        {
            throw new ArgumentException($"Challenge must be {ChallengeSize} bytes", nameof(challenge));
        }

        var expectedChallenge = DeriveGenesisChallenge(networkId);
        return challenge.SequenceEqual(expectedChallenge);
    }
}
