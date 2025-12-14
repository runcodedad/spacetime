namespace Spacetime.Network.Tests;

public class BlockMessageTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesBlockMessage()
    {
        // Arrange
        var blockData = new byte[1000];
        Array.Fill(blockData, (byte)0xAA);

        // Act
        var message = new BlockMessage(blockData);

        // Assert
        Assert.Equal(1000, message.BlockData.Length);
    }

    [Fact]
    public void Constructor_WithEmptyData_ThrowsArgumentException()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BlockMessage(emptyData));
    }

    [Fact]
    public void Constructor_WithTooLargeData_ThrowsArgumentException()
    {
        // Arrange
        var largeData = new byte[BlockMessage.MaxBlockSize + 1];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BlockMessage(largeData));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var blockData = new byte[1000];
        Array.Fill(blockData, (byte)0xBB);
        var original = new BlockMessage(blockData);

        // Act
        var serialized = original.Serialize();
        var deserialized = BlockMessage.Deserialize(serialized);

        // Assert
        Assert.True(original.BlockData.Span.SequenceEqual(deserialized.BlockData.Span));
    }

    [Fact]
    public void Deserialize_WithEmptyData_ThrowsInvalidDataException()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => BlockMessage.Deserialize(emptyData));
    }

    [Fact]
    public void Deserialize_WithTooLargeData_ThrowsInvalidDataException()
    {
        // Arrange
        var largeData = new byte[BlockMessage.MaxBlockSize + 1];

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => BlockMessage.Deserialize(largeData));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var blockData = new byte[1000];
        var message = new BlockMessage(blockData);

        // Act
        var result = message.ToString();

        // Assert
        Assert.Contains("1000", result);
    }
}
