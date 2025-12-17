using System.Net;

namespace Spacetime.Network;

/// <summary>
/// Represents a peer address with metadata for peer discovery and management.
/// </summary>
public sealed record PeerAddress
{
    /// <summary>
    /// Gets the network endpoint (IP address and port) of the peer.
    /// </summary>
    public IPEndPoint EndPoint { get; init; }

    /// <summary>
    /// Gets the timestamp when this address was first discovered.
    /// </summary>
    public DateTimeOffset FirstSeen { get; init; }

    /// <summary>
    /// Gets the timestamp of the last successful connection or communication.
    /// </summary>
    public DateTimeOffset LastSeen { get; init; }

    /// <summary>
    /// Gets the timestamp when this address was last attempted for connection.
    /// </summary>
    public DateTimeOffset LastAttempt { get; init; }

    /// <summary>
    /// Gets the number of successful connections to this address.
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Gets the number of failed connection attempts.
    /// </summary>
    public int FailureCount { get; init; }

    /// <summary>
    /// Gets the source of this peer address (e.g., "seed", "gossip", "manual").
    /// </summary>
    public string Source { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PeerAddress"/> class.
    /// </summary>
    /// <param name="endPoint">The network endpoint.</param>
    /// <param name="source">The source of this address. Default is "unknown".</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endPoint"/> is null.</exception>
    public PeerAddress(IPEndPoint endPoint, string source = "unknown")
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        ArgumentNullException.ThrowIfNull(source);

        EndPoint = endPoint;
        Source = source;
        FirstSeen = DateTimeOffset.UtcNow;
        LastSeen = DateTimeOffset.UtcNow;
        LastAttempt = DateTimeOffset.MinValue;
        SuccessCount = 0;
        FailureCount = 0;
    }

    /// <summary>
    /// Creates a copy of this address with updated LastSeen timestamp.
    /// </summary>
    /// <returns>A new instance with updated LastSeen.</returns>
    public PeerAddress WithUpdatedLastSeen()
    {
        return this with { LastSeen = DateTimeOffset.UtcNow };
    }

    /// <summary>
    /// Creates a copy of this address with updated LastAttempt timestamp.
    /// </summary>
    /// <returns>A new instance with updated LastAttempt.</returns>
    public PeerAddress WithUpdatedLastAttempt()
    {
        return this with { LastAttempt = DateTimeOffset.UtcNow };
    }

    /// <summary>
    /// Creates a copy of this address with incremented SuccessCount.
    /// </summary>
    /// <returns>A new instance with incremented SuccessCount.</returns>
    public PeerAddress WithRecordedSuccess()
    {
        return this with 
        { 
            SuccessCount = SuccessCount + 1,
            FailureCount = 0,
            LastSeen = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates a copy of this address with incremented FailureCount.
    /// </summary>
    /// <returns>A new instance with incremented FailureCount.</returns>
    public PeerAddress WithRecordedFailure()
    {
        return this with { FailureCount = FailureCount + 1 };
    }

    /// <summary>
    /// Gets the connection quality score based on success/failure ratio.
    /// </summary>
    public double QualityScore
    {
        get
        {
            var totalAttempts = SuccessCount + FailureCount;
            if (totalAttempts == 0)
            {
                return 0.5; // Neutral score for untested addresses
            }
            return (double)SuccessCount / totalAttempts;
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{EndPoint} (source={Source}, quality={QualityScore:F2}, last_seen={LastSeen:s})";
    }
}
