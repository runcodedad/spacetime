using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Spacetime.Network;

/// <summary>
/// Tracks seen messages to prevent duplicate relays.
/// Uses a sliding window approach with automatic cleanup of old entries.
/// </summary>
public sealed class MessageTracker
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _seenMessages;
    private readonly TimeSpan _messageLifetime;
    private readonly int _maxTrackedMessages;
    private DateTimeOffset _lastCleanup;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageTracker"/> class.
    /// </summary>
    /// <param name="messageLifetime">How long to track messages before they can be seen again. Default is 5 minutes.</param>
    /// <param name="maxTrackedMessages">Maximum number of messages to track. Default is 100,000.</param>
    public MessageTracker(TimeSpan? messageLifetime = null, int maxTrackedMessages = 100_000)
    {
        _messageLifetime = messageLifetime ?? TimeSpan.FromMinutes(5);
        _maxTrackedMessages = maxTrackedMessages;
        _seenMessages = new ConcurrentDictionary<string, DateTimeOffset>();
        _lastCleanup = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the total number of messages currently being tracked.
    /// </summary>
    public int TrackedMessageCount => _seenMessages.Count;

    /// <summary>
    /// Checks if a message has been seen before and marks it as seen if not.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <returns>True if the message is new (not seen before), false if it's a duplicate.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    public bool MarkAndCheckIfNew(NetworkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageHash = ComputeMessageHash(message);
        var now = DateTimeOffset.UtcNow;

        // Try to add the message
        if (_seenMessages.TryAdd(messageHash, now))
        {
            // New message - trigger cleanup if needed
            if (_seenMessages.Count > _maxTrackedMessages || now - _lastCleanup > TimeSpan.FromMinutes(1))
            {
                CleanupOldEntries(now);
            }
            return true;
        }

        // Message already seen - check if it's still within the lifetime window
        if (_seenMessages.TryGetValue(messageHash, out var seenTime))
        {
            if (now - seenTime > _messageLifetime)
            {
                // Old entry, update timestamp and treat as new
                _seenMessages[messageHash] = now;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a message has been seen before without marking it.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <returns>True if the message has been seen, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    public bool HasSeen(NetworkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageHash = ComputeMessageHash(message);
        if (_seenMessages.TryGetValue(messageHash, out var seenTime))
        {
            return DateTimeOffset.UtcNow - seenTime <= _messageLifetime;
        }

        return false;
    }

    /// <summary>
    /// Clears all tracked messages.
    /// </summary>
    public void Clear()
    {
        _seenMessages.Clear();
        _lastCleanup = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Computes a unique hash for a message based on its type and payload.
    /// </summary>
    /// <param name="message">The message to hash.</param>
    /// <returns>A hex string representing the message hash.</returns>
    private static string ComputeMessageHash(NetworkMessage message)
    {
        var payload = message.Payload.Span;
        var hash = SHA256.HashData(payload);
        return $"{(byte)message.Type:X2}:{Convert.ToHexString(hash)}";
    }

    /// <summary>
    /// Removes entries that are older than the message lifetime.
    /// </summary>
    /// <param name="now">The current timestamp.</param>
    private void CleanupOldEntries(DateTimeOffset now)
    {
        _lastCleanup = now;
        var cutoffTime = now - _messageLifetime;

        // Remove expired entries
        var expiredKeys = _seenMessages
            .Where(kvp => kvp.Value < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _seenMessages.TryRemove(key, out _);
        }

        // If still over capacity, remove oldest entries
        if (_seenMessages.Count > _maxTrackedMessages)
        {
            var toRemove = _seenMessages
                .OrderBy(kvp => kvp.Value)
                .Take(_seenMessages.Count - _maxTrackedMessages)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                _seenMessages.TryRemove(key, out _);
            }
        }
    }
}
