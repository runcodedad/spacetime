using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Spacetime.Plotting;

/// <summary>
/// Generates deterministic leaf values for plot files using a combination of
/// miner public key, plot seed, and nonce values.
/// </summary>
public static class LeafGenerator
{
    /// <summary>
    /// Size of each generated leaf in bytes (32 bytes = 256 bits)
    /// </summary>
    public const int LeafSize = 32;

    /// <summary>
    /// Generates a deterministic leaf value.
    /// </summary>
    /// <param name="minerPublicKey">The miner's public key (32 bytes)</param>
    /// <param name="plotSeed">The plot seed (32 bytes)</param>
    /// <param name="nonce">The leaf index/nonce</param>
    /// <returns>A 32-byte leaf value</returns>
    /// <remarks>
    /// The leaf is computed as: SHA256(minerPublicKey || plotSeed || nonce)
    /// This ensures:
    /// - Determinism: same inputs always produce the same output
    /// - Uniqueness: different miners or plots produce different leaves
    /// - Collision resistance: SHA256 properties prevent practical collisions
    /// </remarks>
    public static byte[] GenerateLeaf(byte[] minerPublicKey, byte[] plotSeed, long nonce)
    {
        ArgumentNullException.ThrowIfNull(minerPublicKey);
        ArgumentNullException.ThrowIfNull(plotSeed);

        if (minerPublicKey.Length != 32)
        {
            throw new ArgumentException("Miner public key must be 32 bytes", nameof(minerPublicKey));
        }

        if (plotSeed.Length != 32)
        {
            throw new ArgumentException("Plot seed must be 32 bytes", nameof(plotSeed));
        }

        if (nonce < 0)
        {
            throw new ArgumentException("Nonce must be non-negative", nameof(nonce));
        }

        // Combine inputs: minerPublicKey || plotSeed || nonce
        var input = new byte[32 + 32 + 8];
        var offset = 0;

        minerPublicKey.CopyTo(input.AsSpan(offset));
        offset += 32;

        plotSeed.CopyTo(input.AsSpan(offset));
        offset += 32;

        BinaryPrimitives.WriteInt64LittleEndian(input.AsSpan(offset), nonce);

        // Hash to produce leaf
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(input);
    }

    /// <summary>
    /// Generates a sequence of leaf values asynchronously.
    /// </summary>
    /// <param name="minerPublicKey">The miner's public key (32 bytes)</param>
    /// <param name="plotSeed">The plot seed (32 bytes)</param>
    /// <param name="startNonce">The starting nonce value</param>
    /// <param name="count">The number of leaves to generate</param>
    /// <param name="onLeafGenerated">Optional callback invoked after each leaf is generated</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of leaf values</returns>
    public static async IAsyncEnumerable<byte[]> GenerateLeavesAsync(
        byte[] minerPublicKey,
        byte[] plotSeed,
        long startNonce,
        long count,
        Action? onLeafGenerated = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(minerPublicKey);
        ArgumentNullException.ThrowIfNull(plotSeed);

        if (minerPublicKey.Length != 32)
        {
            throw new ArgumentException("Miner public key must be 32 bytes", nameof(minerPublicKey));
        }

        if (plotSeed.Length != 32)
        {
            throw new ArgumentException("Plot seed must be 32 bytes", nameof(plotSeed));
        }

        if (startNonce < 0)
        {
            throw new ArgumentException("Start nonce must be non-negative", nameof(startNonce));
        }

        if (count <= 0)
        {
            throw new ArgumentException("Count must be positive", nameof(count));
        }

        for (long i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return GenerateLeaf(minerPublicKey, plotSeed, startNonce + i);
            onLeafGenerated?.Invoke();

            // Yield control periodically to avoid blocking
            if (i % 1000 == 0)
            {
                await Task.Yield();
            }
        }
    }
}
