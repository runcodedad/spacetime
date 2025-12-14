namespace Spacetime.Network;

/// <summary>
/// Represents a protocol message exchanged between peers.
/// </summary>
public sealed class NetworkMessage
{
    /// <summary>
    /// Gets the type of the message.
    /// </summary>
    public MessageType Type { get; }

    /// <summary>
    /// Gets the payload data of the message.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkMessage"/> class.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <param name="payload">The message payload.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is null.</exception>
    public NetworkMessage(MessageType type, ReadOnlyMemory<byte> payload)
    {
        Type = type;
        Payload = payload;
    }

    /// <summary>
    /// Creates a new message with no payload.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <returns>A new network message.</returns>
    public static NetworkMessage CreateEmpty(MessageType type)
    {
        return new NetworkMessage(type, ReadOnlyMemory<byte>.Empty);
    }
}
