using System.Buffers.Binary;

namespace Spacetime.Network;

/// <summary>
/// Represents a ping or pong message for connection liveness checking.
/// </summary>
/// <remarks>
/// Ping/Pong messages are used to verify that a connection is still alive
/// and to measure round-trip time. The nonce in a ping must be echoed back
/// in the corresponding pong message.
/// </remarks>
public sealed class PingPongMessage
{
    /// <summary>
    /// Gets the nonce value used to match ping/pong pairs.
    /// </summary>
    public long Nonce { get; }

    /// <summary>
    /// Gets the timestamp when the message was created.
    /// </summary>
    public long Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PingPongMessage"/> class.
    /// </summary>
    /// <param name="nonce">The nonce value.</param>
    /// <param name="timestamp">The timestamp.</param>
    public PingPongMessage(long nonce, long timestamp)
    {
        Nonce = nonce;
        Timestamp = timestamp;
    }

    /// <summary>
    /// Serializes the ping/pong message to a byte array.
    /// </summary>
    /// <returns>The serialized message.</returns>
    public byte[] Serialize()
    {
        // Format: [8 bytes nonce][8 bytes timestamp]
        var buffer = new byte[16];
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(0, 8), Nonce);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(8, 8), Timestamp);
        return buffer;
    }

    /// <summary>
    /// Deserializes a ping/pong message from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidDataException">Thrown when the data format is invalid.</exception>
    public static PingPongMessage Deserialize(ReadOnlyMemory<byte> data)
    {
        var span = data.Span;
        if (span.Length < 16)
        {
            throw new InvalidDataException("Ping/Pong message too short.");
        }

        var nonce = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(0, 8));
        var timestamp = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(8, 8));

        return new PingPongMessage(nonce, timestamp);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"PingPong(Nonce={Nonce}, Timestamp={Timestamp})";
    }
}
