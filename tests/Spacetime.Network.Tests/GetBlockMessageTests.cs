namespace Spacetime.Network.Tests;

public class GetBlockMessageTests
{
    [Fact]
    public void Constructor_WithValidHash_CreatesGetBlockMessage()
    {
        // Arrange
        var blockHash = new byte[32];
        Array.Fill(blockHash, (byte)0xAA);

        // Act
        var message = new GetBlockMessage(blockHash);

        // Assert
        Assert.Equal(32, message.BlockHash.Length);
    }

    [Fact]
    public void Constructor_WithInvalidHashSize_ThrowsArgumentException()
    {
        // Arrange
        var invalidHash = new byte[16];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GetBlockMessage(invalidHash));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var blockHash = new byte[32];
        Array.Fill(blockHash, (byte)0xAA);
        var original = new GetBlockMessage(blockHash);

        // Act
        var serialized = original.Serialize();
        var deserialized = GetBlockMessage.Deserialize(serialized);

        // Assert
        Assert.True(original.BlockHash.Span.SequenceEqual(deserialized.BlockHash.Span));
    }

    [Fact]
    public void Serialize_ProducesCorrectSize()
    {
        // Arrange
        var blockHash = new byte[32];
        var message = new GetBlockMessage(blockHash);

        // Act
        var serialized = message.Serialize();

        // Assert
        Assert.Equal(32, serialized.Length);
    }

    [Fact]
    public void Deserialize_WithInvalidData_ThrowsInvalidDataException()
    {
        // Arrange
        var invalidData = new byte[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => GetBlockMessage.Deserialize(invalidData));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var blockHash = new byte[32];
        Array.Fill(blockHash, (byte)0xAA);
        var message = new GetBlockMessage(blockHash);

        // Act
        var result = message.ToString();

        // Assert
        Assert.Contains("GetBlock", result);
    }
}
