namespace Spacetime.Core;

/// <summary>
/// Represents the body of a block containing transactions and proof data.
/// </summary>
/// <remarks>
/// The block body contains:
/// - List of transactions (currently as raw byte arrays)
/// - The winning PoST proof from the miner
/// </remarks>
public sealed class BlockBody
{
    private readonly List<byte[]> _transactions;

    /// <summary>
    /// Gets the list of transactions in this block.
    /// </summary>
    /// <remarks>
    /// Transactions are currently represented as raw byte arrays.
    /// This will be replaced with a proper Transaction class in the future.
    /// </remarks>
    public IReadOnlyList<byte[]> Transactions => _transactions;

    /// <summary>
    /// Gets the winning PoST proof for this block.
    /// </summary>
    public BlockProof Proof { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockBody"/> class.
    /// </summary>
    /// <param name="transactions">The list of transactions.</param>
    /// <param name="proof">The PoST proof.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public BlockBody(IReadOnlyList<byte[]> transactions, BlockProof proof)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        ArgumentNullException.ThrowIfNull(proof);

        // Validate and copy transactions
        _transactions = new List<byte[]>(transactions.Count);
        foreach (var tx in transactions)
        {
            if (tx == null)
            {
                throw new ArgumentException("Transactions cannot contain null entries", nameof(transactions));
            }
            _transactions.Add((byte[])tx.Clone());
        }

        Proof = proof;
    }

    /// <summary>
    /// Serializes the block body using a <see cref="BinaryWriter"/>.
    /// </summary>
    /// <param name="writer">The binary writer to serialize to.</param>
    /// <exception cref="ArgumentNullException">Thrown when writer is null.</exception>
    public void Serialize(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        // Write transaction count and each transaction
        writer.Write(_transactions.Count);
        foreach (var tx in _transactions)
        {
            writer.Write(tx.Length);
            writer.Write(tx);
        }

        // Write proof
        Proof.Serialize(writer);
    }

    /// <summary>
    /// Deserializes a block body from a <see cref="BinaryReader"/>.
    /// </summary>
    /// <param name="reader">The binary reader to deserialize from.</param>
    /// <returns>A new <see cref="BlockBody"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when reader is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static BlockBody Deserialize(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        // Read transactions
        var txCount = reader.ReadInt32();
        if (txCount < 0)
        {
            throw new InvalidOperationException("Invalid transaction count");
        }

        var transactions = new List<byte[]>(txCount);
        for (var i = 0; i < txCount; i++)
        {
            var txLength = reader.ReadInt32();
            if (txLength < 0)
            {
                throw new InvalidOperationException("Invalid transaction length");
            }

            var tx = reader.ReadBytes(txLength);
            if (tx.Length != txLength)
            {
                throw new InvalidOperationException("Failed to read transaction: unexpected end of stream");
            }
            transactions.Add(tx);
        }

        // Read proof
        var proof = BlockProof.Deserialize(reader);

        return new BlockBody(transactions, proof);
    }
}
