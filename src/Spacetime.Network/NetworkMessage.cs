namespace Spacetime.Network;

/// <summary>
/// Represents a protocol message exchanged between peers.
/// All network messages must inherit from this base class.
/// </summary>
public abstract class NetworkMessage
{
    private byte[]? _cachedPayload;

    /// <summary>
    /// Gets the type of the message.
    /// </summary>
    public abstract MessageType Type { get; }

    /// <summary>
    /// Gets the payload data of the message.
    /// </summary>
    public ReadOnlyMemory<byte> Payload
    {
        get
        {
            _cachedPayload ??= Serialize();
            return new(_cachedPayload);
        }
    }

    /// <summary>
    /// Serializes the message-specific data to a byte array.
    /// </summary>
    /// <returns>The serialized message data.</returns>
    protected abstract byte[] Serialize();

    /// <summary>
    /// Deserializes a network message from raw data based on its type.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <param name="data">The serialized payload data.</param>
    /// <returns>The deserialized network message.</returns>
    /// <exception cref="InvalidDataException">Thrown when the message type is unknown or data is invalid.</exception>
    public static NetworkMessage Deserialize(MessageType type, ReadOnlyMemory<byte> data)
    {
        return type switch
        {
            MessageType.Handshake => HandshakeMessage.Deserialize(data),
            MessageType.Ping => PingPongMessage.Deserialize(data),
            MessageType.Pong => PingPongMessage.Deserialize(data),
            MessageType.Peers => PeerListMessage.Deserialize(data),
            MessageType.GetHeaders => GetHeadersMessage.Deserialize(data),
            MessageType.Headers => HeadersMessage.Deserialize(data),
            MessageType.GetBlock => GetBlockMessage.Deserialize(data),
            MessageType.Block => BlockMessage.Deserialize(data),
            MessageType.Transaction => TransactionMessage.Deserialize(data),
            MessageType.ProofSubmission => ProofSubmissionMessage.Deserialize(data),
            MessageType.BlockAccepted => BlockAcceptedMessage.Deserialize(data),
            MessageType.TxPoolRequest => TxPoolRequestMessage.Deserialize(data),
            MessageType.NewBlock => BlockProposalMessage.Deserialize(data),
            MessageType.GetPeers => CreateEmpty(MessageType.GetPeers),
            MessageType.Heartbeat => CreateEmpty(MessageType.Heartbeat),
            MessageType.HandshakeAck => CreateEmpty(MessageType.HandshakeAck),
            _ => throw new InvalidDataException($"Unknown message type: {type}")
        };
    }

    /// <summary>
    /// Creates an empty message for message types that don't carry data.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <returns>A new empty network message.</returns>
    private static EmptyMessage CreateEmpty(MessageType type) => new(type);

    /// <summary>
    /// Represents a message with no payload.
    /// </summary>
    private sealed class EmptyMessage(MessageType type) : NetworkMessage
    {
        public override MessageType Type { get; } = type;

        protected override byte[] Serialize() => [];
    }
}
