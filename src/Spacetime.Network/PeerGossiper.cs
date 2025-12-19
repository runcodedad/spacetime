using System.Collections.Concurrent;

namespace Spacetime.Network;

/// <summary>
/// Implements periodic gossiping of peer addresses to maintain network connectivity.
/// </summary>
public sealed class PeerGossiper : IPeerGossiper
{
    private readonly IPeerAddressBook _addressBook;
    private readonly IPeerManager _peerManager;
    private readonly IConnectionManager _connectionManager;
    private readonly TimeSpan _gossipInterval;
    private readonly int _addressesPerGossip;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _recentlySeenAddresses = new();
    private readonly TimeSpan _addressDeduplicationWindow;
    private CancellationTokenSource? _cts;
    private Task? _gossipTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="PeerGossiper"/> class.
    /// </summary>
    /// <param name="addressBook">The peer address book.</param>
    /// <param name="peerManager">The peer manager.</param>
    /// <param name="connectionManager">The connection manager.</param>
    /// <param name="gossipInterval">How often to gossip addresses. Default is 10 minutes.</param>
    /// <param name="addressesPerGossip">Number of addresses to include per gossip. Default is 20.</param>
    /// <param name="addressDeduplicationWindow">Time window for deduplicating addresses. Default is 1 hour.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public PeerGossiper(
        IPeerAddressBook addressBook,
        IPeerManager peerManager,
        IConnectionManager connectionManager,
        TimeSpan? gossipInterval = null,
        int addressesPerGossip = 20,
        TimeSpan? addressDeduplicationWindow = null)
    {
        ArgumentNullException.ThrowIfNull(addressBook);
        ArgumentNullException.ThrowIfNull(peerManager);
        ArgumentNullException.ThrowIfNull(connectionManager);

        if (addressesPerGossip < 1)
        {
            throw new ArgumentException("Addresses per gossip must be at least 1.", nameof(addressesPerGossip));
        }

        _addressBook = addressBook;
        _peerManager = peerManager;
        _connectionManager = connectionManager;
        _gossipInterval = gossipInterval ?? TimeSpan.FromMinutes(10);
        _addressesPerGossip = addressesPerGossip;
        _addressDeduplicationWindow = addressDeduplicationWindow ?? TimeSpan.FromHours(1);
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_gossipTask != null)
        {
            throw new InvalidOperationException("Gossiper is already running.");
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _gossipTask = GossipLoopAsync(_cts.Token);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts == null || _gossipTask == null)
        {
            return;
        }

        _cts.Cancel();

        try
        {
            await _gossipTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _gossipTask = null;
        }
    }

    /// <inheritdoc/>
    public void ProcessReceivedAddresses(IEnumerable<PeerAddress> peerAddresses, string sourceId)
    {
        ArgumentNullException.ThrowIfNull(peerAddresses);
        ArgumentNullException.ThrowIfNull(sourceId);

        foreach (var address in peerAddresses)
        {
            var key = GetAddressKey(address);

            // Check if recently seen (deduplication)
            if (_recentlySeenAddresses.TryGetValue(key, out var lastSeen))
            {
                if (DateTimeOffset.UtcNow - lastSeen < _addressDeduplicationWindow)
                {
                    continue; // Skip recently seen address
                }
            }

            // Add or update in address book
            if (_addressBook.GetAddress(address.EndPoint) == null)
            {
                var gossipedAddress = new PeerAddress(address.EndPoint, $"gossip:{sourceId}");
                _addressBook.AddAddress(gossipedAddress);
            }
            else
            {
                _addressBook.UpdateLastSeen(address.EndPoint);
            }

            // Track as recently seen
            _recentlySeenAddresses[key] = DateTimeOffset.UtcNow;
        }

        // Clean up old entries in deduplication cache
        CleanupDeduplicationCache();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private async Task GossipLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_gossipInterval, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                await GossipAddressesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Log error and continue
            }
        }
    }

    private async Task GossipAddressesAsync(CancellationToken cancellationToken)
    {
        var connectedPeers = _peerManager.ConnectedPeers;
        if (connectedPeers.Count == 0)
        {
            return;
        }

        // Get a subset of good addresses to gossip
        var addressesToGossip = _addressBook.GetBestAddresses(_addressesPerGossip);
        if (addressesToGossip.Count == 0)
        {
            return;
        }

        var endPoints = addressesToGossip.Select(a => a.EndPoint).ToList();
        var message = new PeerListMessage(endPoints);

        // Send to all connected peers
        var gossipTasks = connectedPeers.Select(async peer =>
        {
            try
            {
                var connection = _connectionManager.GetConnection(peer.Id);
                if (connection != null)
                {
                    await connection.SendAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }
            catch
            {
                // Ignore errors for individual peers
            }
        });

        await Task.WhenAll(gossipTasks).ConfigureAwait(false);
    }

    private static string GetAddressKey(PeerAddress address)
    {
        return $"{address.EndPoint.Address}:{address.EndPoint.Port}";
    }

    private void CleanupDeduplicationCache()
    {
        var cutoff = DateTimeOffset.UtcNow - _addressDeduplicationWindow;
        var oldEntries = _recentlySeenAddresses
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in oldEntries)
        {
            _recentlySeenAddresses.TryRemove(key, out _);
        }
    }
}
