using System.Collections.Concurrent;
using System.Net;

namespace Spacetime.Network;

/// <summary>
/// Implements peer address exchange protocol with rate limiting.
/// </summary>
public sealed class PeerExchange : IPeerExchange
{
    private readonly IPeerAddressBook _addressBook;
    private readonly RateLimiter _rateLimiter;
    private readonly TimeSpan _requestTimeout;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastRequestTime = new();
    private readonly TimeSpan _minRequestInterval;

    /// <summary>
    /// Initializes a new instance of the <see cref="PeerExchange"/> class.
    /// </summary>
    /// <param name="addressBook">The peer address book.</param>
    /// <param name="minRequestInterval">Minimum interval between peer requests per peer. Default is 5 minutes.</param>
    /// <param name="requestTimeout">Timeout for peer requests. Default is 10 seconds.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="addressBook"/> is null.</exception>
    public PeerExchange(
        IPeerAddressBook addressBook,
        TimeSpan? minRequestInterval = null,
        TimeSpan? requestTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(addressBook);

        _addressBook = addressBook;
        _minRequestInterval = minRequestInterval ?? TimeSpan.FromMinutes(5);
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(10);
        _rateLimiter = new RateLimiter(
            maxTokens: 10,
            refillInterval: TimeSpan.FromMinutes(1),
            refillAmount: 1);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IPEndPoint>> RequestPeersAsync(
        IPeerConnection connection,
        int maxCount = 100,
        IEnumerable<IPEndPoint>? excludeAddresses = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var peers = new List<IPEndPoint>();

        try
        {
            // Create request message
            var excludeList = excludeAddresses?
                .Select(ep => $"{ep.Address}:{ep.Port}")
                .ToList();

            var request = new GetPeersMessage(maxCount, excludeList);
            
            // Send request
            await connection.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Wait for response with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_requestTimeout);

            var response = await connection.ReceiveAsync(cts.Token).ConfigureAwait(false);
            if (response is PeerListMessage peerList)
            {
                peers.AddRange(peerList.Peers);
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout or cancellation - return empty list
        }
        catch
        {
            // Other errors - return empty list
        }

        return peers;
    }

    /// <inheritdoc/>
    public PeerListMessage HandlePeerRequest(GetPeersMessage request, string requesterId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requesterId);

        // Check rate limiting
        if (!CanRequestPeers(requesterId))
        {
            return new PeerListMessage(Array.Empty<IPEndPoint>());
        }

        // Record request time
        _lastRequestTime[requesterId] = DateTimeOffset.UtcNow;

        // Consume rate limit token
        _rateLimiter.TryConsume(requesterId);

        // Get excluded endpoints
        var excludeEndPoints = request.ExcludeAddresses
            .Select(ParseEndPoint)
            .Where(ep => ep != null)
            .Cast<IPEndPoint>()
            .ToList();

        // Get best addresses from address book
        var addresses = _addressBook.GetBestAddresses(
            Math.Min(request.MaxCount, PeerListMessage.MaxPeers),
            excludeEndPoints);

        var endPoints = addresses.Select(a => a.EndPoint).ToList();
        return new PeerListMessage(endPoints);
    }

    /// <inheritdoc/>
    public bool CanRequestPeers(string peerId)
    {
        ArgumentNullException.ThrowIfNull(peerId);

        // Check rate limiter
        if (!_rateLimiter.TryConsume(peerId, tokens: 0))
        {
            return false;
        }

        // Check minimum interval
        if (_lastRequestTime.TryGetValue(peerId, out var lastTime))
        {
            var elapsed = DateTimeOffset.UtcNow - lastTime;
            if (elapsed < _minRequestInterval)
            {
                return false;
            }
        }

        return true;
    }

    private static IPEndPoint? ParseEndPoint(string addressString)
    {
        try
        {
            var parts = addressString.Split(':');
            if (parts.Length != 2)
            {
                return null;
            }

            if (!IPAddress.TryParse(parts[0], out var address))
            {
                return null;
            }

            if (!int.TryParse(parts[1], out var port) || port < 1 || port > 65535)
            {
                return null;
            }

            return new IPEndPoint(address, port);
        }
        catch
        {
            return null;
        }
    }
}
