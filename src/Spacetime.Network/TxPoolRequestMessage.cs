namespace Spacetime.Network;

/// <summary>
/// Represents a request for the contents of a node's transaction pool (mempool).
/// </summary>
/// <remarks>
/// This message requests pending transactions from a peer's mempool.
/// The response should contain a list of transaction hashes or complete transactions.
/// </remarks>
public sealed class TxPoolRequestMessage : NetworkMessage
{
    /// <summary>
    /// Gets the type of the message.
    /// </summary>
    public override MessageType Type => MessageType.TxPoolRequest;

    /// <summary>
    /// Gets the maximum number of transactions to return.
    /// </summary>
    public int MaxTransactions { get; }

    /// <summary>
    /// Gets a value indicating whether to return full transactions or just hashes.
    /// </summary>
    public bool IncludeTransactionData { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TxPoolRequestMessage"/> class.
    /// </summary>
    /// <param name="maxTransactions">The maximum number of transactions to return.</param>
    /// <param name="includeTransactionData">Whether to include full transaction data.</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public TxPoolRequestMessage(int maxTransactions, bool includeTransactionData)
    {
        if (maxTransactions <= 0)
        {
            throw new ArgumentException("Max transactions must be positive.", nameof(maxTransactions));
        }

        MaxTransactions = maxTransactions;
        IncludeTransactionData = includeTransactionData;
    }

    /// <summary>
    /// Serializes the message to a byte array.
    /// </summary>
    /// <returns>The serialized message.</returns>
    protected override byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(MaxTransactions);
        writer.Write(IncludeTransactionData);

        return ms.ToArray();
    }

    /// <summary>
    /// Deserializes a message from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidDataException">Thrown when the data format is invalid.</exception>
    public static TxPoolRequestMessage Deserialize(ReadOnlyMemory<byte> data)
    {
        using var ms = new MemoryStream(data.ToArray());
        using var reader = new BinaryReader(ms);

        var maxTransactions = reader.ReadInt32();
        var includeTransactionData = reader.ReadBoolean();

        if (maxTransactions <= 0)
        {
            throw new InvalidDataException("Max transactions must be positive.");
        }

        return new TxPoolRequestMessage(maxTransactions, includeTransactionData);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"TxPoolRequest(Max={MaxTransactions}, IncludeData={IncludeTransactionData})";
    }
}
