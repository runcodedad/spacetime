namespace Spacetime.Network;

/// <summary>
/// Manages periodic gossiping of peer addresses to connected peers.
/// </summary>
public interface IPeerGossiper : IAsyncDisposable
{
    /// <summary>
    /// Starts the peer address gossiping service.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the peer address gossiping service.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes received peer addresses from gossip messages.
    /// </summary>
    /// <param name="peerAddresses">The peer addresses received.</param>
    /// <param name="sourceId">The ID of the peer that sent the addresses.</param>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null.</exception>
    void ProcessReceivedAddresses(IEnumerable<PeerAddress> peerAddresses, string sourceId);
}
