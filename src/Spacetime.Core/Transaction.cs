using System.Security.Cryptography;

namespace Spacetime.Core;

/// <summary>
/// Represents a transaction in the Spacetime blockchain.
/// </summary>
/// <remarks>
/// Spacetime uses an account-based transaction model for extensibility.
/// Each transaction includes:
/// - Sender public key (33 bytes compressed ECDSA secp256k1)
/// - Recipient public key (33 bytes)
/// - Amount to transfer
/// - Nonce for replay protection
/// - Transaction fee
/// - ECDSA signature (64 bytes)
/// 
/// The transaction hash is computed as SHA256(serialize(transaction_without_signature)).
/// 
/// <example>
/// Creating and signing a transaction:
/// <code>
/// var tx = new Transaction(
///     sender: senderPublicKey,
///     recipient: recipientPublicKey,
///     amount: 1000,
///     nonce: 1,
///     fee: 10,
///     signature: Array.Empty&lt;byte&gt;());
/// 
/// // Sign the transaction
/// var txHash = tx.ComputeHash();
/// var signature = SignWithEcdsaSecp256k1(txHash, senderPrivateKey); // User-implemented
/// tx.SetSignature(signature);
/// 
/// // Serialize for network transmission
/// var bytes = tx.Serialize();
/// </code>
/// </example>
/// </remarks>
public sealed class Transaction
{
    /// <summary>
    /// Size of compressed ECDSA secp256k1 public key.
    /// </summary>
    private const int PublicKeySize = 33;

    /// <summary>
    /// Size of ECDSA signature (r + s components).
    /// </summary>
    private const int SignatureSize = 64;

    /// <summary>
    /// Current transaction format version.
    /// </summary>
    public const byte CurrentVersion = 1;

    private readonly byte[] _sender;
    private readonly byte[] _recipient;
    private byte[] _signature;

    /// <summary>
    /// Gets the transaction format version.
    /// </summary>
    public byte Version { get; }

    /// <summary>
    /// Gets the sender's public key (compressed ECDSA secp256k1).
    /// </summary>
    public ReadOnlySpan<byte> Sender => _sender;

    /// <summary>
    /// Gets the recipient's public key (compressed ECDSA secp256k1).
    /// </summary>
    public ReadOnlySpan<byte> Recipient => _recipient;

    /// <summary>
    /// Gets the amount to transfer.
    /// </summary>
    public long Amount { get; }

    /// <summary>
    /// Gets the nonce for replay protection.
    /// </summary>
    /// <remarks>
    /// The nonce must be sequential per account to prevent replay attacks.
    /// Each account maintains a nonce counter that increments with each transaction.
    /// </remarks>
    public long Nonce { get; }

    /// <summary>
    /// Gets the transaction fee paid to the miner.
    /// </summary>
    public long Fee { get; }

    /// <summary>
    /// Gets the signature of the transaction using the sender's private key.
    /// </summary>
    public ReadOnlySpan<byte> Signature => _signature;

    /// <summary>
    /// Initializes a new instance of the <see cref="Transaction"/> class.
    /// </summary>
    /// <param name="version">The transaction format version.</param>
    /// <param name="sender">The 33-byte compressed public key of the sender.</param>
    /// <param name="recipient">The 33-byte compressed public key of the recipient.</param>
    /// <param name="amount">The amount to transfer (must be positive).</param>
    /// <param name="nonce">The nonce for replay protection (must be non-negative).</param>
    /// <param name="fee">The transaction fee (must be non-negative).</param>
    /// <param name="signature">The 64-byte signature (can be empty for unsigned transactions).</param>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid values.</exception>
    public Transaction(
        byte version,
        ReadOnlySpan<byte> sender,
        ReadOnlySpan<byte> recipient,
        long amount,
        long nonce,
        long fee,
        ReadOnlySpan<byte> signature)
    {
        if (sender.Length != PublicKeySize)
        {
            throw new ArgumentException($"Sender must be {PublicKeySize} bytes", nameof(sender));
        }

        if (recipient.Length != PublicKeySize)
        {
            throw new ArgumentException($"Recipient must be {PublicKeySize} bytes", nameof(recipient));
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be positive", nameof(amount));
        }

        if (nonce < 0)
        {
            throw new ArgumentException("Nonce must be non-negative", nameof(nonce));
        }

        if (fee < 0)
        {
            throw new ArgumentException("Fee must be non-negative", nameof(fee));
        }

        if (signature.Length != 0 && signature.Length != SignatureSize)
        {
            throw new ArgumentException($"Signature must be 0 or {SignatureSize} bytes", nameof(signature));
        }

        Version = version;
        _sender = sender.ToArray();
        _recipient = recipient.ToArray();
        Amount = amount;
        Nonce = nonce;
        Fee = fee;
        _signature = signature.ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Transaction"/> class with the current version.
    /// </summary>
    /// <param name="sender">The 33-byte compressed public key of the sender.</param>
    /// <param name="recipient">The 33-byte compressed public key of the recipient.</param>
    /// <param name="amount">The amount to transfer (must be positive).</param>
    /// <param name="nonce">The nonce for replay protection (must be non-negative).</param>
    /// <param name="fee">The transaction fee (must be non-negative).</param>
    /// <param name="signature">The 64-byte signature (can be empty for unsigned transactions).</param>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid values.</exception>
    public Transaction(
        ReadOnlySpan<byte> sender,
        ReadOnlySpan<byte> recipient,
        long amount,
        long nonce,
        long fee,
        ReadOnlySpan<byte> signature)
        : this(CurrentVersion, sender, recipient, amount, nonce, fee, signature)
    {
    }

    /// <summary>
    /// Sets the signature for this transaction.
    /// </summary>
    /// <param name="signature">The 64-byte signature.</param>
    /// <exception cref="ArgumentException">Thrown when signature has invalid size.</exception>
    public void SetSignature(ReadOnlySpan<byte> signature)
    {
        if (signature.Length != SignatureSize)
        {
            throw new ArgumentException($"Signature must be {SignatureSize} bytes", nameof(signature));
        }

        _signature = signature.ToArray();
    }

    /// <summary>
    /// Checks if this transaction has been signed.
    /// </summary>
    /// <returns>True if the transaction has a signature; otherwise, false.</returns>
    public bool IsSigned() => _signature.Length == SignatureSize;

    /// <summary>
    /// Serializes the transaction without the signature (for hashing and signing).
    /// </summary>
    /// <returns>The serialized transaction bytes without signature.</returns>
    public byte[] SerializeWithoutSignature()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(Version);
        writer.Write(_sender);
        writer.Write(_recipient);
        writer.Write(Amount);
        writer.Write(Nonce);
        writer.Write(Fee);

        return stream.ToArray();
    }

    /// <summary>
    /// Computes the hash of this transaction.
    /// </summary>
    /// <returns>The 32-byte SHA256 hash of the transaction (without signature).</returns>
    public byte[] ComputeHash()
    {
        var data = SerializeWithoutSignature();
        return SHA256.HashData(data);
    }

    /// <summary>
    /// Serializes the complete transaction including signature to a byte array.
    /// </summary>
    /// <returns>The serialized transaction bytes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when transaction is not signed.</exception>
    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        Serialize(writer);
        return stream.ToArray();
    }

    /// <summary>
    /// Serializes the complete transaction including signature.
    /// </summary>
    /// <param name="writer">The binary writer to serialize to.</param>
    /// <exception cref="ArgumentNullException">Thrown when writer is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when transaction is not signed.</exception>
    public void Serialize(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (!IsSigned())
        {
            throw new InvalidOperationException("Transaction must be signed before serialization");
        }

        writer.Write(Version);
        writer.Write(_sender);
        writer.Write(_recipient);
        writer.Write(Amount);
        writer.Write(Nonce);
        writer.Write(Fee);
        writer.Write(_signature);
    }

    /// <summary>
    /// Deserializes a transaction from a byte array.
    /// </summary>
    /// <param name="data">The serialized transaction data.</param>
    /// <returns>A new <see cref="Transaction"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static Transaction Deserialize(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        return Deserialize(reader);
    }

    /// <summary>
    /// Deserializes a transaction from a <see cref="BinaryReader"/>.
    /// </summary>
    /// <param name="reader">The binary reader to deserialize from.</param>
    /// <returns>A new <see cref="Transaction"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when reader is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static Transaction Deserialize(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var version = reader.ReadByte();
        var sender = ReadExactBytes(reader, PublicKeySize, "sender");
        var recipient = ReadExactBytes(reader, PublicKeySize, "recipient");
        var amount = reader.ReadInt64();
        var nonce = reader.ReadInt64();
        var fee = reader.ReadInt64();
        var signature = ReadExactBytes(reader, SignatureSize, "signature");

        return new Transaction(
            version,
            sender,
            recipient,
            amount,
            nonce,
            fee,
            signature);
    }

    /// <summary>
    /// Gets the serialized size of a transaction (with signature) in bytes.
    /// </summary>
    public static int SerializedSize =>
        sizeof(byte) +      // version
        PublicKeySize +     // sender
        PublicKeySize +     // recipient
        sizeof(long) +      // amount
        sizeof(long) +      // nonce
        sizeof(long) +      // fee
        SignatureSize;      // signature

    /// <summary>
    /// Validates the transaction according to basic rules.
    /// </summary>
    /// <returns>True if the transaction passes basic validation; otherwise, false.</returns>
    /// <remarks>
    /// Basic validation includes:
    /// - Transaction must be signed
    /// - Amount must be positive
    /// - Fee must be non-negative
    /// - Nonce must be non-negative
    /// - Sender and recipient must not be identical
    /// 
    /// Note: This does NOT verify the signature or check account balances.
    /// Those checks must be performed by the caller with access to the state.
    /// </remarks>
    public bool ValidateBasicRules()
    {
        if (!IsSigned())
        {
            return false;
        }

        if (Amount <= 0)
        {
            return false;
        }

        if (Fee < 0)
        {
            return false;
        }

        if (Nonce < 0)
        {
            return false;
        }

        // Sender and recipient should not be the same
        if (_sender.AsSpan().SequenceEqual(_recipient))
        {
            return false;
        }

        return true;
    }

    private static byte[] ReadExactBytes(BinaryReader reader, int count, string fieldName)
    {
        var bytes = reader.ReadBytes(count);
        if (bytes.Length != count)
        {
            throw new InvalidOperationException($"Failed to read {fieldName}: unexpected end of stream");
        }
        return bytes;
    }
}
