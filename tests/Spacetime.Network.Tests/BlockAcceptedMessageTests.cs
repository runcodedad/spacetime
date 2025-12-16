namespace Spacetime.Network.Tests;

public class BlockAcceptedMessageTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesBlockAcceptedMessage()
    {
        // Arrange
        var blockHash = new byte[32];
        Array.Fill(blockHash, (byte)0xAA);

        // Act
        var message = new BlockAcceptedMessage(blockHash, 100);

        // Assert
        Assert.Equal(32, message.BlockHash.Length);
        Assert.Equal(100, message.BlockHeight);
    }

    [Fact]
    public void Constructor_WithInvalidHashSize_ThrowsArgumentException()
    {
        // Arrange
        var invalidHash = new byte[16];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BlockAcceptedMessage(invalidHash, 0));
    }

    [Fact]
    public void Constructor_WithNegativeBlockHeight_ThrowsArgumentException()
    {
        // Arrange
        var blockHash = new byte[32];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BlockAcceptedMessage(blockHash, -1));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var blockHash = new byte[32];
        Array.Fill(blockHash, (byte)0xAA);
        var original = new BlockAcceptedMessage(blockHash, 100);

        // Act
        var serialized = original.Payload;
        var deserialized = BlockAcceptedMessage.Deserialize(serialized);

        // Assert
        Assert.True(original.BlockHash.Span.SequenceEqual(deserialized.BlockHash.Span));
        Assert.Equal(original.BlockHeight, deserialized.BlockHeight);
    }

    [Fact]
    public void Serialize_ProducesCorrectSize()
    {
        // Arrange
        var blockHash = new byte[32];
        var message = new BlockAcceptedMessage(blockHash, 100);

        // Act
        var serialized = message.Payload;

        // Assert
        Assert.Equal(40, serialized.Length); // 32 bytes hash + 8 bytes height
    }

    [Fact]
    public void Deserialize_WithInvalidData_ThrowsInvalidDataException()
    {
        // Arrange
        var invalidData = new byte[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => BlockAcceptedMessage.Deserialize(invalidData));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var blockHash = new byte[32];
        Array.Fill(blockHash, (byte)0xAA);
        var message = new BlockAcceptedMessage(blockHash, 100);

        // Act
        var result = message.ToString();

        // Assert
        Assert.Contains("100", result);
    }
}
