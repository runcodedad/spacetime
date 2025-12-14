namespace Spacetime.Network;

/// <summary>
/// Represents a request for a complete block by its hash.
/// </summary>
public sealed class GetBlockMessage
{
    /// <summary>
    /// Size of a block hash in bytes.
    /// </summary>
    private const int HashSize = 32;

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
        if (blockHash.Length != HashSize)
        {
            throw new ArgumentException($"Block hash must be {HashSize} bytes.", nameof(blockHash));
        }

        BlockHash = blockHash;
    }

    /// <summary>
    /// Serializes the message to a byte array.
    /// </summary>
    /// <returns>The serialized message.</returns>
    public byte[] Serialize()
    {
        return BlockHash.ToArray();
    }

    /// <summary>
    /// Deserializes a message from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidDataException">Thrown when the data format is invalid.</exception>
    public static GetBlockMessage Deserialize(ReadOnlyMemory<byte> data)
    {
        if (data.Length != HashSize)
        {
            throw new InvalidDataException($"GetBlock message must be {HashSize} bytes.");
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
