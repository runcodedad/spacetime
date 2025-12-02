namespace Spacetime.Core;

/// <summary>
/// Represents the body of a block containing transactions and proof data.
/// </summary>
/// <remarks>
/// The block body contains:
/// - List of transactions (as Transaction objects or raw byte arrays for backward compatibility)
/// - The winning PoST proof from the miner
/// </remarks>
public sealed class BlockBody
{
    private readonly List<byte[]> _transactionBytes;
    private readonly List<Transaction>? _transactions;

    /// <summary>
    /// Gets the list of transactions in this block as raw byte arrays.
    /// </summary>
    /// <remarks>
    /// This property is maintained for backward compatibility.
    /// Use <see cref="GetTransactions"/> to access transactions as Transaction objects.
    /// </remarks>
    public IReadOnlyList<byte[]> Transactions => _transactionBytes;

    /// <summary>
    /// Gets whether this block body contains typed Transaction objects.
    /// </summary>
    public bool HasTypedTransactions => _transactions != null;

    /// <summary>
    /// Gets the winning PoST proof for this block.
    /// </summary>
    public BlockProof Proof { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockBody"/> class with raw transaction bytes.
    /// </summary>
    /// <param name="transactions">The list of transactions as byte arrays.</param>
    /// <param name="proof">The PoST proof.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public BlockBody(IReadOnlyList<byte[]> transactions, BlockProof proof)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        ArgumentNullException.ThrowIfNull(proof);

        // Validate and copy transactions
        _transactionBytes = new List<byte[]>(transactions.Count);
        foreach (var tx in transactions)
        {
            if (tx == null)
            {
                throw new ArgumentException("Transactions cannot contain null entries", nameof(transactions));
            }
            _transactionBytes.Add((byte[])tx.Clone());
        }

        _transactions = null;
        Proof = proof;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockBody"/> class with Transaction objects.
    /// </summary>
    /// <param name="transactions">The list of typed transactions.</param>
    /// <param name="proof">The PoST proof.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public BlockBody(IReadOnlyList<Transaction> transactions, BlockProof proof)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        ArgumentNullException.ThrowIfNull(proof);

        // Validate and store transactions
        _transactions = new List<Transaction>(transactions.Count);
        _transactionBytes = new List<byte[]>(transactions.Count);
        
        foreach (var tx in transactions)
        {
            if (tx == null)
            {
                throw new ArgumentException("Transactions cannot contain null entries", nameof(transactions));
            }
            
            if (!tx.IsSigned())
            {
                throw new ArgumentException("All transactions must be signed", nameof(transactions));
            }
            
            _transactions.Add(tx);
            _transactionBytes.Add(tx.Serialize());
        }

        Proof = proof;
    }

    /// <summary>
    /// Gets the transactions as Transaction objects.
    /// </summary>
    /// <returns>The list of transactions.</returns>
    /// <exception cref="InvalidOperationException">Thrown when transactions are not typed or cannot be deserialized.</exception>
    public IReadOnlyList<Transaction> GetTransactions()
    {
        if (_transactions != null)
        {
            return _transactions;
        }

        // Try to deserialize byte arrays to Transaction objects
        var result = new List<Transaction>(_transactionBytes.Count);
        foreach (var txBytes in _transactionBytes)
        {
            try
            {
                result.Add(Transaction.Deserialize(txBytes));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize transaction: {ex.Message}", ex);
            }
        }
        return result;
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
        writer.Write(_transactionBytes.Count);
        foreach (var tx in _transactionBytes)
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
