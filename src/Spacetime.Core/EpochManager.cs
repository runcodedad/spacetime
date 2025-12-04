namespace Spacetime.Core;

/// <summary>
/// Manages epoch transitions and challenge generation.
/// </summary>
/// <remarks>
/// This implementation:
/// - Tracks the current epoch and challenge
/// - Enforces challenge window timing
/// - Derives challenges deterministically from previous block hash
/// - Ensures thread-safe epoch transitions
/// - Prevents replay attacks by ensuring unique challenges per epoch
/// </remarks>
public sealed class EpochManager : IEpochManager
{
    private readonly EpochConfig _config;
    private readonly object _lock = new();
    private long _currentEpoch;
    private byte[] _currentChallenge;
    private DateTimeOffset _epochStartTime;

    /// <summary>
    /// Gets the current epoch number.
    /// </summary>
    public long CurrentEpoch
    {
        get
        {
            lock (_lock)
            {
                return _currentEpoch;
            }
        }
    }

    /// <summary>
    /// Gets the current challenge for this epoch.
    /// </summary>
    public ReadOnlyMemory<byte> CurrentChallenge
    {
        get
        {
            lock (_lock)
            {
                return _currentChallenge;
            }
        }
    }

    /// <summary>
    /// Gets the UTC timestamp when the current epoch started.
    /// </summary>
    public DateTimeOffset EpochStartTime
    {
        get
        {
            lock (_lock)
            {
                return _epochStartTime;
            }
        }
    }

    /// <summary>
    /// Gets the remaining time in the current epoch.
    /// </summary>
    public TimeSpan TimeRemainingInEpoch
    {
        get
        {
            lock (_lock)
            {
                var elapsed = DateTimeOffset.UtcNow - _epochStartTime;
                var epochDuration = TimeSpan.FromSeconds(_config.EpochDurationSeconds);
                var remaining = epochDuration - elapsed;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current epoch has expired.
    /// </summary>
    public bool IsEpochExpired
    {
        get
        {
            lock (_lock)
            {
                var elapsed = DateTimeOffset.UtcNow - _epochStartTime;
                return elapsed.TotalSeconds >= _config.EpochDurationSeconds;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EpochManager"/> class.
    /// </summary>
    /// <param name="config">The epoch configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    public EpochManager(EpochConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        _config = config;
        _currentEpoch = 0;
        _currentChallenge = new byte[ChallengeDerivation.ChallengeSize];
        _epochStartTime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EpochManager"/> class with default configuration.
    /// </summary>
    public EpochManager() : this(EpochConfig.Default())
    {
    }

    /// <summary>
    /// Advances to the next epoch.
    /// </summary>
    /// <param name="previousBlockHash">The hash of the previous block (or genesis challenge base).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when previousBlockHash has invalid size.</exception>
    public Task AdvanceEpochAsync(ReadOnlyMemory<byte> previousBlockHash, CancellationToken cancellationToken = default)
    {
        if (previousBlockHash.Length != ChallengeDerivation.ChallengeSize)
        {
            throw new ArgumentException(
                $"Previous block hash must be {ChallengeDerivation.ChallengeSize} bytes",
                nameof(previousBlockHash));
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _currentEpoch++;
            _currentChallenge = ChallengeDerivation.DeriveChallenge(previousBlockHash.Span, _currentEpoch);
            _epochStartTime = DateTimeOffset.UtcNow;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates if a challenge is valid for a specific epoch.
    /// </summary>
    /// <param name="challenge">The challenge to validate.</param>
    /// <param name="epochNumber">The epoch number.</param>
    /// <param name="previousBlockHash">The hash of the previous block.</param>
    /// <returns>True if the challenge is valid for the epoch; otherwise, false.</returns>
    public bool ValidateChallengeForEpoch(ReadOnlySpan<byte> challenge, long epochNumber, ReadOnlySpan<byte> previousBlockHash)
    {
        if (challenge.Length != ChallengeDerivation.ChallengeSize)
        {
            return false;
        }

        if (previousBlockHash.Length != ChallengeDerivation.ChallengeSize)
        {
            return false;
        }

        if (epochNumber < 0)
        {
            return false;
        }

        return ChallengeDerivation.VerifyChallenge(challenge, previousBlockHash, epochNumber);
    }

    /// <summary>
    /// Resets the epoch manager to a specific state.
    /// </summary>
    /// <param name="epochNumber">The epoch number to reset to.</param>
    /// <param name="challenge">The challenge for this epoch.</param>
    /// <param name="startTime">The start time of this epoch.</param>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid values.</exception>
    public void Reset(long epochNumber, ReadOnlyMemory<byte> challenge, DateTimeOffset startTime)
    {
        if (epochNumber < 0)
        {
            throw new ArgumentException("Epoch number must be non-negative", nameof(epochNumber));
        }

        if (challenge.Length != ChallengeDerivation.ChallengeSize)
        {
            throw new ArgumentException(
                $"Challenge must be {ChallengeDerivation.ChallengeSize} bytes",
                nameof(challenge));
        }

        lock (_lock)
        {
            _currentEpoch = epochNumber;
            _currentChallenge = challenge.ToArray();
            _epochStartTime = startTime;
        }
    }
}
