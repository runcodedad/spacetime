using System.Collections.Concurrent;

namespace Spacetime.Network;

/// <summary>
/// Manages the list of known peers and their states.
/// </summary>
public sealed class PeerManager : IPeerManager
{
    private readonly ConcurrentDictionary<string, PeerInfo> _peers = new();
    private readonly int _blacklistThreshold;
    private readonly int _maxFailures;

    /// <summary>
    /// Initializes a new instance of the <see cref="PeerManager"/> class.
    /// </summary>
    /// <param name="blacklistThreshold">The reputation score below which a peer is blacklisted. Default is -10.</param>
    /// <param name="maxFailures">The maximum number of consecutive failures before blacklisting. Default is 5.</param>
    public PeerManager(int blacklistThreshold = -10, int maxFailures = 5)
    {
        _blacklistThreshold = blacklistThreshold;
        _maxFailures = maxFailures;
    }

    /// <inheritdoc/>
    public IReadOnlyList<PeerInfo> KnownPeers => _peers.Values.ToList();

    /// <inheritdoc/>
    public IReadOnlyList<PeerInfo> ConnectedPeers => _peers.Values.Where(p => p.IsConnected).ToList();

    /// <inheritdoc/>
    public bool AddPeer(PeerInfo peerInfo)
    {
        ArgumentNullException.ThrowIfNull(peerInfo);
        return _peers.TryAdd(peerInfo.Id, peerInfo);
    }

    /// <inheritdoc/>
    public bool RemovePeer(string peerId)
    {
        ArgumentNullException.ThrowIfNull(peerId);
        return _peers.TryRemove(peerId, out _);
    }

    /// <inheritdoc/>
    public PeerInfo? GetPeer(string peerId)
    {
        ArgumentNullException.ThrowIfNull(peerId);
        return _peers.TryGetValue(peerId, out var peer) ? peer : null;
    }

    /// <inheritdoc/>
    public void UpdatePeerConnectionStatus(string peerId, bool isConnected)
    {
        ArgumentNullException.ThrowIfNull(peerId);

        if (_peers.TryGetValue(peerId, out var peer))
        {
            peer.IsConnected = isConnected;
            if (isConnected)
            {
                peer.UpdateLastSeen();
                peer.ResetFailureCount();
            }
        }
    }

    /// <inheritdoc/>
    public void RecordSuccess(string peerId)
    {
        ArgumentNullException.ThrowIfNull(peerId);

        if (_peers.TryGetValue(peerId, out var peer))
        {
            peer.UpdateLastSeen();
            peer.IncrementReputation();
            peer.ResetFailureCount();
        }
    }

    /// <inheritdoc/>
    public void RecordFailure(string peerId)
    {
        ArgumentNullException.ThrowIfNull(peerId);

        if (_peers.TryGetValue(peerId, out var peer))
        {
            peer.RecordFailure();
            peer.DecrementReputation(2); // Failures decrease reputation more than successes increase it
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<PeerInfo> GetBestPeers(int count)
    {
        return _peers.Values
            .Where(p => !p.IsConnected && !ShouldBlacklist(p.Id))
            .OrderByDescending(p => p.ReputationScore)
            .ThenBy(p => p.LastSeen)
            .Take(count)
            .ToList();
    }

    /// <inheritdoc/>
    public bool ShouldBlacklist(string peerId)
    {
        ArgumentNullException.ThrowIfNull(peerId);

        if (!_peers.TryGetValue(peerId, out var peer))
        {
            return false;
        }

        return peer.ReputationScore <= _blacklistThreshold || peer.FailureCount >= _maxFailures;
    }
}
