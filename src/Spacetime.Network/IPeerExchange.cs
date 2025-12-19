using System.Net;

namespace Spacetime.Network;

/// <summary>
/// Defines methods for exchanging peer addresses between nodes.
/// </summary>
public interface IPeerExchange
{
    /// <summary>
    /// Requests peer addresses from a connected peer.
    /// </summary>
    /// <param name="connection">The connection to request peers from.</param>
    /// <param name="maxCount">Maximum number of peer addresses to request.</param>
    /// <param name="excludeAddresses">Optional addresses to exclude from the response.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of peer addresses.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    Task<IReadOnlyList<IPEndPoint>> RequestPeersAsync(
        IPeerConnection connection,
        int maxCount = 100,
        IEnumerable<IPEndPoint>? excludeAddresses = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a peer address request and returns a list of known peers.
    /// </summary>
    /// <param name="request">The GetPeers request message.</param>
    /// <param name="requesterId">The ID of the requesting peer.</param>
    /// <returns>A PeerList message containing peer addresses.</returns>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null.</exception>
    PeerListMessage HandlePeerRequest(GetPeersMessage request, string requesterId);

    /// <summary>
    /// Checks if a peer can make another peer request based on rate limiting.
    /// </summary>
    /// <param name="peerId">The ID of the peer.</param>
    /// <returns>True if the peer can make a request, false if rate limited.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    bool CanRequestPeers(string peerId);
}
