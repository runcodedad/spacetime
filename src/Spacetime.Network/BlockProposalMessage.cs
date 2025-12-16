namespace Spacetime.Network;

/// <summary>
/// Represents a proposal of a new block to be added to the blockchain.
/// </summary>
/// <remarks>
/// This message is broadcast when a node wants to propose a new block.
/// It contains the complete serialized block data using the Block's
/// native serialization format.
/// </remarks>
public sealed class BlockProposalMessage : NetworkMessage
{
    /// <summary>
    /// Gets the type of the message.
    /// </summary>
    public override MessageType Type => MessageType.NewBlock;

    /// <summary>
    /// Maximum block size (16 MB).
    /// </summary>
    public const int MaxBlockSize = 16 * 1024 * 1024;

    /// <summary>
    /// Gets the serialized block data.
    /// </summary>
    public ReadOnlyMemory<byte> BlockData { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockProposalMessage"/> class.
    /// </summary>
    /// <param name="blockData">The serialized block data.</param>
    /// <exception cref="ArgumentException">Thrown when block data is invalid.</exception>
    public BlockProposalMessage(ReadOnlyMemory<byte> blockData)
    {
        if (blockData.Length == 0)
        {
            throw new ArgumentException("Block data cannot be empty.", nameof(blockData));
        }

        if (blockData.Length > MaxBlockSize)
        {
            throw new ArgumentException($"Block data cannot exceed {MaxBlockSize} bytes.", nameof(blockData));
        }

        BlockData = blockData;
    }

    /// <summary>
    /// Serializes the message to a byte array.
    /// </summary>
    /// <returns>The serialized message.</returns>
    protected override byte[] Serialize()
    {
        return BlockData.ToArray();
    }

    /// <summary>
    /// Deserializes a message from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidDataException">Thrown when the data format is invalid.</exception>
    public static BlockProposalMessage Deserialize(ReadOnlyMemory<byte> data)
    {
        if (data.Length == 0)
        {
            throw new InvalidDataException("Block proposal data cannot be empty.");
        }

        if (data.Length > MaxBlockSize)
        {
            throw new InvalidDataException($"Block proposal data cannot exceed {MaxBlockSize} bytes.");
        }

        return new BlockProposalMessage(data);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"BlockProposal(Size={BlockData.Length} bytes)";
    }
}
