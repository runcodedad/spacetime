using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Spacetime.Network;

/// <summary>
/// Priority queue for message relay with support for different priority levels.
/// Messages are dequeued in priority order (highest first), with FIFO within each priority level.
/// </summary>
public sealed class PriorityMessageQueue : IAsyncDisposable
{
    private readonly Channel<QueuedMessage>[] _channels;
    private readonly CancellationTokenSource _disposalCts;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PriorityMessageQueue"/> class.
    /// </summary>
    /// <param name="capacity">Maximum capacity per priority level. Default is 1000.</param>
    public PriorityMessageQueue(int capacity = 1000)
    {
        _channels = new Channel<QueuedMessage>[4]; // One channel per priority level
        for (int i = 0; i < _channels.Length; i++)
        {
            _channels[i] = Channel.CreateBounded<QueuedMessage>(new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
        }
        _disposalCts = new CancellationTokenSource();
    }

    /// <summary>
    /// Gets the total number of messages currently queued across all priority levels.
    /// </summary>
    public int Count
    {
        get
        {
            var count = 0;
            foreach (var channel in _channels)
            {
                count += channel.Reader.Count;
            }
            return count;
        }
    }

    /// <summary>
    /// Enqueues a message with the specified priority.
    /// </summary>
    /// <param name="message">The message to enqueue.</param>
    /// <param name="targetPeerId">The target peer ID to send the message to.</param>
    /// <param name="priority">The priority level.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> or <paramref name="targetPeerId"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the queue has been disposed.</exception>
    public async ValueTask EnqueueAsync(NetworkMessage message, string targetPeerId, MessagePriority priority, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(targetPeerId);

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PriorityMessageQueue));
        }

        var queuedMessage = new QueuedMessage(message, targetPeerId, priority);
        var channel = _channels[(int)priority];

        await channel.Writer.WriteAsync(queuedMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Dequeues the highest priority message available.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The dequeued message, or null if the queue is empty and closed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the queue has been disposed.</exception>
    public async ValueTask<QueuedMessage?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PriorityMessageQueue));
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposalCts.Token);

        // Check priorities from highest to lowest
        for (int i = _channels.Length - 1; i >= 0; i--)
        {
            var channel = _channels[i];
            if (channel.Reader.TryRead(out var message))
            {
                return message;
            }
        }

        // Wait for any message from any priority level
        var tasks = _channels.Select((ch, idx) => ch.Reader.ReadAsync(cts.Token).AsTask()).ToArray();

        try
        {
            var completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
            return await completedTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_disposed)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the message priority for a given message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>The priority level for the message type.</returns>
    public static MessagePriority GetPriorityForMessageType(MessageType messageType)
    {
        return messageType switch
        {
            MessageType.Block or MessageType.NewBlock or MessageType.BlockAccepted => MessagePriority.High,
            MessageType.ProofSubmission => MessagePriority.Normal,
            MessageType.Transaction => MessagePriority.Low,
            MessageType.GetHeaders or MessageType.Headers => MessagePriority.Normal,
            MessageType.GetBlock => MessagePriority.Normal,
            MessageType.Ping or MessageType.Pong or MessageType.Heartbeat => MessagePriority.Critical,
            _ => MessagePriority.Normal
        };
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _disposalCts.CancelAsync().ConfigureAwait(false);

        // Complete all writers to signal no more messages
        foreach (var channel in _channels)
        {
            channel.Writer.Complete();
        }

        _disposalCts.Dispose();
    }

    /// <summary>
    /// Represents a message queued for relay.
    /// </summary>
    public sealed record QueuedMessage(NetworkMessage Message, string TargetPeerId, MessagePriority Priority);
}
