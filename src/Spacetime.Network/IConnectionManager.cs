using System.Net;

namespace Spacetime.Network;

/// <summary>
/// Manages TCP connections to peers in the network.
/// </summary>
public interface IConnectionManager : IAsyncDisposable
{
    /// <summary>
    /// Gets the maximum number of concurrent peer connections to maintain.
    /// </summary>
    int MaxConnections { get; }

    /// <summary>
    /// Gets the current number of active connections.
    /// </summary>
    int ActiveConnectionCount { get; }

    /// <summary>
    /// Starts the connection manager and begins listening for incoming connections.
    /// </summary>
    /// <param name="listenEndPoint">The local endpoint to listen on.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="listenEndPoint"/> is null.</exception>
    Task StartAsync(IPEndPoint listenEndPoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the connection manager and closes all active connections.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to a remote peer.
    /// </summary>
    /// <param name="endPoint">The endpoint of the remote peer.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A connection to the peer, or null if the connection failed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endPoint"/> is null.</exception>
    Task<IPeerConnection?> ConnectAsync(IPEndPoint endPoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active peer connections.
    /// </summary>
    /// <returns>A read-only list of active connections.</returns>
    IReadOnlyList<IPeerConnection> GetActiveConnections();

    /// <summary>
    /// Disconnects from a specific peer.
    /// </summary>
    /// <param name="peerId">The ID of the peer to disconnect.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    Task DisconnectAsync(string peerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a connection to a specific peer.
    /// </summary>
    /// <param name="peerId">The ID of the peer.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The connection, or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    Task<IPeerConnection?> GetConnectionAsync(string peerId, CancellationToken cancellationToken = default);
}
