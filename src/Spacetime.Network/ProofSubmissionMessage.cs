namespace Spacetime.Network;

/// <summary>
/// Represents a submission of a Proof-of-Space-Time proof from a miner.
/// </summary>
/// <remarks>
/// This message contains the serialized proof data using the BlockProof's
/// native serialization format. Miners send this when they find a proof
/// that satisfies the current challenge and difficulty requirements.
/// </remarks>
public sealed class ProofSubmissionMessage : NetworkMessage
{
    /// <summary>
    /// Gets the type of the message.
    /// </summary>
    public override MessageType Type => MessageType.ProofSubmission;

    /// <summary>
    /// Maximum proof size (1 MB).
    /// </summary>
    public const int MaxProofSize = 1024 * 1024;

    /// <summary>
    /// Gets the serialized proof data.
    /// </summary>
    public ReadOnlyMemory<byte> ProofData { get; }

    /// <summary>
    /// Gets the miner ID (public key) submitting the proof.
    /// </summary>
    public ReadOnlyMemory<byte> MinerId { get; }

    /// <summary>
    /// Gets the block height this proof is for.
    /// </summary>
    public long BlockHeight { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProofSubmissionMessage"/> class.
    /// </summary>
    /// <param name="proofData">The serialized proof data.</param>
    /// <param name="minerId">The miner's public key (33 bytes).</param>
    /// <param name="blockHeight">The block height this proof is for.</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public ProofSubmissionMessage(ReadOnlyMemory<byte> proofData, ReadOnlyMemory<byte> minerId, long blockHeight)
    {
        if (proofData.Length == 0)
        {
            throw new ArgumentException("Proof data cannot be empty.", nameof(proofData));
        }

        if (proofData.Length > MaxProofSize)
        {
            throw new ArgumentException($"Proof data cannot exceed {MaxProofSize} bytes.", nameof(proofData));
        }

        if (minerId.Length != 33)
        {
            throw new ArgumentException("Miner ID must be 33 bytes.", nameof(minerId));
        }

        if (blockHeight < 0)
        {
            throw new ArgumentException("Block height must be non-negative.", nameof(blockHeight));
        }

        ProofData = proofData;
        MinerId = minerId;
        BlockHeight = blockHeight;
    }

    /// <summary>
    /// Serializes the message to a byte array.
    /// </summary>
    /// <returns>The serialized message.</returns>
    protected override byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(BlockHeight);
        writer.Write(MinerId.Span);
        writer.Write(ProofData.Length);
        writer.Write(ProofData.Span);

        return ms.ToArray();
    }

    /// <summary>
    /// Deserializes a message from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidDataException">Thrown when the data format is invalid.</exception>
    public static ProofSubmissionMessage Deserialize(ReadOnlyMemory<byte> data)
    {
        using var ms = new MemoryStream(data.ToArray());
        using var reader = new BinaryReader(ms);

        var blockHeight = reader.ReadInt64();
        var minerId = reader.ReadBytes(33);
        var proofLength = reader.ReadInt32();

        if (proofLength < 0 || proofLength > MaxProofSize)
        {
            throw new InvalidDataException($"Invalid proof length: {proofLength}");
        }

        var proofData = reader.ReadBytes(proofLength);

        return new ProofSubmissionMessage(proofData, minerId, blockHeight);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var minerIdHex = Convert.ToHexString(MinerId.Span);
        return $"ProofSubmission(Height={BlockHeight}, Miner={minerIdHex[..8]}..., Size={ProofData.Length})";
    }
}
