using Spacetime.Core;

namespace Spacetime.Miner;

/// <summary>
/// Mock implementation of IBlockSigner for testing and development.
/// </summary>
/// <remarks>
/// This is a placeholder implementation that does NOT provide real cryptographic security.
/// TODO: Replace with actual ECDSA secp256k1 implementation before production use.
/// </remarks>
internal sealed class MockBlockSigner : IBlockSigner
{
    private readonly byte[] _publicKey;
    private readonly byte[] _privateKey;

    private MockBlockSigner(byte[] privateKey, byte[] publicKey)
    {
        _privateKey = privateKey;
        _publicKey = publicKey;
    }

    /// <summary>
    /// Creates a new mock signer with generated keys.
    /// </summary>
    public static MockBlockSigner Generate()
    {
        var privateKey = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var publicKey = System.Security.Cryptography.RandomNumberGenerator.GetBytes(33);
        return new MockBlockSigner(privateKey, publicKey);
    }

    /// <summary>
    /// Creates a mock signer from an existing private key.
    /// </summary>
    public static MockBlockSigner FromPrivateKey(byte[] privateKey)
    {
        if (privateKey.Length != 32)
        {
            throw new ArgumentException("Private key must be 32 bytes", nameof(privateKey));
        }
        
        // Derive public key (mock - not real ECDSA)
        var publicKey = System.Security.Cryptography.RandomNumberGenerator.GetBytes(33);
        return new MockBlockSigner(privateKey, publicKey);
    }

    /// <inheritdoc/>
    public byte[] GetPublicKey() => _publicKey;

    /// <summary>
    /// Gets the private key (for persistence only - never expose in production).
    /// </summary>
    public byte[] GetPrivateKey() => _privateKey;

    /// <inheritdoc/>
    public Task<byte[]> SignBlockHeaderAsync(ReadOnlyMemory<byte> headerHash, CancellationToken cancellationToken = default)
    {
        if (headerHash.Length != 32)
        {
            throw new ArgumentException("Header hash must be 32 bytes", nameof(headerHash));
        }

        // Mock signature - NOT cryptographically secure
        // TODO: Replace with real ECDSA secp256k1 signing
        var signature = new byte[64];
        Array.Copy(headerHash.ToArray(), 0, signature, 0, 32);
        Array.Copy(_publicKey, 0, signature, 32, Math.Min(32, _publicKey.Length));
        
        return Task.FromResult(signature);
    }
}
