namespace Spacetime.Core;

/// <summary>
/// Provides cryptographic signing functionality for blocks.
/// </summary>
/// <remarks>
/// Block signers use ECDSA with secp256k1 curve to sign block headers.
/// The signature proves that the miner authorized the block.
/// </remarks>
public interface IBlockSigner
{
    /// <summary>
    /// Signs a block header hash with the miner's private key.
    /// </summary>
    /// <param name="headerHash">The 32-byte hash of the block header (without signature).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A 64-byte ECDSA signature.</returns>
    /// <exception cref="ArgumentException">Thrown when headerHash is not 32 bytes.</exception>
    /// <remarks>
    /// The signature is computed as ECDSA_sign(SHA256(serialize(header_without_signature))).
    /// The signature format is (r, s) components concatenated (32 + 32 = 64 bytes).
    /// </remarks>
    Task<byte[]> SignBlockHeaderAsync(ReadOnlyMemory<byte> headerHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the public key (miner ID) for this signer.
    /// </summary>
    /// <returns>A 33-byte compressed ECDSA secp256k1 public key.</returns>
    byte[] GetPublicKey();
}
