using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace Spacetime.Network;

/// <summary>
/// Represents a list of peer addresses exchanged during peer discovery.
/// </summary>
public sealed class PeerListMessage
{
    /// <summary>
    /// Maximum number of peers that can be included in a single message.
    /// </summary>
    public const int MaxPeers = 1000;

    /// <summary>
    /// Gets the list of peer endpoints.
    /// </summary>
    public IReadOnlyList<IPEndPoint> Peers { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PeerListMessage"/> class.
    /// </summary>
    /// <param name="peers">The list of peer endpoints.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peers"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when peer count exceeds maximum.</exception>
    public PeerListMessage(IReadOnlyList<IPEndPoint> peers)
    {
        ArgumentNullException.ThrowIfNull(peers);

        if (peers.Count > MaxPeers)
        {
            throw new ArgumentException($"Peer count cannot exceed {MaxPeers}.", nameof(peers));
        }

        Peers = peers;
    }

    /// <summary>
    /// Serializes the peer list message to a byte array.
    /// </summary>
    /// <returns>The serialized message.</returns>
    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write peer count
        writer.Write(Peers.Count);

        // Write each peer endpoint
        foreach (var peer in Peers)
        {
            var addressBytes = peer.Address.GetAddressBytes();
            writer.Write(addressBytes.Length);
            writer.Write(addressBytes);
            writer.Write(peer.Port);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Deserializes a peer list message from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidDataException">Thrown when the data format is invalid.</exception>
    public static PeerListMessage Deserialize(ReadOnlyMemory<byte> data)
    {
        using var ms = new MemoryStream(data.ToArray());
        using var reader = new BinaryReader(ms);

        var peerCount = reader.ReadInt32();
        if (peerCount < 0 || peerCount > MaxPeers)
        {
            throw new InvalidDataException($"Invalid peer count: {peerCount}");
        }

        var peers = new List<IPEndPoint>(peerCount);
        for (var i = 0; i < peerCount; i++)
        {
            var addressLength = reader.ReadInt32();
            if (addressLength < 0 || addressLength > 16)
            {
                throw new InvalidDataException($"Invalid address length: {addressLength}");
            }

            var addressBytes = reader.ReadBytes(addressLength);
            var address = new IPAddress(addressBytes);
            var port = reader.ReadInt32();

            if (port < 0 || port > 65535)
            {
                throw new InvalidDataException($"Invalid port: {port}");
            }

            peers.Add(new IPEndPoint(address, port));
        }

        return new PeerListMessage(peers);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"PeerList(Count={Peers.Count})";
    }
}
