namespace Spacetime.Network;

/// <summary>
/// Defines priority levels for message relay.
/// Higher priority messages are relayed first.
/// </summary>
public enum MessagePriority : byte
{
    /// <summary>
    /// Low priority - regular transactions.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority - proofs, peer messages.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority - blocks, block accepted notifications.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority - urgent network messages.
    /// </summary>
    Critical = 3
}
