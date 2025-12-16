namespace Spacetime.Network.Tests;

public class LengthPrefixedMessageCodecTests
{
    private readonly LengthPrefixedMessageCodec _codec = new();

    [Fact]
    public void Encode_WithValidMessage_EncodesCorrectly()
    {
        // Arrange
        var message = new HandshakeMessage(1, "node123", "TestAgent/1.0", 1234567890);

        // Act
        var encoded = _codec.Encode(message);

        // Assert
        Assert.NotNull(encoded);
        Assert.True(encoded.Length > 5); // At least header size
        Assert.Equal((byte)MessageType.Handshake, encoded[4]);
    }

    [Fact]
    public void Encode_WithEmptyPayload_EncodesCorrectly()
    {
        // Arrange - Use Deserialize factory to create empty message
        var message = NetworkMessage.Deserialize(MessageType.Heartbeat, ReadOnlyMemory<byte>.Empty);

        // Act
        var encoded = _codec.Encode(message);

        // Assert
        Assert.NotNull(encoded);
        Assert.Equal(5, encoded.Length); // 4 (length) + 1 (type)
        Assert.Equal((byte)MessageType.Heartbeat, encoded[4]);
    }

    [Fact]
    public void Encode_WithNullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _codec.Encode(null!));
    }

    [Fact]
    public async Task DecodeAsync_WithValidMessage_DecodesCorrectly()
    {
        // Arrange
        var blockData = new byte[1000];
        var originalMessage = new BlockMessage(blockData);
        var encoded = _codec.Encode(originalMessage);
        using var stream = new MemoryStream(encoded);

        // Act
        var decoded = await _codec.DecodeAsync(stream);

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal(MessageType.Block, decoded.Type);
        Assert.IsType<BlockMessage>(decoded);
        var blockMsg = (BlockMessage)decoded;
        Assert.Equal(blockData, blockMsg.BlockData.ToArray());
    }

    [Fact]
    public async Task DecodeAsync_WithEmptyStream_ReturnsNull()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var result = await _codec.DecodeAsync(stream);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DecodeAsync_WithIncompleteLengthHeader_ThrowsInvalidDataException()
    {
        // Arrange
        var buffer = new byte[] { 1, 2 }; // Only 2 bytes instead of 4
        using var stream = new MemoryStream(buffer);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(() => _codec.DecodeAsync(stream));
    }

    [Fact]
    public async Task DecodeAsync_WithInvalidMessageLength_ThrowsInvalidDataException()
    {
        // Arrange
        var buffer = new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }; // Very large length
        using var stream = new MemoryStream(buffer);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(() => _codec.DecodeAsync(stream));
    }

    [Fact]
    public async Task DecodeAsync_WithUnknownMessageType_ThrowsInvalidDataException()
    {
        // Arrange
        var buffer = new byte[] { 1, 0, 0, 0, 0xFE }; // Unknown type 0xFE
        using var stream = new MemoryStream(buffer);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(() => _codec.DecodeAsync(stream));
    }

    [Fact]
    public async Task DecodeAsync_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _codec.DecodeAsync(null!));
    }

    [Fact]
    public async Task EncodeDecodeRoundTrip_PreservesMessage()
    {
        // Arrange
        var txData = new byte[1000];
        Random.Shared.NextBytes(txData);
        var originalMessage = new TransactionMessage(txData);

        // Act
        var encoded = _codec.Encode(originalMessage);
        using var stream = new MemoryStream(encoded);
        var decoded = await _codec.DecodeAsync(stream);

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal(MessageType.Transaction, decoded.Type);
        Assert.IsType<TransactionMessage>(decoded);
        var txMsg = (TransactionMessage)decoded;
        Assert.Equal(txData, txMsg.TransactionData.ToArray());
    }

    [Fact]
    public async Task DecodeAsync_MultipleMessages_DecodesInSequence()
    {
        // Arrange
        var message1 = new HandshakeMessage(1, "node1", "Agent/1.0", 1234567890);
        var message2 = NetworkMessage.Deserialize(MessageType.HandshakeAck, ReadOnlyMemory<byte>.Empty);
        var encoded1 = _codec.Encode(message1);
        var encoded2 = _codec.Encode(message2);
        
        using var stream = new MemoryStream();
        await stream.WriteAsync(encoded1);
        await stream.WriteAsync(encoded2);
        stream.Position = 0;

        // Act
        var decoded1 = await _codec.DecodeAsync(stream);
        var decoded2 = await _codec.DecodeAsync(stream);

        // Assert
        Assert.NotNull(decoded1);
        Assert.Equal(MessageType.Handshake, decoded1.Type);
        Assert.IsType<HandshakeMessage>(decoded1);

        Assert.NotNull(decoded2);
        Assert.Equal(MessageType.HandshakeAck, decoded2.Type);
    }
}
