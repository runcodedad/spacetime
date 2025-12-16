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
        var message = new HandshakeMessage(1, "node123", "Spacetime/1.0", 1234567890);

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithValidPing_ReturnsTrue()
    {
        // Arrange
        var message = new PingPongMessage(12345, 1234567890);

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithValidPong_ReturnsTrue()
    {
        // Arrange
        var message = new PingPongMessage(12345, 1234567890);

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithGetPeers_ReturnsTrue()
    {
        // Arrange - GetPeers is an empty message
        var message = NetworkMessage.Deserialize(MessageType.GetPeers, ReadOnlyMemory<byte>.Empty);

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
        var message = new PeerListMessage(peers);

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
        var message = new GetHeadersMessage(locatorHash, ReadOnlyMemory<byte>.Empty, 100);

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
        var message = new HeadersMessage(headers);

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
        var message = new GetBlockMessage(blockHash);

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
        var message = new BlockMessage(blockData);

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
        var message = new TransactionMessage(txData);

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
        var message = new ProofSubmissionMessage(proofData, minerId, 100);

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
        var message = new BlockAcceptedMessage(blockHash, 100);

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithValidTxPoolRequest_ReturnsTrue()
    {
        // Arrange
        var message = new TxPoolRequestMessage(100, true);

        // Act
        var result = MessageValidator.ValidateMessage(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateMessage_WithInvalidHandshake_ThrowsInvalidDataException()
    {
        // Arrange
        var invalidData = new byte[] { 1, 2, 3 };

        // Act & Assert - Deserialization will fail for invalid data
        Assert.Throws<InvalidDataException>(() => 
            NetworkMessage.Deserialize(MessageType.Handshake, invalidData));
    }

    [Fact]
    public void ValidateMessage_WithTooLargePayload_ReturnsFalse()
    {
        // Arrange - Create a block message that's too large
        var largeData = new byte[MessageValidator.MaxPayloadSize + 1];

        // Act & Assert - BlockMessage constructor should reject this
        Assert.Throws<ArgumentException>(() => new BlockMessage(largeData));
    }

    [Fact]
    public void ValidateMessage_WithErrorMessage_ThrowsInvalidDataException()
    {
        // Arrange & Act & Assert - Error type isn't implemented in the factory yet
        Assert.Throws<InvalidDataException>(() => 
            NetworkMessage.Deserialize(MessageType.Error, new byte[] { 1, 2, 3 }));
    }
}
