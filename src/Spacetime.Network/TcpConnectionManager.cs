using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Spacetime.Network;

/// <summary>
/// Manages TCP connections to peers in the network.
/// </summary>
public sealed class TcpConnectionManager : IConnectionManager
{
    private readonly IMessageCodec _codec;
    private readonly IPeerManager _peerManager;
    private readonly ConcurrentDictionary<string, IPeerConnection> _connections = new();
    private readonly int _maxConnections;
    private readonly bool _useTls;
    private readonly TimeSpan _connectionTimeout;
    private readonly TimeSpan _retryDelay;
    private TcpListener? _listener;
    private CancellationTokenSource? _listenerCts;
    private Task? _listenerTask;
    private bool _disposed;

    /// <inheritdoc/>
    public int MaxConnections => _maxConnections;

    /// <inheritdoc/>
    public int ActiveConnectionCount => _connections.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="TcpConnectionManager"/> class.
    /// </summary>
    /// <param name="codec">The message codec to use.</param>
    /// <param name="peerManager">The peer manager.</param>
    /// <param name="maxConnections">The maximum number of concurrent connections. Default is 50.</param>
    /// <param name="useTls">Whether to use TLS encryption. Default is false.</param>
    /// <param name="connectionTimeout">The connection timeout. Default is 10 seconds.</param>
    /// <param name="retryDelay">The delay before retrying after a connection error. Default is 1 second.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxConnections"/> is less than 1.</exception>
    public TcpConnectionManager(
        IMessageCodec codec,
        IPeerManager peerManager,
        int maxConnections = 50,
        bool useTls = false,
        TimeSpan? connectionTimeout = null,
        TimeSpan? retryDelay = null)
    {
        ArgumentNullException.ThrowIfNull(codec);
        ArgumentNullException.ThrowIfNull(peerManager);

        if (maxConnections < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConnections), "Must be at least 1.");
        }

        _codec = codec;
        _peerManager = peerManager;
        _maxConnections = maxConnections;
        _useTls = useTls;
        _connectionTimeout = connectionTimeout ?? TimeSpan.FromSeconds(10);
        _retryDelay = retryDelay ?? TimeSpan.FromSeconds(1);
    }

    /// <inheritdoc/>
    public async Task StartAsync(IPEndPoint listenEndPoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(listenEndPoint);

        if (_listener != null)
        {
            throw new InvalidOperationException("Connection manager is already started.");
        }

        _listener = new TcpListener(listenEndPoint);
        _listener.Start();

        _listenerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listenerTask = AcceptConnectionsAsync(_listenerCts.Token);
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_listener == null)
        {
            return;
        }

        _listenerCts?.Cancel();
        _listener.Stop();

        if (_listenerTask != null)
        {
            try
            {
                await _listenerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        // Close all active connections
        var disconnectTasks = _connections.Values.Select(c => c.CloseAsync()).ToList();
        await Task.WhenAll(disconnectTasks).ConfigureAwait(false);
        _connections.Clear();

        _listener = null;
        _listenerCts?.Dispose();
        _listenerCts = null;
        _listenerTask = null;
    }

    /// <inheritdoc/>
    public async Task<IPeerConnection?> ConnectAsync(IPEndPoint endPoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        if (ActiveConnectionCount >= _maxConnections)
        {
            return null;
        }

        var client = new TcpClient();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_connectionTimeout);

            await client.ConnectAsync(endPoint.Address, endPoint.Port, cts.Token).ConfigureAwait(false);

            // Generate a temporary peer ID until handshake completes
            var peerId = $"peer_{Guid.NewGuid():N}";
            var peerInfo = new PeerInfo(peerId, endPoint, 1);

            var connection = await TcpPeerConnection.CreateAsync(
                client,
                peerInfo,
                _codec,
                _useTls,
                cancellationToken).ConfigureAwait(false);

            if (_connections.TryAdd(peerId, connection))
            {
                _peerManager.AddPeer(peerInfo);
                _peerManager.UpdatePeerConnectionStatus(peerId, true);
                return connection;
            }

            await connection.DisposeAsync().ConfigureAwait(false);
            return null;
        }
        catch (Exception)
        {
            client.Dispose();
            return null;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<IPeerConnection> GetActiveConnections()
    {
        return _connections.Values.ToList();
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(string peerId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peerId);

        if (_connections.TryRemove(peerId, out var connection))
        {
            _peerManager.UpdatePeerConnectionStatus(peerId, false);
            await connection.CloseAsync(cancellationToken).ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);

                if (ActiveConnectionCount >= _maxConnections)
                {
                    client.Close();
                    continue;
                }

                _ = HandleIncomingConnectionAsync(client, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // Log error and continue
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_retryDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task HandleIncomingConnectionAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            var remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint!;
            var peerId = $"peer_{Guid.NewGuid():N}";
            var peerInfo = new PeerInfo(peerId, remoteEndPoint, 1);

            var connection = await TcpPeerConnection.CreateAsync(
                client,
                peerInfo,
                _codec,
                _useTls,
                cancellationToken).ConfigureAwait(false);

            if (_connections.TryAdd(peerId, connection))
            {
                _peerManager.AddPeer(peerInfo);
                _peerManager.UpdatePeerConnectionStatus(peerId, true);
            }
            else
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            client.Dispose();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAsync().ConfigureAwait(false);
    }
}
