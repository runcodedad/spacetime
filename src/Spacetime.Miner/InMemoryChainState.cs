using Spacetime.Consensus;

namespace Spacetime.Miner;

/// <summary>
/// A minimal in-memory chain state implementation for miners.
/// </summary>
/// <remarks>
/// This is a simplified implementation for miners that don't maintain
/// full chain state. In production, miners should query chain state
/// from the full node they're connected to.
/// </remarks>
internal sealed class InMemoryChainState : IChainState
{
    private long _tipHeight;
    private byte[]? _tipHash;
    private long _difficulty;
    private long _expectedEpoch;
    private byte[] _expectedChallenge;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryChainState"/> class.
    /// </summary>
    public InMemoryChainState()
    {
        _tipHeight = -1; // Empty chain
        _difficulty = 1000; // Default difficulty
        _expectedEpoch = 0;
        _expectedChallenge = new byte[32];
    }

    /// <inheritdoc/>
    public Task<byte[]?> GetChainTipHashAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_tipHash);
        }
    }

    /// <inheritdoc/>
    public Task<long> GetChainTipHeightAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_tipHeight);
        }
    }

    /// <inheritdoc/>
    public Task<long> GetExpectedDifficultyAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_difficulty);
        }
    }

    /// <inheritdoc/>
    public Task<long> GetExpectedEpochAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_expectedEpoch);
        }
    }

    /// <inheritdoc/>
    public Task<byte[]> GetExpectedChallengeAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_expectedChallenge);
        }
    }

    /// <summary>
    /// Updates the chain tip (for testing/simulation purposes).
    /// </summary>
    internal void UpdateTip(long height, byte[] hash, long difficulty)
    {
        lock (_lock)
        {
            _tipHeight = height;
            _tipHash = hash;
            _difficulty = difficulty;
        }
    }

    /// <summary>
    /// Updates the expected epoch and challenge.
    /// </summary>
    internal void UpdateExpectedEpoch(long epoch, byte[] challenge)
    {
        lock (_lock)
        {
            _expectedEpoch = epoch;
            _expectedChallenge = challenge;
        }
    }
}
