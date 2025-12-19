using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;

namespace Spacetime.Network;

/// <summary>
/// Implements peer discovery functionality using seed nodes and peer exchange.
/// </summary>
public sealed class PeerDiscovery : IPeerDiscovery
{
    private readonly IConnectionManager _connectionManager;
    private readonly IPeerManager _peerManager;
    private readonly ConcurrentBag<IPEndPoint> _seedNodes = new();
    private readonly TimeSpan _requestTimeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="PeerDiscovery"/> class.
    /// </summary>
    /// <param name="connectionManager">The connection manager.</param>
    /// <param name="peerManager">The peer manager.</param>
    /// <param name="requestTimeout">The timeout for peer requests. Default is 5 seconds.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public PeerDiscovery(
        IConnectionManager connectionManager,
        IPeerManager peerManager,
        TimeSpan? requestTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);
        ArgumentNullException.ThrowIfNull(peerManager);

        _connectionManager = connectionManager;
        _peerManager = peerManager;
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(5);
    }

    /// <inheritdoc/>
    public void AddSeedNode(IPEndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        _seedNodes.Add(endPoint);
    }

    /// <inheritdoc/>
    public IReadOnlyList<IPEndPoint> GetSeedNodes()
    {
        return _seedNodes.ToList();
    }

    /// <inheritdoc/>
    public async Task DiscoverPeersAsync(CancellationToken cancellationToken = default)
    {
        var seedNodes = _seedNodes.ToList();
        if (seedNodes.Count == 0)
        {
            return;
        }

        // Try to discover peers from seed nodes
        var discoveryTasks = seedNodes.Select(async seedNode =>
        {
            try
            {
                var connection = await _connectionManager.ConnectAsync(seedNode, cancellationToken).ConfigureAwait(false);
                if (connection != null)
                {
                    var peers = await RequestPeersAsync(connection, cancellationToken).ConfigureAwait(false);
                    foreach (var peerEndPoint in peers)
                    {
                        // Add discovered peer to peer manager
                        var peerId = $"peer_{peerEndPoint.Address}_{peerEndPoint.Port}";
                        var peerInfo = new PeerInfo(peerId, peerEndPoint, 1);
                        _peerManager.AddPeer(peerInfo);
                    }
                }
            }
            catch
            {
                // Ignore failures from individual seed nodes
            }
        });

        await Task.WhenAll(discoveryTasks).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IPEndPoint>> RequestPeersAsync(
        IPeerConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var peers = new List<IPEndPoint>();

        try
        {
            // Send GetPeers request
            var getPeersMessage = new GetPeersMessage();
            var request = NetworkMessage.Deserialize(getPeersMessage.Type, getPeersMessage.Payload);
           
            await connection.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Wait for Peers response with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_requestTimeout);

            var response = await connection.ReceiveAsync(cts.Token).ConfigureAwait(false);
            if (response != null && response.Type == getPeersMessage.Type)
            {
                peers = DecodePeerList(response.Payload);
            }
        }
        catch
        {
            // Return empty list on error
        }

        return peers;
    }

    private static List<IPEndPoint> DecodePeerList(ReadOnlyMemory<byte> payload)
    {
        var peers = new List<IPEndPoint>();
        var span = payload.Span;
        var offset = 0;

        if (span.Length < 4)
        {
            return peers;
        }

        // Read peer count
        var peerCount = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, 4));
        offset += 4;

        for (int i = 0; i < peerCount && offset < span.Length; i++)
        {
            // Read IP address type (4 = IPv4, 16 = IPv6)
            if (offset + 1 > span.Length)
            {
                break;
            }

            var addressLength = span[offset];
            offset++;

            if (addressLength != 4 && addressLength != 16)
            {
                break;
            }

            if (offset + addressLength + 2 > span.Length)
            {
                break;
            }

            // Read IP address bytes
            var addressBytes = span.Slice(offset, addressLength).ToArray();
            offset += addressLength;

            // Read port
            var port = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(offset, 2));
            offset += 2;

            try
            {
                var address = new IPAddress(addressBytes);
                peers.Add(new IPEndPoint(address, port));
            }
            catch
            {
                // Skip invalid addresses
            }
        }

        return peers;
    }

    /// <summary>
    /// Encodes a list of peer endpoints into a binary payload.
    /// </summary>
    /// <param name="peers">The list of peer endpoints to encode.</param>
    /// <returns>The encoded binary payload.</returns>
    public static byte[] EncodePeerList(IReadOnlyList<IPEndPoint> peers)
    {
        ArgumentNullException.ThrowIfNull(peers);

        // Calculate required size
        var size = 4; // peer count
        foreach (var peer in peers)
        {
            size += 1; // address length
            size += peer.Address.GetAddressBytes().Length; // address bytes
            size += 2; // port
        }

        var buffer = new byte[size];
        var offset = 0;

        // Write peer count
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset, 4), peers.Count);
        offset += 4;

        foreach (var peer in peers)
        {
            var addressBytes = peer.Address.GetAddressBytes();

            // Write address length
            buffer[offset] = (byte)addressBytes.Length;
            offset++;

            // Write address bytes
            addressBytes.CopyTo(buffer, offset);
            offset += addressBytes.Length;

            // Write port
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset, 2), (ushort)peer.Port);
            offset += 2;
        }

        return buffer;
    }
}
