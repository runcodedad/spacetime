namespace Spacetime.Core;

/// <summary>
/// Configuration for epoch timing and challenge management.
/// </summary>
/// <remarks>
/// An epoch is one full challenge cycle:
/// 1. Network issues a challenge
/// 2. Miners compute and submit proofs
/// 3. Network evaluates proofs
/// 4. If a valid proof meets difficulty → block is produced
/// 5. Otherwise → epoch ends with no block
/// 
/// The challenge window defines how long miners have to submit proofs.
/// Many epochs may occur before a block is produced.
/// </remarks>
public sealed record EpochConfig
{
    /// <summary>
    /// Default epoch duration in seconds (challenge window).
    /// </summary>
    public const int DefaultEpochDurationSeconds = 10;

    /// <summary>
    /// Minimum epoch duration in seconds.
    /// </summary>
    public const int MinEpochDurationSeconds = 1;

    /// <summary>
    /// Maximum epoch duration in seconds.
    /// </summary>
    public const int MaxEpochDurationSeconds = 3600;

    /// <summary>
    /// Gets the duration of each epoch in seconds (challenge window).
    /// </summary>
    /// <remarks>
    /// This is the time window during which miners can submit proofs for a challenge.
    /// After this window, proofs for that challenge are rejected and a new epoch begins.
    /// </remarks>
    public int EpochDurationSeconds { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EpochConfig"/> class.
    /// </summary>
    /// <param name="epochDurationSeconds">The duration of each epoch in seconds.</param>
    /// <exception cref="ArgumentException">Thrown when epoch duration is out of valid range.</exception>
    public EpochConfig(int epochDurationSeconds = DefaultEpochDurationSeconds)
    {
        if (epochDurationSeconds < MinEpochDurationSeconds || epochDurationSeconds > MaxEpochDurationSeconds)
        {
            throw new ArgumentException(
                $"Epoch duration must be between {MinEpochDurationSeconds} and {MaxEpochDurationSeconds} seconds",
                nameof(epochDurationSeconds));
        }

        EpochDurationSeconds = epochDurationSeconds;
    }

    /// <summary>
    /// Creates the default epoch configuration.
    /// </summary>
    /// <returns>A new <see cref="EpochConfig"/> with default values.</returns>
    public static EpochConfig Default() => new();
}
