namespace Spacetime.Network;

/// <summary>
/// Represents a message containing a complete block.
/// </summary>
/// <remarks>
/// This message contains the serialized block data using the Block's
/// native serialization format. The block can be deserialized using
/// Spacetime.Core.Block.Deserialize().
/// </remarks>
public sealed class BlockMessage
{
    /// <summary>
    /// Maximum block size (16 MB).
    /// </summary>
    public const int MaxBlockSize = 16 * 1024 * 1024;

    /// <summary>
    /// Gets the serialized block data.
    /// </summary>
    public ReadOnlyMemory<byte> BlockData { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockMessage"/> class.
    /// </summary>
    /// <param name="blockData">The serialized block data.</param>
    /// <exception cref="ArgumentException">Thrown when block data size exceeds maximum.</exception>
    public BlockMessage(ReadOnlyMemory<byte> blockData)
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
    public byte[] Serialize()
    {
        return BlockData.ToArray();
    }

    /// <summary>
    /// Deserializes a message from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidDataException">Thrown when the data format is invalid.</exception>
    public static BlockMessage Deserialize(ReadOnlyMemory<byte> data)
    {
        if (data.Length == 0)
        {
            throw new InvalidDataException("Block message data cannot be empty.");
        }

        if (data.Length > MaxBlockSize)
        {
            throw new InvalidDataException($"Block message data cannot exceed {MaxBlockSize} bytes.");
        }

        return new BlockMessage(data);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Block(Size={BlockData.Length} bytes)";
    }
}
