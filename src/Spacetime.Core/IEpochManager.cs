namespace Spacetime.Core;

/// <summary>
/// Manages epoch transitions and challenge generation.
/// </summary>
/// <remarks>
/// The epoch manager is responsible for:
/// - Tracking current epoch number and timing
/// - Deriving challenges from previous block hash
/// - Enforcing challenge window timing
/// - Signaling epoch transitions
/// - Ensuring unique challenges per epoch (anti-replay)
/// </remarks>
public interface IEpochManager
{
    /// <summary>
    /// Gets the current epoch number.
    /// </summary>
    long CurrentEpoch { get; }

    /// <summary>
    /// Gets the current challenge for this epoch.
    /// </summary>
    ReadOnlyMemory<byte> CurrentChallenge { get; }

    /// <summary>
    /// Gets the UTC timestamp when the current epoch started.
    /// </summary>
    DateTimeOffset EpochStartTime { get; }

    /// <summary>
    /// Gets the remaining time in the current epoch.
    /// </summary>
    TimeSpan TimeRemainingInEpoch { get; }

    /// <summary>
    /// Gets a value indicating whether the current epoch has expired.
    /// </summary>
    bool IsEpochExpired { get; }

    /// <summary>
    /// Advances to the next epoch.
    /// </summary>
    /// <param name="previousBlockHash">The hash of the previous block (or genesis challenge base).</param>
    /// <exception cref="ArgumentException">Thrown when previousBlockHash has invalid size.</exception>
    void AdvanceEpoch(ReadOnlyMemory<byte> previousBlockHash);

    /// <summary>
    /// Validates if a challenge is valid for a specific epoch.
    /// </summary>
    /// <param name="challenge">The challenge to validate.</param>
    /// <param name="epochNumber">The epoch number.</param>
    /// <param name="previousBlockHash">The hash of the previous block.</param>
    /// <returns>True if the challenge is valid for the epoch; otherwise, false.</returns>
    bool ValidateChallengeForEpoch(ReadOnlySpan<byte> challenge, long epochNumber, ReadOnlySpan<byte> previousBlockHash);

    /// <summary>
    /// Resets the epoch manager to a specific state.
    /// </summary>
    /// <param name="epochNumber">The epoch number to reset to.</param>
    /// <param name="challenge">The challenge for this epoch.</param>
    /// <param name="startTime">The start time of this epoch.</param>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid values.</exception>
    void Reset(long epochNumber, ReadOnlyMemory<byte> challenge, DateTimeOffset startTime);
}
