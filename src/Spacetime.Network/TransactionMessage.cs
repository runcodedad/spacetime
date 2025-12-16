namespace Spacetime.Network;

/// <summary>
/// Represents a message containing a transaction to be broadcast to the network.
/// </summary>
/// <remarks>
/// This message contains the serialized transaction data using the Transaction's
/// native serialization format. The transaction can be deserialized using
/// Spacetime.Core.Transaction.Deserialize().
/// </remarks>
public sealed class TransactionMessage : NetworkMessage
{
    /// <summary>
    /// Gets the type of the message.
    /// </summary>
    public override MessageType Type => MessageType.Transaction;

    /// <summary>
    /// Maximum transaction size (1 MB).
    /// </summary>
    public const int MaxTransactionSize = 1024 * 1024;

    /// <summary>
    /// Gets the serialized transaction data.
    /// </summary>
    public ReadOnlyMemory<byte> TransactionData { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionMessage"/> class.
    /// </summary>
    /// <param name="transactionData">The serialized transaction data.</param>
    /// <exception cref="ArgumentException">Thrown when transaction data is invalid.</exception>
    public TransactionMessage(ReadOnlyMemory<byte> transactionData)
    {
        if (transactionData.Length == 0)
        {
            throw new ArgumentException("Transaction data cannot be empty.", nameof(transactionData));
        }

        if (transactionData.Length > MaxTransactionSize)
        {
            throw new ArgumentException($"Transaction data cannot exceed {MaxTransactionSize} bytes.", nameof(transactionData));
        }

        TransactionData = transactionData;
    }

    /// <summary>
    /// Serializes the message to a byte array.
    /// </summary>
    /// <returns>The serialized message.</returns>
    protected override byte[] Serialize()
    {
        return TransactionData.ToArray();
    }

    /// <summary>
    /// Deserializes a message from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidDataException">Thrown when the data format is invalid.</exception>
    public static TransactionMessage Deserialize(ReadOnlyMemory<byte> data)
    {
        if (data.Length == 0)
        {
            throw new InvalidDataException("Transaction message data cannot be empty.");
        }

        if (data.Length > MaxTransactionSize)
        {
            throw new InvalidDataException($"Transaction message data cannot exceed {MaxTransactionSize} bytes.");
        }

        return new TransactionMessage(data);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Transaction(Size={TransactionData.Length} bytes)";
    }
}
