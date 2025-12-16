using System.Buffers.Binary;

namespace Spacetime.Network;

/// <summary>
/// Implements a length-prefixed message codec for network messages.
/// Message format: [4 bytes length][1 byte type][N bytes payload]
/// </summary>
public sealed class LengthPrefixedMessageCodec : IMessageCodec
{
    private const int _headerSize = 5; // 4 bytes for length + 1 byte for type
    private const int _maxMessageSize = 16 * 1024 * 1024; // 16 MB max message size

    /// <inheritdoc/>
    public byte[] Encode(NetworkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var payloadLength = message.Payload.Length;
        var totalLength = _headerSize + payloadLength;
        var buffer = new byte[totalLength];

        // Write length (includes type byte and payload)
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(0, 4), payloadLength + 1);

        // Write message type
        buffer[4] = (byte)message.Type;

        // Write payload
        if (payloadLength > 0)
        {
            message.Payload.Span.CopyTo(buffer.AsSpan(5));
        }

        return buffer;
    }

    /// <inheritdoc/>
    public async Task<NetworkMessage?> DecodeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Read length header
        var lengthBuffer = new byte[4];
        var bytesRead = await ReadExactlyAsync(stream, lengthBuffer, cancellationToken).ConfigureAwait(false);
        if (bytesRead == 0)
        {
            // Connection closed gracefully
            return null;
        }

        if (bytesRead < 4)
        {
            throw new InvalidDataException("Incomplete length header received.");
        }

        var messageLength = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);

        // Validate message length
        if (messageLength < 1 || messageLength > _maxMessageSize)
        {
            throw new InvalidDataException($"Invalid message length: {messageLength}");
        }

        // Read message type
        var typeBuffer = new byte[1];
        bytesRead = await ReadExactlyAsync(stream, typeBuffer, cancellationToken).ConfigureAwait(false);
        if (bytesRead < 1)
        {
            throw new InvalidDataException("Incomplete message type received.");
        }

        var messageType = (MessageType)typeBuffer[0];
        if (!Enum.IsDefined(messageType))
        {
            throw new InvalidDataException($"Unknown message type: {typeBuffer[0]}");
        }

        // Read payload
        var payloadLength = messageLength - 1;
        var payload = payloadLength > 0 ? new byte[payloadLength] : Array.Empty<byte>();

        if (payloadLength > 0)
        {
            bytesRead = await ReadExactlyAsync(stream, payload, cancellationToken).ConfigureAwait(false);
            if (bytesRead < payloadLength)
            {
                throw new InvalidDataException("Incomplete payload received.");
            }
        }

        return NetworkMessage.Deserialize(messageType, payload);
    }

    private static async Task<int> ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var totalBytesRead = 0;
        var bytesToRead = buffer.Length;

        while (totalBytesRead < bytesToRead)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead, bytesToRead - totalBytesRead), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                // End of stream
                return totalBytesRead;
            }

            totalBytesRead += bytesRead;
        }

        return totalBytesRead;
    }
}
