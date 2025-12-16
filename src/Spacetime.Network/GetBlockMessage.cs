namespace Spacetime.Network;

/// <summary>
/// Represents a request for a complete block by its hash.
/// </summary>
public sealed class GetBlockMessage : NetworkMessage
{
    /// <summary>
    /// Gets the type of the message.
    /// </summary>
    public override MessageType Type => MessageType.GetBlock;

    /// <summary>
    /// Size of a block hash in bytes.
    /// </summary>
    private const int _hashSize = 32;

    /// <summary>
    /// Gets the hash of the requested block.
    /// </summary>
    public ReadOnlyMemory<byte> BlockHash { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetBlockMessage"/> class.
    /// </summary>
    /// <param name="blockHash">The hash of the block to request.</param>
    /// <exception cref="ArgumentException">Thrown when hash size is invalid.</exception>
    public GetBlockMessage(ReadOnlyMemory<byte> blockHash)
    {
        if (blockHash.Length != _hashSize)
        {
            throw new ArgumentException($"Block hash must be {_hashSize} bytes.", nameof(blockHash));
        }

        BlockHash = blockHash;
    }

    /// <summary>
    /// Serializes the message to a byte array.
    /// </summary>
    /// <returns>The serialized message.</returns>
    protected override byte[] Serialize()
    {
        return BlockHash.ToArray();
    }

    /// <summary>
    /// Deserializes a message from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidDataException">Thrown when the data format is invalid.</exception>
    internal static GetBlockMessage Deserialize(ReadOnlyMemory<byte> data)
    {
        if (data.Length != _hashSize)
        {
            throw new InvalidDataException($"GetBlock message must be {_hashSize} bytes.");
        }

        return new GetBlockMessage(data);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var hashHex = Convert.ToHexString(BlockHash.Span);
        return $"GetBlock(Hash={hashHex[..8]}...)";
    }
}
