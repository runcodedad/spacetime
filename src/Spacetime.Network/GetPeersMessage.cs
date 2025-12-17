using System.Buffers.Binary;

namespace Spacetime.Network;

/// <summary>
/// Represents a request for peer addresses from a connected peer.
/// </summary>
public sealed class GetPeersMessage : NetworkMessage
{
    /// <summary>
    /// Gets the type of the message.
    /// </summary>
    public override MessageType Type => MessageType.GetPeers;

    /// <summary>
    /// Maximum number of peers that can be requested.
    /// </summary>
    public const int MaxRequestCount = 1000;

    /// <summary>
    /// Gets the maximum number of peer addresses requested.
    /// </summary>
    public int MaxCount { get; }

    /// <summary>
    /// Gets the list of peer endpoints to exclude from the response.
    /// Typically includes peers the requester already knows about.
    /// </summary>
    public IReadOnlyList<string> ExcludeAddresses { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPeersMessage"/> class.
    /// </summary>
    /// <param name="maxCount">The maximum number of peers to request. Default is 100.</param>
    /// <param name="excludeAddresses">List of addresses to exclude. Default is empty.</param>
    /// <exception cref="ArgumentException">Thrown when maxCount exceeds maximum or is less than 1.</exception>
    public GetPeersMessage(int maxCount = 100, IReadOnlyList<string>? excludeAddresses = null)
    {
        if (maxCount < 1 || maxCount > MaxRequestCount)
        {
            throw new ArgumentException($"MaxCount must be between 1 and {MaxRequestCount}.", nameof(maxCount));
        }

        MaxCount = maxCount;
        ExcludeAddresses = excludeAddresses ?? Array.Empty<string>();
    }

    /// <summary>
    /// Serializes the message to a byte array.
    /// </summary>
    /// <returns>The serialized message.</returns>
    protected override byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write max count
        writer.Write(MaxCount);

        // Write exclude addresses count
        writer.Write(ExcludeAddresses.Count);

        // Write each excluded address
        foreach (var address in ExcludeAddresses)
        {
            writer.Write(address);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Deserializes a GetPeers message from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidDataException">Thrown when the data format is invalid.</exception>
    internal static GetPeersMessage Deserialize(ReadOnlyMemory<byte> data)
    {
        // Handle empty message (legacy format or default request)
        if (data.Length == 0)
        {
            return new GetPeersMessage();
        }

        if (data.Length < 8)
        {
            // Minimum size: 4 bytes maxCount + 4 bytes exclude count
            throw new InvalidDataException("GetPeers message data too short.");
        }

        using var ms = new MemoryStream(data.ToArray());
        using var reader = new BinaryReader(ms);

        var maxCount = reader.ReadInt32();
        if (maxCount < 1 || maxCount > MaxRequestCount)
        {
            throw new InvalidDataException($"Invalid maxCount: {maxCount}");
        }

        var excludeCount = reader.ReadInt32();
        if (excludeCount < 0 || excludeCount > MaxRequestCount)
        {
            throw new InvalidDataException($"Invalid exclude count: {excludeCount}");
        }

        var excludeAddresses = new List<string>(excludeCount);
        for (var i = 0; i < excludeCount; i++)
        {
            try
            {
                var address = reader.ReadString();
                excludeAddresses.Add(address);
            }
            catch (EndOfStreamException)
            {
                throw new InvalidDataException("Unexpected end of data while reading exclude addresses.");
            }
        }

        return new GetPeersMessage(maxCount, excludeAddresses);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"GetPeers(MaxCount={MaxCount}, Exclude={ExcludeAddresses.Count})";
    }
}
