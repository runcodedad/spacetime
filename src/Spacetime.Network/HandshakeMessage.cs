using System.Buffers.Binary;
using System.Text;

namespace Spacetime.Network;

/// <summary>
/// Represents a handshake message exchanged during connection establishment.
/// </summary>
public sealed class HandshakeMessage : NetworkMessage
{
    /// <summary>
    /// Gets the type of the message.
    /// </summary>
    public override MessageType Type => MessageType.Handshake;

    /// <summary>
    /// Gets the protocol version.
    /// </summary>
    public int ProtocolVersion { get; }

    /// <summary>
    /// Gets the node ID.
    /// </summary>
    public string NodeId { get; }

    /// <summary>
    /// Gets the node's user agent string.
    /// </summary>
    public string UserAgent { get; }

    /// <summary>
    /// Gets the timestamp of the handshake.
    /// </summary>
    public long Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HandshakeMessage"/> class.
    /// </summary>
    /// <param name="protocolVersion">The protocol version.</param>
    /// <param name="nodeId">The node ID.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when string parameters are empty.</exception>
    public HandshakeMessage(int protocolVersion, string nodeId, string userAgent, long timestamp)
    {
        ArgumentNullException.ThrowIfNull(nodeId);
        ArgumentNullException.ThrowIfNull(userAgent);

        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("Node ID cannot be empty.", nameof(nodeId));
        }

        if (string.IsNullOrWhiteSpace(userAgent))
        {
            throw new ArgumentException("User agent cannot be empty.", nameof(userAgent));
        }

        ProtocolVersion = protocolVersion;
        NodeId = nodeId;
        UserAgent = userAgent;
        Timestamp = timestamp;
    }

    /// <summary>
    /// Serializes the handshake message to a byte array.
    /// </summary>
    /// <returns>The serialized handshake message.</returns>
    protected override byte[] Serialize()
    {
        var nodeIdBytes = Encoding.UTF8.GetBytes(NodeId);
        var userAgentBytes = Encoding.UTF8.GetBytes(UserAgent);

        // Format: [4 bytes version][8 bytes timestamp][4 bytes nodeId length][nodeId bytes][4 bytes userAgent length][userAgent bytes]
        var totalLength = 4 + 8 + 4 + nodeIdBytes.Length + 4 + userAgentBytes.Length;
        var buffer = new byte[totalLength];
        var offset = 0;

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset, 4), ProtocolVersion);
        offset += 4;

        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(offset, 8), Timestamp);
        offset += 8;

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset, 4), nodeIdBytes.Length);
        offset += 4;

        nodeIdBytes.CopyTo(buffer, offset);
        offset += nodeIdBytes.Length;

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset, 4), userAgentBytes.Length);
        offset += 4;

        userAgentBytes.CopyTo(buffer, offset);

        return buffer;
    }

    /// <summary>
    /// Deserializes a handshake message from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized handshake message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the data format is invalid.</exception>
    internal static HandshakeMessage Deserialize(ReadOnlyMemory<byte> data)
    {
        var span = data.Span;
        if (span.Length < 16)
        {
            throw new InvalidDataException("Handshake message too short.");
        }

        var offset = 0;

        var protocolVersion = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, 4));
        offset += 4;

        var timestamp = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(offset, 8));
        offset += 8;

        var nodeIdLength = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, 4));
        offset += 4;

        if (nodeIdLength < 0 || offset + nodeIdLength > span.Length)
        {
            throw new InvalidDataException("Invalid node ID length.");
        }

        var nodeId = Encoding.UTF8.GetString(span.Slice(offset, nodeIdLength));
        offset += nodeIdLength;

        if (offset + 4 > span.Length)
        {
            throw new InvalidDataException("Invalid user agent length.");
        }

        var userAgentLength = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, 4));
        offset += 4;

        if (userAgentLength < 0 || offset + userAgentLength > span.Length)
        {
            throw new InvalidDataException("Invalid user agent length.");
        }

        var userAgent = Encoding.UTF8.GetString(span.Slice(offset, userAgentLength));

        return new HandshakeMessage(protocolVersion, nodeId, userAgent, timestamp);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Handshake(v{ProtocolVersion}, {NodeId}, {UserAgent}, {Timestamp})";
    }
}
