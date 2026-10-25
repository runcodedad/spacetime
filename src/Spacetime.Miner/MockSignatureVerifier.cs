using Spacetime.Core;

namespace Spacetime.Miner;

/// <summary>
/// Mock implementation of ISignatureVerifier for testing and development.
/// </summary>
/// <remarks>
/// This is a placeholder implementation that does NOT provide real cryptographic security.
/// TODO: Replace with actual ECDSA secp256k1 implementation before production use.
/// </remarks>
internal sealed class MockSignatureVerifier : ISignatureVerifier
{
    /// <inheritdoc/>
    public bool VerifySignature(byte[] hash, byte[] signature, byte[] publicKey)
    {
        if (hash.Length != 32)
        {
            throw new ArgumentException("Hash must be 32 bytes", nameof(hash));
        }

        if (signature.Length != 64)
        {
            throw new ArgumentException("Signature must be 64 bytes", nameof(signature));
        }

        if (publicKey.Length != 33)
        {
            throw new ArgumentException("Public key must be 33 bytes", nameof(publicKey));
        }

        // Mock verification - always returns true for testing
        // TODO: Replace with real ECDSA secp256k1 verification
        return true;
    }
}
