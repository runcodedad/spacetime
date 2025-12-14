namespace Spacetime.Network;

/// <summary>
/// Manages the list of known peers and their states.
/// </summary>
public interface IPeerManager
{
    /// <summary>
    /// Gets all known peers.
    /// </summary>
    IReadOnlyList<PeerInfo> KnownPeers { get; }

    /// <summary>
    /// Gets all currently connected peers.
    /// </summary>
    IReadOnlyList<PeerInfo> ConnectedPeers { get; }

    /// <summary>
    /// Adds a peer to the known peer list.
    /// </summary>
    /// <param name="peerInfo">The peer to add.</param>
    /// <returns>True if the peer was added, false if it already exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerInfo"/> is null.</exception>
    bool AddPeer(PeerInfo peerInfo);

    /// <summary>
    /// Removes a peer from the known peer list.
    /// </summary>
    /// <param name="peerId">The ID of the peer to remove.</param>
    /// <returns>True if the peer was removed, false if it was not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    bool RemovePeer(string peerId);

    /// <summary>
    /// Gets a peer by its ID.
    /// </summary>
    /// <param name="peerId">The ID of the peer to retrieve.</param>
    /// <returns>The peer information, or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    PeerInfo? GetPeer(string peerId);

    /// <summary>
    /// Updates the connection status of a peer.
    /// </summary>
    /// <param name="peerId">The ID of the peer.</param>
    /// <param name="isConnected">The new connection status.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    void UpdatePeerConnectionStatus(string peerId, bool isConnected);

    /// <summary>
    /// Records a successful interaction with a peer.
    /// </summary>
    /// <param name="peerId">The ID of the peer.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    void RecordSuccess(string peerId);

    /// <summary>
    /// Records a failed interaction with a peer.
    /// </summary>
    /// <param name="peerId">The ID of the peer.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    void RecordFailure(string peerId);

    /// <summary>
    /// Gets the best peers to connect to based on reputation and availability.
    /// </summary>
    /// <param name="count">The maximum number of peers to return.</param>
    /// <returns>A list of recommended peers.</returns>
    IReadOnlyList<PeerInfo> GetBestPeers(int count);

    /// <summary>
    /// Determines if a peer should be blacklisted based on its reputation and failure count.
    /// </summary>
    /// <param name="peerId">The ID of the peer to check.</param>
    /// <returns>True if the peer should be blacklisted, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    bool ShouldBlacklist(string peerId);
}
