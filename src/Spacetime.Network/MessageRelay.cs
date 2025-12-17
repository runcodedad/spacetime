using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Spacetime.Network;

/// <summary>
/// Implements message relay and broadcasting with deduplication, rate limiting, and priority queuing.
/// </summary>
public sealed class MessageRelay : IMessageRelay
{
    private readonly IConnectionManager _connectionManager;
    private readonly IPeerManager _peerManager;
    private readonly MessageTracker _messageTracker;
    private readonly RateLimiter _rateLimiter;
    private readonly BandwidthMonitor _bandwidthMonitor;
    private readonly PriorityMessageQueue _messageQueue;
    private readonly ILogger<MessageRelay> _logger;
    private readonly CancellationTokenSource _disposalCts;
    private readonly Task _relayWorker;

    private long _totalMessagesRelayed;
    private long _totalDuplicatesFiltered;
    private long _totalMessagesDropped;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageRelay"/> class.
    /// </summary>
    /// <param name="connectionManager">The connection manager.</param>
    /// <param name="peerManager">The peer manager.</param>
    /// <param name="messageTracker">The message tracker for deduplication. If null, a default instance is created.</param>
    /// <param name="rateLimiter">The rate limiter. If null, a default instance is created.</param>
    /// <param name="bandwidthMonitor">The bandwidth monitor. If null, a default instance is created.</param>
    /// <param name="logger">The logger. If null, a null logger is used.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public MessageRelay(
        IConnectionManager connectionManager,
        IPeerManager peerManager,
        MessageTracker? messageTracker = null,
        RateLimiter? rateLimiter = null,
        BandwidthMonitor? bandwidthMonitor = null,
        ILogger<MessageRelay>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);
        ArgumentNullException.ThrowIfNull(peerManager);

        _connectionManager = connectionManager;
        _peerManager = peerManager;
        _messageTracker = messageTracker ?? new MessageTracker();
        _rateLimiter = rateLimiter ?? new RateLimiter();
        _bandwidthMonitor = bandwidthMonitor ?? new BandwidthMonitor();
        _messageQueue = new PriorityMessageQueue();
        _logger = logger ?? NullLogger<MessageRelay>.Instance;
        _disposalCts = new CancellationTokenSource();

        _totalMessagesRelayed = 0;
        _totalDuplicatesFiltered = 0;
        _totalMessagesDropped = 0;

        // Start background worker for message relay
        _relayWorker = Task.Run(RelayWorkerAsync);
    }

    /// <inheritdoc/>
    public long TotalMessagesRelayed => Interlocked.Read(ref _totalMessagesRelayed);

    /// <inheritdoc/>
    public long TotalDuplicatesFiltered => Interlocked.Read(ref _totalDuplicatesFiltered);

    /// <inheritdoc/>
    public long TotalMessagesDropped => Interlocked.Read(ref _totalMessagesDropped);

    /// <inheritdoc/>
    public async Task BroadcastAsync(NetworkMessage message, string? sourcePeerId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MessageRelay));
        }

        // Validate the message
        if (!MessageValidator.ValidateMessage(message))
        {
            _logger.LogWarning("Invalid message not broadcast: {MessageType}", message.Type);
            Interlocked.Increment(ref _totalMessagesDropped);
            return;
        }

        // Mark as seen to prevent relay loops
        _messageTracker.MarkAndCheckIfNew(message);

        var priority = PriorityMessageQueue.GetPriorityForMessageType(message.Type);
        var connections = _connectionManager.GetActiveConnections();

        foreach (var connection in connections)
        {
            // Skip source peer
            if (sourcePeerId != null && connection.PeerInfo.Id == sourcePeerId)
            {
                continue;
            }

            // Skip disconnected peers
            if (!connection.IsConnected)
            {
                continue;
            }

            // Queue the message for sending
            try
            {
                await _messageQueue.EnqueueAsync(message, connection.PeerInfo.Id, priority, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue message for peer {PeerId}", connection.PeerInfo.Id);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RelayAsync(NetworkMessage message, string sourcePeerId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sourcePeerId);

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MessageRelay));
        }

        // Check if message should be relayed
        if (!ShouldRelay(message))
        {
            return false;
        }

        // Check rate limit for source peer
        var messageSize = message.Payload.Length;
        if (!_rateLimiter.TryConsume(sourcePeerId, tokens: 1))
        {
            _logger.LogWarning("Rate limit exceeded for peer {PeerId}, message dropped", sourcePeerId);
            Interlocked.Increment(ref _totalMessagesDropped);
            _peerManager.RecordFailure(sourcePeerId);
            return false;
        }

        // Mark as seen (this is a new message if we got here)
        _messageTracker.MarkAndCheckIfNew(message);

        // Broadcast to other peers
        await BroadcastAsync(message, sourcePeerId, cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc/>
    public bool ShouldRelay(NetworkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Validate message structure
        if (!MessageValidator.ValidateMessage(message))
        {
            _logger.LogDebug("Message validation failed: {MessageType}", message.Type);
            Interlocked.Increment(ref _totalMessagesDropped);
            return false;
        }

        // Check for duplicates
        if (_messageTracker.HasSeen(message))
        {
            _logger.LogDebug("Duplicate message detected: {MessageType}", message.Type);
            Interlocked.Increment(ref _totalDuplicatesFiltered);
            return false;
        }

        // Only relay specific message types
        var shouldRelay = message.Type switch
        {
            MessageType.Block => true,
            MessageType.NewBlock => true,
            MessageType.Transaction => true,
            MessageType.ProofSubmission => true,
            MessageType.BlockAccepted => true,
            _ => false
        };

        if (!shouldRelay)
        {
            _logger.LogDebug("Message type not eligible for relay: {MessageType}", message.Type);
        }

        return shouldRelay;
    }

    /// <summary>
    /// Background worker that processes queued messages and sends them to peers.
    /// </summary>
    private async Task RelayWorkerAsync()
    {
        _logger.LogInformation("Message relay worker started");

        try
        {
            while (!_disposalCts.Token.IsCancellationRequested)
            {
                // Dequeue the next highest priority message
                var queuedMessage = await _messageQueue.DequeueAsync(_disposalCts.Token).ConfigureAwait(false);
                if (queuedMessage == null)
                {
                    break;
                }

                var message = queuedMessage.Message;
                var peerId = queuedMessage.TargetPeerId;

                try
                {
                    // Find the connection
                    var connections = _connectionManager.GetActiveConnections();
                    var connection = connections.FirstOrDefault(c => c.PeerInfo.Id == peerId);

                    if (connection == null || !connection.IsConnected)
                    {
                        _logger.LogDebug("Peer {PeerId} not connected, skipping message", peerId);
                        continue;
                    }

                    // Check bandwidth limits
                    var messageSize = message.Payload.Length;
                    if (!_bandwidthMonitor.CanSend(peerId, messageSize))
                    {
                        _logger.LogWarning("Bandwidth limit exceeded for peer {PeerId}, message dropped", peerId);
                        Interlocked.Increment(ref _totalMessagesDropped);
                        continue;
                    }

                    // Send the message
                    await connection.SendAsync(message, _disposalCts.Token).ConfigureAwait(false);

                    // Record bandwidth usage
                    _bandwidthMonitor.RecordSent(peerId, messageSize);

                    // Record success
                    _peerManager.RecordSuccess(peerId);
                    Interlocked.Increment(ref _totalMessagesRelayed);

                    _logger.LogDebug("Relayed {MessageType} to peer {PeerId} ({Size} bytes)",
                        message.Type, peerId, messageSize);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to relay message to peer {PeerId}", peerId);
                    _peerManager.RecordFailure(peerId);
                    Interlocked.Increment(ref _totalMessagesDropped);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message relay worker encountered an error");
        }

        _logger.LogInformation("Message relay worker stopped");
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _logger.LogInformation("Disposing message relay service");

        // Cancel background worker
        await _disposalCts.CancelAsync().ConfigureAwait(false);

        // Wait for worker to complete
        try
        {
            await _relayWorker.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Dispose components
        await _messageQueue.DisposeAsync().ConfigureAwait(false);
        _disposalCts.Dispose();

        _logger.LogInformation("Message relay service disposed. Stats - Relayed: {Relayed}, Duplicates: {Duplicates}, Dropped: {Dropped}",
            _totalMessagesRelayed, _totalDuplicatesFiltered, _totalMessagesDropped);
    }
}
