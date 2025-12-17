namespace Spacetime.Network;

/// <summary>
/// Defines the interface for message relay and broadcasting functionality.
/// </summary>
public interface IMessageRelay : IAsyncDisposable
{
    /// <summary>
    /// Gets the total number of messages relayed since startup.
    /// </summary>
    long TotalMessagesRelayed { get; }

    /// <summary>
    /// Gets the total number of duplicate messages filtered.
    /// </summary>
    long TotalDuplicatesFiltered { get; }

    /// <summary>
    /// Gets the total number of messages dropped due to rate limiting.
    /// </summary>
    long TotalMessagesDropped { get; }

    /// <summary>
    /// Broadcasts a message to all connected peers except the source.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <param name="sourcePeerId">The peer ID that originated the message (to avoid sending back).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    Task BroadcastAsync(NetworkMessage message, string? sourcePeerId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Relays a received message to other peers if it passes validation and deduplication checks.
    /// </summary>
    /// <param name="message">The message to relay.</param>
    /// <param name="sourcePeerId">The peer ID that sent the message.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the message was relayed, false if it was filtered or dropped.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> or <paramref name="sourcePeerId"/> is null.</exception>
    Task<bool> RelayAsync(NetworkMessage message, string sourcePeerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a message should be relayed based on deduplication and validation.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <returns>True if the message should be relayed, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    bool ShouldRelay(NetworkMessage message);
}
