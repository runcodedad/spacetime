namespace Spacetime.Network;

/// <summary>
/// Provides validation for network messages.
/// </summary>
public static class MessageValidator
{
    /// <summary>
    /// Maximum allowed message payload size (16 MB).
    /// </summary>
    public const int MaxPayloadSize = 16 * 1024 * 1024;

    /// <summary>
    /// Validates a network message for basic structural correctness.
    /// </summary>
    /// <param name="message">The message to validate.</param>
    /// <returns>True if the message is valid; otherwise, false.</returns>
    public static bool ValidateMessage(NetworkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Check payload size
        if (message.Payload.Length > MaxPayloadSize)
        {
            return false;
        }

        // Validate message type-specific requirements
        return message.Type switch
        {
            MessageType.Handshake => ValidateHandshake(message.Payload),
            MessageType.HandshakeAck => ValidateHandshake(message.Payload),
            MessageType.Heartbeat => ValidateHeartbeat(message.Payload),
            MessageType.Ping => ValidatePingPong(message.Payload),
            MessageType.Pong => ValidatePingPong(message.Payload),
            MessageType.GetPeers => true, // Empty message
            MessageType.Peers => ValidatePeerList(message.Payload),
            MessageType.GetHeaders => ValidateGetHeaders(message.Payload),
            MessageType.Headers => ValidateHeaders(message.Payload),
            MessageType.GetBlock => ValidateGetBlock(message.Payload),
            MessageType.Block => ValidateBlock(message.Payload),
            MessageType.Transaction => ValidateTransaction(message.Payload),
            MessageType.NewBlock => ValidateBlock(message.Payload),
            MessageType.ProofSubmission => ValidateProofSubmission(message.Payload),
            MessageType.BlockAccepted => ValidateBlockAccepted(message.Payload),
            MessageType.TxPoolRequest => ValidateTxPoolRequest(message.Payload),
            MessageType.Error => true, // Error messages are freeform
            _ => false // Unknown message type
        };
    }

    private static bool ValidateHandshake(ReadOnlyMemory<byte> payload)
    {
        try
        {
            HandshakeMessage.Deserialize(payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateHeartbeat(ReadOnlyMemory<byte> payload)
    {
        // Heartbeat can be empty or contain optional data
        return payload.Length <= 1024;
    }

    private static bool ValidatePingPong(ReadOnlyMemory<byte> payload)
    {
        try
        {
            PingPongMessage.Deserialize(payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidatePeerList(ReadOnlyMemory<byte> payload)
    {
        try
        {
            PeerListMessage.Deserialize(payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateGetHeaders(ReadOnlyMemory<byte> payload)
    {
        try
        {
            GetHeadersMessage.Deserialize(payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateHeaders(ReadOnlyMemory<byte> payload)
    {
        try
        {
            HeadersMessage.Deserialize(payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateGetBlock(ReadOnlyMemory<byte> payload)
    {
        try
        {
            GetBlockMessage.Deserialize(payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateBlock(ReadOnlyMemory<byte> payload)
    {
        try
        {
            BlockMessage.Deserialize(payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateTransaction(ReadOnlyMemory<byte> payload)
    {
        try
        {
            TransactionMessage.Deserialize(payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateProofSubmission(ReadOnlyMemory<byte> payload)
    {
        try
        {
            ProofSubmissionMessage.Deserialize(payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateBlockAccepted(ReadOnlyMemory<byte> payload)
    {
        try
        {
            BlockAcceptedMessage.Deserialize(payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateTxPoolRequest(ReadOnlyMemory<byte> payload)
    {
        try
        {
            TxPoolRequestMessage.Deserialize(payload);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
