namespace Spacetime.Core;

/// <summary>
/// Provides challenge broadcasting capabilities to miners.
/// </summary>
/// <remarks>
/// Implementations of this interface are responsible for:
/// - Broadcasting new challenges to connected miners
/// - Tracking which miners have received challenges
/// - Handling network-level challenge distribution
/// - Ensuring timely delivery of challenges
/// 
/// This interface is typically implemented by the networking layer.
/// </remarks>
public interface IChallengeProvider
{
    /// <summary>
    /// Broadcasts a new challenge to all connected miners.
    /// </summary>
    /// <param name="challenge">The challenge to broadcast.</param>
    /// <param name="epochNumber">The epoch number for this challenge.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous broadcast operation.</returns>
    /// <exception cref="ArgumentException">Thrown when challenge has invalid size.</exception>
    Task BroadcastChallengeAsync(ReadOnlyMemory<byte> challenge, long epochNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a new epoch begins and a challenge is available.
    /// </summary>
    event EventHandler<ChallengeEventArgs>? ChallengeAvailable;
}

/// <summary>
/// Provides data for the <see cref="IChallengeProvider.ChallengeAvailable"/> event.
/// </summary>
public sealed class ChallengeEventArgs : EventArgs
{
    /// <summary>
    /// Gets the challenge for the new epoch.
    /// </summary>
    public ReadOnlyMemory<byte> Challenge { get; }

    /// <summary>
    /// Gets the epoch number.
    /// </summary>
    public long EpochNumber { get; }

    /// <summary>
    /// Gets the timestamp when the epoch started.
    /// </summary>
    public DateTimeOffset EpochStartTime { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChallengeEventArgs"/> class.
    /// </summary>
    /// <param name="challenge">The challenge for the new epoch.</param>
    /// <param name="epochNumber">The epoch number.</param>
    /// <param name="epochStartTime">The timestamp when the epoch started.</param>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid values.</exception>
    public ChallengeEventArgs(ReadOnlyMemory<byte> challenge, long epochNumber, DateTimeOffset epochStartTime)
    {
        if (challenge.Length != ChallengeDerivation.ChallengeSize)
        {
            throw new ArgumentException(
                $"Challenge must be {ChallengeDerivation.ChallengeSize} bytes",
                nameof(challenge));
        }

        if (epochNumber < 0)
        {
            throw new ArgumentException("Epoch number must be non-negative", nameof(epochNumber));
        }

        Challenge = challenge;
        EpochNumber = epochNumber;
        EpochStartTime = epochStartTime;
    }
}
