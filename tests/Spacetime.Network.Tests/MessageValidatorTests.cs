namespace Spacetime.Network.Tests;

public class MessageValidatorTests
{
    [Fact]
    public void ValidateMessage_WithNullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => MessageValidator.ValidateMessage(null!));
    }

    [Fact]
    public void ValidateMessage_WithValidHandshake_ReturnsTrue()
    {
        // Arrange
        var handshake = new HandshakeMessage(1, "node123", "Spacetime/1.0", 1234567890);
        var message = new NetworkMessage(MessageType.Handshake, handshake.Serialize());

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithValidPing_ReturnsTrue()
    {
        // Arrange
        var ping = new PingPongMessage(12345, 1234567890);
        var message = new NetworkMessage(MessageType.Ping, ping.Serialize());

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithValidPong_ReturnsTrue()
    {
        // Arrange
        var pong = new PingPongMessage(12345, 1234567890);
        var message = new NetworkMessage(MessageType.Pong, pong.Serialize());

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithGetPeers_ReturnsTrue()
    {
        // Arrange
        var message = NetworkMessage.CreateEmpty(MessageType.GetPeers);

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithValidPeerList_ReturnsTrue()
    {
        // Arrange
        var peers = new List<System.Net.IPEndPoint>
        {
            new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.168.1.1"), 8333)
        };
        var peerList = new PeerListMessage(peers);
        var message = new NetworkMessage(MessageType.Peers, peerList.Serialize());

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithValidGetHeaders_ReturnsTrue()
    {
        // Arrange
        var locatorHash = new byte[32];
        var getHeaders = new GetHeadersMessage(locatorHash, ReadOnlyMemory<byte>.Empty, 100);
        var message = new NetworkMessage(MessageType.GetHeaders, getHeaders.Serialize());

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithValidHeaders_ReturnsTrue()
    {
        // Arrange
        var headers = new List<ReadOnlyMemory<byte>> { new byte[] { 1, 2, 3 } };
        var headersMsg = new HeadersMessage(headers);
        var message = new NetworkMessage(MessageType.Headers, headersMsg.Serialize());

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithValidGetBlock_ReturnsTrue()
    {
        // Arrange
        var blockHash = new byte[32];
        var getBlock = new GetBlockMessage(blockHash);
        var message = new NetworkMessage(MessageType.GetBlock, getBlock.Serialize());

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithValidBlock_ReturnsTrue()
    {
        // Arrange
        var blockData = new byte[1000];
        var block = new BlockMessage(blockData);
        var message = new NetworkMessage(MessageType.Block, block.Serialize());

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithValidTransaction_ReturnsTrue()
    {
        // Arrange
        var txData = new byte[200];
        var tx = new TransactionMessage(txData);
        var message = new NetworkMessage(MessageType.Transaction, tx.Serialize());

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithValidProofSubmission_ReturnsTrue()
    {
        // Arrange
        var proofData = new byte[500];
        var minerId = new byte[33];
        var proof = new ProofSubmissionMessage(proofData, minerId, 100);
        var message = new NetworkMessage(MessageType.ProofSubmission, proof.Serialize());

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithValidBlockAccepted_ReturnsTrue()
    {
        // Arrange
        var blockHash = new byte[32];
        var blockAccepted = new BlockAcceptedMessage(blockHash, 100);
        var message = new NetworkMessage(MessageType.BlockAccepted, blockAccepted.Serialize());

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithValidTxPoolRequest_ReturnsTrue()
    {
        // Arrange
        var txPoolReq = new TxPoolRequestMessage(100, true);
        var message = new NetworkMessage(MessageType.TxPoolRequest, txPoolReq.Serialize());

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithInvalidHandshake_ReturnsFalse()
    {
        // Arrange
        var invalidData = new byte[] { 1, 2, 3 };
        var message = new NetworkMessage(MessageType.Handshake, invalidData);

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateMessage_WithTooLargePayload_ReturnsFalse()
    {
        // Arrange
        var largeData = new byte[MessageValidator.MaxPayloadSize + 1];
        var message = new NetworkMessage(MessageType.Error, largeData);

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateMessage_WithErrorMessage_ReturnsTrue()
    {
        // Arrange
        var errorData = new byte[] { 1, 2, 3 };
        var message = new NetworkMessage(MessageType.Error, errorData);

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }
}
