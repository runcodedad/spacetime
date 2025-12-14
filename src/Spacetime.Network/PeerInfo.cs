using System.Net;

namespace Spacetime.Network;

/// <summary>
/// Represents information about a network peer.
/// </summary>
public sealed class PeerInfo
{
    /// <summary>
    /// Gets the unique identifier for this peer.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the network endpoint (IP address and port) of the peer.
    /// </summary>
    public IPEndPoint EndPoint { get; }

    /// <summary>
    /// Gets the protocol version supported by the peer.
    /// </summary>
    public int ProtocolVersion { get; }

    /// <summary>
    /// Gets the reputation score of the peer.
    /// Higher scores indicate more trustworthy peers.
    /// </summary>
    public int ReputationScore { get; internal set; }

    /// <summary>
    /// Gets the timestamp of the last successful communication with this peer.
    /// </summary>
    public DateTimeOffset LastSeen { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this peer has an active connection tracked by the peer manager.
    /// This is managed by the peer manager and may differ from the underlying socket state.
    /// </summary>
    public bool IsConnected { get; internal set; }

    /// <summary>
    /// Gets the number of consecutive connection failures.
    /// </summary>
    public int FailureCount { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PeerInfo"/> class.
    /// </summary>
    /// <param name="id">The unique peer identifier.</param>
    /// <param name="endPoint">The network endpoint.</param>
    /// <param name="protocolVersion">The protocol version.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> or <paramref name="endPoint"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is empty.</exception>
    public PeerInfo(string id, IPEndPoint endPoint, int protocolVersion)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(endPoint);
        
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Peer ID cannot be empty.", nameof(id));
        }

        Id = id;
        EndPoint = endPoint;
        ProtocolVersion = protocolVersion;
        ReputationScore = 0;
        LastSeen = DateTimeOffset.UtcNow;
        IsConnected = false;
        FailureCount = 0;
    }

    /// <summary>
    /// Updates the last seen timestamp to the current time.
    /// </summary>
    public void UpdateLastSeen()
    {
        LastSeen = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Increments the reputation score of the peer.
    /// </summary>
    /// <param name="amount">The amount to increment by.</param>
    public void IncrementReputation(int amount = 1)
    {
        ReputationScore += amount;
    }

    /// <summary>
    /// Decrements the reputation score of the peer.
    /// </summary>
    /// <param name="amount">The amount to decrement by.</param>
    public void DecrementReputation(int amount = 1)
    {
        ReputationScore -= amount;
    }

    /// <summary>
    /// Increments the failure count.
    /// </summary>
    public void RecordFailure()
    {
        FailureCount++;
    }

    /// <summary>
    /// Resets the failure count to zero.
    /// </summary>
    public void ResetFailureCount()
    {
        FailureCount = 0;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Id}@{EndPoint} (v{ProtocolVersion}, score={ReputationScore}, connected={IsConnected})";
    }
}
