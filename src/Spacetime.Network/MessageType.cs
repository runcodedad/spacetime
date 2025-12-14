namespace Spacetime.Network;

/// <summary>
/// Defines the types of messages that can be sent between peers in the network.
/// </summary>
public enum MessageType : byte
{
    /// <summary>
    /// Handshake message sent when establishing a connection.
    /// </summary>
    Handshake = 0x01,

    /// <summary>
    /// Response to a handshake message.
    /// </summary>
    HandshakeAck = 0x02,

    /// <summary>
    /// Heartbeat message to keep connection alive.
    /// </summary>
    Heartbeat = 0x03,

    /// <summary>
    /// Request for list of known peers.
    /// </summary>
    GetPeers = 0x10,

    /// <summary>
    /// Response containing list of peers.
    /// </summary>
    Peers = 0x11,

    /// <summary>
    /// Request for block headers.
    /// </summary>
    GetHeaders = 0x20,

    /// <summary>
    /// Response containing block headers.
    /// </summary>
    Headers = 0x21,

    /// <summary>
    /// Request for a complete block.
    /// </summary>
    GetBlock = 0x22,

    /// <summary>
    /// Response containing a complete block.
    /// </summary>
    Block = 0x23,

    /// <summary>
    /// Broadcast a new transaction to the network.
    /// </summary>
    Transaction = 0x30,

    /// <summary>
    /// Broadcast a new block to the network.
    /// </summary>
    NewBlock = 0x31,

    /// <summary>
    /// Submit a proof in response to a challenge.
    /// </summary>
    ProofSubmission = 0x40,

    /// <summary>
    /// Generic error message.
    /// </summary>
    Error = 0xFF
}
