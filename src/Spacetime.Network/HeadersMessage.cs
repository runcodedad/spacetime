namespace Spacetime.Network;

/// <summary>
/// Represents a response containing a list of block headers.
/// </summary>
/// <remarks>
/// This message is sent in response to a GetHeaders request and contains
/// serialized block header data. The headers are serialized using the
/// BlockHeader's native serialization format.
/// </remarks>
public sealed class HeadersMessage : NetworkMessage
{
    /// <summary>
    /// Gets the type of the message.
    /// </summary>
    public override MessageType Type => MessageType.Headers;

    /// <summary>
    /// Maximum number of headers that can be included in a single message.
    /// </summary>
    public const int MaxHeaders = 2000;

    /// <summary>
    /// Maximum size of a single header in bytes (10 MB).
    /// </summary>
    public const int MaxHeaderSize = 10 * 1024 * 1024;

    /// <summary>
    /// Gets the serialized block headers.
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<byte>> Headers { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HeadersMessage"/> class.
    /// </summary>
    /// <param name="headers">The list of serialized block headers.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="headers"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when header count exceeds maximum.</exception>
    public HeadersMessage(IReadOnlyList<ReadOnlyMemory<byte>> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        if (headers.Count > MaxHeaders)
        {
            throw new ArgumentException($"Header count cannot exceed {MaxHeaders}.", nameof(headers));
        }

        Headers = headers;
    }

    /// <summary>
    /// Serializes the headers message to a byte array.
    /// </summary>
    /// <returns>The serialized message.</returns>
    protected override byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write header count
        writer.Write(Headers.Count);

        // Write each header
        foreach (var header in Headers)
        {
            writer.Write(header.Length);
            writer.Write(header.Span);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Deserializes a headers message from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidDataException">Thrown when the data format is invalid.</exception>
    public static HeadersMessage Deserialize(ReadOnlyMemory<byte> data)
    {
        using var ms = new MemoryStream(data.ToArray());
        using var reader = new BinaryReader(ms);

        var headerCount = reader.ReadInt32();
        if (headerCount < 0 || headerCount > MaxHeaders)
        {
            throw new InvalidDataException($"Invalid header count: {headerCount}");
        }

        var headers = new List<ReadOnlyMemory<byte>>(headerCount);
        for (var i = 0; i < headerCount; i++)
        {
            var headerLength = reader.ReadInt32();
            if (headerLength < 0 || headerLength > MaxHeaderSize)
            {
                throw new InvalidDataException($"Invalid header length: {headerLength}");
            }

            var headerData = reader.ReadBytes(headerLength);
            headers.Add(headerData);
        }

        return new HeadersMessage(headers);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Headers(Count={Headers.Count})";
    }
}
