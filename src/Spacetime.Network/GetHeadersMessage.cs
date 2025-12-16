using System.Buffers.Binary;

namespace Spacetime.Network;

/// <summary>
/// Represents a request for block headers starting from a specific block.
/// </summary>
public sealed class GetHeadersMessage : NetworkMessage
{
    /// <summary>
    /// Gets the type of the message.
    /// </summary>
    public override MessageType Type => MessageType.GetHeaders;

    /// <summary>
    /// Size of a block hash in bytes.
    /// </summary>
    private const int _hashSize = 32;

    /// <summary>
    /// Gets the hash of the block to start from (locator block).
    /// </summary>
    public ReadOnlyMemory<byte> LocatorHash { get; }

    /// <summary>
    /// Gets the hash of the block to stop at (optional).
    /// </summary>
    public ReadOnlyMemory<byte> StopHash { get; }

    /// <summary>
    /// Gets the maximum number of headers to return.
    /// </summary>
    public int MaxHeaders { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetHeadersMessage"/> class.
    /// </summary>
    /// <param name="locatorHash">The hash of the block to start from.</param>
    /// <param name="stopHash">The hash of the block to stop at (can be empty).</param>
    /// <param name="maxHeaders">The maximum number of headers to return.</param>
    /// <exception cref="ArgumentException">Thrown when hash sizes are invalid.</exception>
    public GetHeadersMessage(ReadOnlyMemory<byte> locatorHash, ReadOnlyMemory<byte> stopHash, int maxHeaders)
    {
        if (locatorHash.Length != _hashSize)
        {
            throw new ArgumentException($"Locator hash must be {_hashSize} bytes.", nameof(locatorHash));
        }

        if (stopHash.Length != 0 && stopHash.Length != _hashSize)
        {
            throw new ArgumentException($"Stop hash must be empty or {_hashSize} bytes.", nameof(stopHash));
        }

        if (maxHeaders <= 0)
        {
            throw new ArgumentException("Max headers must be positive.", nameof(maxHeaders));
        }

        LocatorHash = locatorHash;
        StopHash = stopHash;
        MaxHeaders = maxHeaders;
    }

    /// <summary>
    /// Serializes the message to a byte array.
    /// </summary>
    /// <returns>The serialized message.</returns>
    protected override byte[] Serialize()
    {
        // Format: [32 bytes locator hash][1 byte has stop hash][0 or 32 bytes stop hash][4 bytes max headers]
        var hasStopHash = StopHash.Length > 0;
        var totalLength = _hashSize + 1 + (hasStopHash ? _hashSize : 0) + 4;
        var buffer = new byte[totalLength];
        var offset = 0;

        LocatorHash.Span.CopyTo(buffer.AsSpan(offset, _hashSize));
        offset += _hashSize;

        buffer[offset] = (byte)(hasStopHash ? 1 : 0);
        offset += 1;

        if (hasStopHash)
        {
            StopHash.Span.CopyTo(buffer.AsSpan(offset, _hashSize));
            offset += _hashSize;
        }

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset, 4), MaxHeaders);

        return buffer;
    }

    /// <summary>
    /// Deserializes a message from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidDataException">Thrown when the data format is invalid.</exception>
    internal static GetHeadersMessage Deserialize(ReadOnlyMemory<byte> data)
    {
        var span = data.Span;
        if (span.Length < _hashSize + 1 + 4)
        {
            throw new InvalidDataException("GetHeaders message too short.");
        }

        var offset = 0;

        var locatorHash = data.Slice(offset, _hashSize);
        offset += _hashSize;

        var hasStopHash = span[offset] != 0;
        offset += 1;

        ReadOnlyMemory<byte> stopHash = ReadOnlyMemory<byte>.Empty;
        if (hasStopHash)
        {
            if (span.Length < offset + _hashSize + 4)
            {
                throw new InvalidDataException("GetHeaders message too short for stop hash.");
            }

            stopHash = data.Slice(offset, _hashSize);
            offset += _hashSize;
        }

        var maxHeaders = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, 4));

        return new GetHeadersMessage(locatorHash, stopHash, maxHeaders);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var locatorHashHex = Convert.ToHexString(LocatorHash.Span);
        var stopHashHex = StopHash.Length > 0 ? Convert.ToHexString(StopHash.Span)[..8] + "..." : "none";
        return $"GetHeaders(From={locatorHashHex[..8]}..., Stop={stopHashHex}, Max={MaxHeaders})";
    }
}
