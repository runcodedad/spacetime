namespace Spacetime.Network;

/// <summary>
/// Represents a connection to a remote peer.
/// </summary>
public interface IPeerConnection : IAsyncDisposable
{
    /// <summary>
    /// Gets information about the connected peer.
    /// </summary>
    PeerInfo PeerInfo { get; }

    /// <summary>
    /// Gets a value indicating whether the underlying TCP socket is currently connected and not disposed.
    /// This reflects the actual network socket state.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Sends a message to the peer asynchronously.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the connection is not active.</exception>
    Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives a message from the peer asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The received message, or null if the connection was closed.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the connection is not active.</exception>
    Task<NetworkMessage?> ReceiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the connection gracefully.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CloseAsync(CancellationToken cancellationToken = default);
}
