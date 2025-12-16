using System.Collections.Concurrent;

namespace Spacetime.Network;

/// <summary>
/// Implements token bucket rate limiting per peer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RateLimiter"/> class.
/// </remarks>
/// <param name="maxTokens">Maximum number of tokens a peer can accumulate. Default is 100.</param>
/// <param name="refillInterval">How often tokens are refilled. Default is 1 second.</param>
/// <param name="refillAmount">Number of tokens to refill per interval. Default is 10.</param>
public sealed class RateLimiter(int maxTokens = 100, TimeSpan? refillInterval = null, int refillAmount = 10)
{
    private readonly ConcurrentDictionary<string, TokenBucket> _peerBuckets = new ConcurrentDictionary<string, TokenBucket>();
    private readonly int _maxTokens = maxTokens;
    private readonly TimeSpan _refillInterval = refillInterval ?? TimeSpan.FromSeconds(1);
    private readonly int _refillAmount = refillAmount;

    /// <summary>
    /// Attempts to consume tokens for a peer.
    /// </summary>
    /// <param name="peerId">The peer ID.</param>
    /// <param name="tokens">The number of tokens to consume. Default is 1.</param>
    /// <returns>True if the tokens were consumed, false if rate limit exceeded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    public bool TryConsume(string peerId, int tokens = 1)
    {
        ArgumentNullException.ThrowIfNull(peerId);

        var bucket = _peerBuckets.GetOrAdd(peerId, _ => new TokenBucket(_maxTokens, _refillInterval, _refillAmount));
        return bucket.TryConsume(tokens);
    }

    /// <summary>
    /// Gets the number of tokens available for a peer.
    /// </summary>
    /// <param name="peerId">The peer ID.</param>
    /// <returns>The number of available tokens.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    public int GetAvailableTokens(string peerId)
    {
        ArgumentNullException.ThrowIfNull(peerId);

        if (_peerBuckets.TryGetValue(peerId, out var bucket))
        {
            return bucket.AvailableTokens;
        }

        return _maxTokens;
    }

    /// <summary>
    /// Removes rate limiting data for a peer.
    /// </summary>
    /// <param name="peerId">The peer ID.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="peerId"/> is null.</exception>
    public void RemovePeer(string peerId)
    {
        ArgumentNullException.ThrowIfNull(peerId);
        _peerBuckets.TryRemove(peerId, out _);
    }

    /// <summary>
    /// Clears all rate limiting data.
    /// </summary>
    public void Clear()
    {
        _peerBuckets.Clear();
    }

    /// <summary>
    /// Represents a token bucket for rate limiting.
    /// </summary>
    private sealed class TokenBucket
    {
        private readonly int _maxTokens;
        private readonly TimeSpan _refillInterval;
        private readonly int _refillAmount;
        private readonly object _lock = new();
        private int _tokens;
        private DateTimeOffset _lastRefill;

        public TokenBucket(int maxTokens, TimeSpan refillInterval, int refillAmount)
        {
            _maxTokens = maxTokens;
            _refillInterval = refillInterval;
            _refillAmount = refillAmount;
            _tokens = maxTokens;
            _lastRefill = DateTimeOffset.UtcNow;
        }

        public int AvailableTokens
        {
            get
            {
                lock (_lock)
                {
                    Refill();
                    return _tokens;
                }
            }
        }

        public bool TryConsume(int tokens)
        {
            lock (_lock)
            {
                Refill();

                if (_tokens >= tokens)
                {
                    _tokens -= tokens;
                    return true;
                }

                return false;
            }
        }

        private void Refill()
        {
            var now = DateTimeOffset.UtcNow;
            var elapsed = now - _lastRefill;

            if (elapsed >= _refillInterval)
            {
                var intervals = (int)(elapsed.TotalMilliseconds / _refillInterval.TotalMilliseconds);
                var tokensToAdd = intervals * _refillAmount;
                _tokens = Math.Min(_maxTokens, _tokens + tokensToAdd);
                _lastRefill = now;
            }
        }
    }
}
