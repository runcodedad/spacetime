namespace Spacetime.Core;

/// <summary>
/// Represents a complete block in the Spacetime blockchain.
/// </summary>
/// <remarks>
/// A block consists of:
/// - A header containing metadata, consensus data, and authentication
/// - A body containing transactions and the PoST proof
/// 
/// <example>
/// Creating a new block:
/// <code>
/// var header = new BlockHeader(
///     version: BlockHeader.CurrentVersion,
///     parentHash: previousBlockHash,
///     height: previousHeight + 1,
///     timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
///     difficulty: currentDifficulty,
///     epoch: currentEpoch,
///     challenge: currentChallenge,
///     plotRoot: minerPlotRoot,
///     proofScore: computedScore,
///     txRoot: transactionMerkleRoot,
///     minerId: minerPublicKey,
///     signature: Array.Empty&lt;byte&gt;());
/// 
/// var body = new BlockBody(transactions, blockProof);
/// var block = new Block(header, body);
/// 
/// // Sign the block
/// block.Header.SetSignature(signature);
/// 
/// // Serialize for network transmission
/// var bytes = block.Serialize();
/// </code>
/// </example>
/// </remarks>
public sealed class Block
{
    /// <summary>
    /// Gets the block header.
    /// </summary>
    public BlockHeader Header { get; }

    /// <summary>
    /// Gets the block body.
    /// </summary>
    public BlockBody Body { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Block"/> class.
    /// </summary>
    /// <param name="header">The block header.</param>
    /// <param name="body">The block body.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public Block(BlockHeader header, BlockBody body)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(body);

        Header = header;
        Body = body;
    }

    /// <summary>
    /// Computes the hash of this block.
    /// </summary>
    /// <returns>The 32-byte SHA256 hash of the block header (without signature).</returns>
    public byte[] ComputeHash() => Header.ComputeHash();

    /// <summary>
    /// Serializes the complete block to a byte array.
    /// </summary>
    /// <returns>The serialized block bytes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the block header is not signed.</exception>
    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        Header.Serialize(writer);
        Body.Serialize(writer);

        return stream.ToArray();
    }

    /// <summary>
    /// Serializes the block using a <see cref="BinaryWriter"/>.
    /// </summary>
    /// <param name="writer">The binary writer to serialize to.</param>
    /// <exception cref="ArgumentNullException">Thrown when writer is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the block header is not signed.</exception>
    public void Serialize(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        Header.Serialize(writer);
        Body.Serialize(writer);
    }

    /// <summary>
    /// Deserializes a block from a byte array.
    /// </summary>
    /// <param name="data">The serialized block data.</param>
    /// <returns>A new <see cref="Block"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static Block Deserialize(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        return Deserialize(reader);
    }

    /// <summary>
    /// Deserializes a block from a <see cref="BinaryReader"/>.
    /// </summary>
    /// <param name="reader">The binary reader to deserialize from.</param>
    /// <returns>A new <see cref="Block"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when reader is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static Block Deserialize(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var header = BlockHeader.Deserialize(reader);
        var body = BlockBody.Deserialize(reader);

        return new Block(header, body);
    }
}
