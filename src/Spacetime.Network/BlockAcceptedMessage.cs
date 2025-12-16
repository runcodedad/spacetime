namespace Spacetime.Network;

/// <summary>
/// Represents a notification that a block has been accepted and added to the blockchain.
/// </summary>
/// <remarks>
/// This message is broadcast to inform the network that a block has been validated
/// and accepted into the blockchain. Nodes use this to update their chain state.
/// </remarks>
public sealed class BlockAcceptedMessage : NetworkMessage
{
    /// <summary>
    /// Gets the type of the message.
    /// </summary>
    public override MessageType Type => MessageType.BlockAccepted;

    /// <summary>
    /// Size of a block hash in bytes.
    /// </summary>
    private const int _hashSize = 32;

    /// <summary>
    /// Gets the hash of the accepted block.
    /// </summary>
    public ReadOnlyMemory<byte> BlockHash { get; }

    /// <summary>
    /// Gets the height of the accepted block.
    /// </summary>
    public long BlockHeight { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockAcceptedMessage"/> class.
    /// </summary>
    /// <param name="blockHash">The hash of the accepted block.</param>
    /// <param name="blockHeight">The height of the accepted block.</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public BlockAcceptedMessage(ReadOnlyMemory<byte> blockHash, long blockHeight)
    {
        if (blockHash.Length != _hashSize)
        {
            throw new ArgumentException($"Block hash must be {_hashSize} bytes.", nameof(blockHash));
        }

        if (blockHeight < 0)
        {
            throw new ArgumentException("Block height must be non-negative.", nameof(blockHeight));
        }

        BlockHash = blockHash;
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

        writer.Write(BlockHash.Span);
        writer.Write(BlockHeight);

        return ms.ToArray();
    }

    /// <summary>
    /// Deserializes a message from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidDataException">Thrown when the data format is invalid.</exception>
    internal static BlockAcceptedMessage Deserialize(ReadOnlyMemory<byte> data)
    {
        using var ms = new MemoryStream(data.ToArray());
        using var reader = new BinaryReader(ms);

        if (data.Length != _hashSize + 8)
        {
            throw new InvalidDataException($"BlockAccepted message must be {_hashSize + 8} bytes.");
        }

        var blockHash = reader.ReadBytes(_hashSize);
        var blockHeight = reader.ReadInt64();

        return new BlockAcceptedMessage(blockHash, blockHeight);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var hashHex = Convert.ToHexString(BlockHash.Span);
        return $"BlockAccepted(Hash={hashHex[..8]}..., Height={BlockHeight})";
    }
}
