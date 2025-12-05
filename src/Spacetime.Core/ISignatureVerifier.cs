namespace Spacetime.Core;

/// <summary>
/// Provides signature verification functionality for ECDSA signatures.
/// </summary>
/// <remarks>
/// Signature verifiers use ECDSA with secp256k1 curve to verify signatures
/// on hashes using public keys.
/// </remarks>
public interface ISignatureVerifier
{
    /// <summary>
    /// Verifies an ECDSA signature.
    /// </summary>
    /// <param name="hash">The 32-byte hash that was signed.</param>
    /// <param name="signature">The 64-byte ECDSA signature (r, s components).</param>
    /// <param name="publicKey">The 33-byte compressed ECDSA secp256k1 public key.</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid sizes.</exception>
    /// <remarks>
    /// The signature format is (r, s) components concatenated (32 + 32 = 64 bytes).
    /// The public key format is compressed secp256k1 (33 bytes).
    /// </remarks>
    bool VerifySignature(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey);
}
