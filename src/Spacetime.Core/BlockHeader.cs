using System.Security.Cryptography;

namespace Spacetime.Core;

/// <summary>
/// Represents the header of a block in the Spacetime blockchain.
/// </summary>
/// <remarks>
/// The block header contains:
/// - Protocol version
/// - Link to the parent block (parent_hash)
/// - Block position (height)
/// - Timestamp
/// - Consensus data (difficulty, epoch, challenge)
/// - Proof data (plot_root, proof_score)
/// - Transaction commitment (tx_root)
/// - Miner identity (miner_id)
/// - Authentication (signature)
/// 
/// The header hash is computed as SHA256(serialize(header_without_signature)).
/// </remarks>
public sealed class BlockHeader
{
    /// <summary>
    /// Size of 32-byte hash fields.
    /// </summary>
    private const int HashSize = 32;

    /// <summary>
    /// Size of compressed ECDSA secp256k1 public key.
    /// </summary>
    private const int PublicKeySize = 33;

    /// <summary>
    /// Size of ECDSA signature (r + s components).
    /// </summary>
    private const int SignatureSize = 64;

    /// <summary>
    /// Current block format version.
    /// </summary>
    public const byte CurrentVersion = 1;

    private readonly byte[] _parentHash;
    private readonly byte[] _challenge;
    private readonly byte[] _plotRoot;
    private readonly byte[] _proofScore;
    private readonly byte[] _txRoot;
    private readonly byte[] _minerId;
    private byte[] _signature;

    /// <summary>
    /// Gets the protocol version for this block.
    /// </summary>
    public byte Version { get; }

    /// <summary>
    /// Gets the hash of the previous block.
    /// </summary>
    public ReadOnlySpan<byte> ParentHash => _parentHash;

    /// <summary>
    /// Gets the block height (parent height + 1).
    /// </summary>
    public long Height { get; }

    /// <summary>
    /// Gets the UTC timestamp when the block was assembled.
    /// </summary>
    public long Timestamp { get; }

    /// <summary>
    /// Gets the difficulty used for this epoch's scoring threshold.
    /// </summary>
    public long Difficulty { get; }

    /// <summary>
    /// Gets the challenge epoch this block belongs to.
    /// </summary>
    public long Epoch { get; }

    /// <summary>
    /// Gets the challenge issued for this epoch.
    /// </summary>
    public ReadOnlySpan<byte> Challenge => _challenge;

    /// <summary>
    /// Gets the Merkle root of the winning miner's plot file.
    /// </summary>
    public ReadOnlySpan<byte> PlotRoot => _plotRoot;

    /// <summary>
    /// Gets the computed score for the winning leaf.
    /// </summary>
    public ReadOnlySpan<byte> ProofScore => _proofScore;

    /// <summary>
    /// Gets the Merkle root of all included transactions.
    /// </summary>
    public ReadOnlySpan<byte> TxRoot => _txRoot;

    /// <summary>
    /// Gets the public key of the winning miner (compressed ECDSA secp256k1).
    /// </summary>
    public ReadOnlySpan<byte> MinerId => _minerId;

    /// <summary>
    /// Gets the signature of the block header using miner_id.
    /// </summary>
    public ReadOnlySpan<byte> Signature => _signature;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockHeader"/> class.
    /// </summary>
    /// <param name="version">The protocol version.</param>
    /// <param name="parentHash">The 32-byte hash of the parent block.</param>
    /// <param name="height">The block height.</param>
    /// <param name="timestamp">The UTC timestamp.</param>
    /// <param name="difficulty">The difficulty value.</param>
    /// <param name="epoch">The challenge epoch.</param>
    /// <param name="challenge">The 32-byte challenge for this epoch.</param>
    /// <param name="plotRoot">The 32-byte Merkle root of the plot.</param>
    /// <param name="proofScore">The 32-byte computed score.</param>
    /// <param name="txRoot">The 32-byte Merkle root of transactions.</param>
    /// <param name="minerId">The 33-byte compressed public key of the miner.</param>
    /// <param name="signature">The 64-byte signature (can be empty for unsigned headers).</param>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid values.</exception>
    public BlockHeader(
        byte version,
        ReadOnlySpan<byte> parentHash,
        long height,
        long timestamp,
        long difficulty,
        long epoch,
        ReadOnlySpan<byte> challenge,
        ReadOnlySpan<byte> plotRoot,
        ReadOnlySpan<byte> proofScore,
        ReadOnlySpan<byte> txRoot,
        ReadOnlySpan<byte> minerId,
        ReadOnlySpan<byte> signature)
    {
        if (parentHash.Length != HashSize)
        {
            throw new ArgumentException($"Parent hash must be {HashSize} bytes", nameof(parentHash));
        }

        if (height < 0)
        {
            throw new ArgumentException("Height must be non-negative", nameof(height));
        }

        if (timestamp < 0)
        {
            throw new ArgumentException("Timestamp must be non-negative", nameof(timestamp));
        }

        if (difficulty < 0)
        {
            throw new ArgumentException("Difficulty must be non-negative", nameof(difficulty));
        }

        if (epoch < 0)
        {
            throw new ArgumentException("Epoch must be non-negative", nameof(epoch));
        }

        if (challenge.Length != HashSize)
        {
            throw new ArgumentException($"Challenge must be {HashSize} bytes", nameof(challenge));
        }

        if (plotRoot.Length != HashSize)
        {
            throw new ArgumentException($"Plot root must be {HashSize} bytes", nameof(plotRoot));
        }

        if (proofScore.Length != HashSize)
        {
            throw new ArgumentException($"Proof score must be {HashSize} bytes", nameof(proofScore));
        }

        if (txRoot.Length != HashSize)
        {
            throw new ArgumentException($"Transaction root must be {HashSize} bytes", nameof(txRoot));
        }

        if (minerId.Length != PublicKeySize)
        {
            throw new ArgumentException($"Miner ID must be {PublicKeySize} bytes", nameof(minerId));
        }

        if (signature.Length != 0 && signature.Length != SignatureSize)
        {
            throw new ArgumentException($"Signature must be 0 or {SignatureSize} bytes", nameof(signature));
        }

        Version = version;
        _parentHash = parentHash.ToArray();
        Height = height;
        Timestamp = timestamp;
        Difficulty = difficulty;
        Epoch = epoch;
        _challenge = challenge.ToArray();
        _plotRoot = plotRoot.ToArray();
        _proofScore = proofScore.ToArray();
        _txRoot = txRoot.ToArray();
        _minerId = minerId.ToArray();
        _signature = signature.ToArray();
    }

    /// <summary>
    /// Sets the signature for this block header.
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
    /// Checks if this block header has been signed.
    /// </summary>
    /// <returns>True if the header has a signature; otherwise, false.</returns>
    public bool IsSigned() => _signature.Length == SignatureSize;

    /// <summary>
    /// Serializes the header without the signature (for hashing).
    /// </summary>
    /// <returns>The serialized header bytes without signature.</returns>
    public byte[] SerializeWithoutSignature()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(Version);
        writer.Write(_parentHash);
        writer.Write(Height);
        writer.Write(Timestamp);
        writer.Write(Difficulty);
        writer.Write(Epoch);
        writer.Write(_challenge);
        writer.Write(_plotRoot);
        writer.Write(_proofScore);
        writer.Write(_txRoot);
        writer.Write(_minerId);

        return stream.ToArray();
    }

    /// <summary>
    /// Computes the hash of this block header.
    /// </summary>
    /// <returns>The 32-byte SHA256 hash of the header (without signature).</returns>
    public byte[] ComputeHash()
    {
        var data = SerializeWithoutSignature();
        return SHA256.HashData(data);
    }

    /// <summary>
    /// Serializes the complete header including signature.
    /// </summary>
    /// <param name="writer">The binary writer to serialize to.</param>
    /// <exception cref="ArgumentNullException">Thrown when writer is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when header is not signed.</exception>
    public void Serialize(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (!IsSigned())
        {
            throw new InvalidOperationException("Block header must be signed before serialization");
        }

        writer.Write(Version);
        writer.Write(_parentHash);
        writer.Write(Height);
        writer.Write(Timestamp);
        writer.Write(Difficulty);
        writer.Write(Epoch);
        writer.Write(_challenge);
        writer.Write(_plotRoot);
        writer.Write(_proofScore);
        writer.Write(_txRoot);
        writer.Write(_minerId);
        writer.Write(_signature);
    }

    /// <summary>
    /// Deserializes a block header from a <see cref="BinaryReader"/>.
    /// </summary>
    /// <param name="reader">The binary reader to deserialize from.</param>
    /// <returns>A new <see cref="BlockHeader"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when reader is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static BlockHeader Deserialize(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var version = reader.ReadByte();
        var parentHash = ReadExactBytes(reader, HashSize, "parent hash");
        var height = reader.ReadInt64();
        var timestamp = reader.ReadInt64();
        var difficulty = reader.ReadInt64();
        var epoch = reader.ReadInt64();
        var challenge = ReadExactBytes(reader, HashSize, "challenge");
        var plotRoot = ReadExactBytes(reader, HashSize, "plot root");
        var proofScore = ReadExactBytes(reader, HashSize, "proof score");
        var txRoot = ReadExactBytes(reader, HashSize, "transaction root");
        var minerId = ReadExactBytes(reader, PublicKeySize, "miner ID");
        var signature = ReadExactBytes(reader, SignatureSize, "signature");

        return new BlockHeader(
            version,
            parentHash,
            height,
            timestamp,
            difficulty,
            epoch,
            challenge,
            plotRoot,
            proofScore,
            txRoot,
            minerId,
            signature);
    }

    /// <summary>
    /// Gets the serialized size of a block header (with signature) in bytes.
    /// </summary>
    public static int SerializedSize =>
        sizeof(byte) +      // version
        HashSize +          // parent_hash
        sizeof(long) +      // height
        sizeof(long) +      // timestamp
        sizeof(long) +      // difficulty
        sizeof(long) +      // epoch
        HashSize +          // challenge
        HashSize +          // plot_root
        HashSize +          // proof_score
        HashSize +          // tx_root
        PublicKeySize +     // miner_id
        SignatureSize;      // signature

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
