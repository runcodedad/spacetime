using System.Collections.Concurrent;

namespace Spacetime.Network;

/// <summary>
/// Monitors and manages bandwidth usage for message relay.
/// Tracks bytes sent per peer and enforces bandwidth limits.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BandwidthMonitor"/> class.
/// </remarks>
/// <param name="maxBytesPerSecondPerPeer">Maximum bytes per second per peer. Default is 1 MB/s.</param>
/// <param name="maxTotalBytesPerSecond">Maximum total bytes per second across all peers. Default is 10 MB/s.</param>
public sealed class BandwidthMonitor(long maxBytesPerSecondPerPeer = 1_048_576, long maxTotalBytesPerSecond = 10_485_760)
{
    private readonly ConcurrentDictionary<string, BandwidthTracker> _peerTrackers = new ConcurrentDictionary<string, BandwidthTracker>();
    private readonly long _maxBytesPerSecondPerPeer = maxBytesPerSecondPerPeer;
    private readonly long _maxTotalBytesPerSecond = maxTotalBytesPerSecond;
    private long _totalBytesThisSecond = 0;
    private DateTimeOffset _currentSecond = GetCurrentSecond();
    private readonly object _lock = new();

    /// <summary>
    /// Gets the total bytes sent in the current second.
    /// </summary>
    public long TotalBytesThisSecond
    {
        get
        {
            lock (_lock)
            {
                ResetIfNewSecond();
                return _totalBytesThisSecond;
            }
        }
    }

    /// <summary>
    /// Gets the total bytes sent to all peers since startup.
    /// </summary>
    public long TotalBytesSent
    {
        get
        {
            return _peerTrackers.Values.Sum(t => t.TotalBytes);
        }
    }

    /// <summary>
    /// Checks if sending data to a peer would exceed bandwidth limits.
    /// </summary>
    /// <param name="peerId">The peer ID.</param>
    /// <param name="bytes">The number of bytes to send.</param>
    /// <returns>True if the send is allowed, false if it would exceed limits.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    public bool CanSend(string peerId, int bytes)
    {
        ArgumentNullException.ThrowIfNull(peerId);

        lock (_lock)
        {
            ResetIfNewSecond();

            // Check global bandwidth limit
            if (_totalBytesThisSecond + bytes > _maxTotalBytesPerSecond)
            {
                return false;
            }

            // Check per-peer bandwidth limit
            var tracker = _peerTrackers.GetOrAdd(peerId, _ => new BandwidthTracker());
            return tracker.BytesThisSecond + bytes <= _maxBytesPerSecondPerPeer;
        }
    }

    /// <summary>
    /// Records bytes sent to a peer.
    /// </summary>
    /// <param name="peerId">The peer ID.</param>
    /// <param name="bytes">The number of bytes sent.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    public void RecordSent(string peerId, int bytes)
    {
        ArgumentNullException.ThrowIfNull(peerId);

        lock (_lock)
        {
            ResetIfNewSecond();

            _totalBytesThisSecond += bytes;
            var tracker = _peerTrackers.GetOrAdd(peerId, _ => new BandwidthTracker());
            tracker.RecordSent(bytes);
        }
    }

    /// <summary>
    /// Gets bandwidth statistics for a peer.
    /// </summary>
    /// <param name="peerId">The peer ID.</param>
    /// <returns>The bandwidth statistics, or null if peer not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    public BandwidthStats? GetPeerStats(string peerId)
    {
        ArgumentNullException.ThrowIfNull(peerId);

        if (_peerTrackers.TryGetValue(peerId, out var tracker))
        {
            lock (_lock)
            {
                ResetIfNewSecond();
                return new BandwidthStats(tracker.TotalBytes, tracker.BytesThisSecond, _maxBytesPerSecondPerPeer);
            }
        }

        return null;
    }

    /// <summary>
    /// Removes tracking data for a peer.
    /// </summary>
    /// <param name="peerId">The peer ID.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    public void RemovePeer(string peerId)
    {
        ArgumentNullException.ThrowIfNull(peerId);
        _peerTrackers.TryRemove(peerId, out _);
    }

    /// <summary>
    /// Resets counters if we've moved to a new second.
    /// </summary>
    private void ResetIfNewSecond()
    {
        var now = GetCurrentSecond();
        if (now > _currentSecond)
        {
            _currentSecond = now;
            _totalBytesThisSecond = 0;

            foreach (var tracker in _peerTrackers.Values)
            {
                tracker.ResetSecond();
            }
        }
    }

    /// <summary>
    /// Gets the current second as a DateTimeOffset.
    /// </summary>
    private static DateTimeOffset GetCurrentSecond()
    {
        var now = DateTimeOffset.UtcNow;
        return new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, TimeSpan.Zero);
    }

    /// <summary>
    /// Tracks bandwidth usage for a single peer.
    /// </summary>
    private sealed class BandwidthTracker
    {
        public long TotalBytes { get; private set; }
        public long BytesThisSecond { get; private set; }

        public void RecordSent(int bytes)
        {
            TotalBytes += bytes;
            BytesThisSecond += bytes;
        }

        public void ResetSecond()
        {
            BytesThisSecond = 0;
        }
    }

    /// <summary>
    /// Represents bandwidth statistics for a peer.
    /// </summary>
    /// <param name="TotalBytes">Total bytes sent to the peer since tracking started.</param>
    /// <param name="BytesThisSecond">Bytes sent to the peer in the current second.</param>
    /// <param name="MaxBytesPerSecond">Maximum bytes per second allowed for the peer.</param>
    public sealed record BandwidthStats(long TotalBytes, long BytesThisSecond, long MaxBytesPerSecond);
}
