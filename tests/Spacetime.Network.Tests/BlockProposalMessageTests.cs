namespace Spacetime.Network.Tests;

public class BlockProposalMessageTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesBlockProposalMessage()
    {
        // Arrange
        var blockData = new byte[1000];
        Array.Fill(blockData, (byte)0xAA);

        // Act
        var message = new BlockProposalMessage(blockData);

        // Assert
        Assert.Equal(1000, message.BlockData.Length);
    }

    [Fact]
    public void Constructor_WithEmptyData_ThrowsArgumentException()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BlockProposalMessage(emptyData));
    }

    [Fact]
    public void Constructor_WithTooLargeData_ThrowsArgumentException()
    {
        // Arrange
        var largeData = new byte[BlockProposalMessage.MaxBlockSize + 1];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BlockProposalMessage(largeData));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var blockData = new byte[1000];
        Array.Fill(blockData, (byte)0xBB);
        var original = new BlockProposalMessage(blockData);

        // Act
        var serialized = original.Serialize();
        var deserialized = BlockProposalMessage.Deserialize(serialized);

        // Assert
        Assert.True(original.BlockData.Span.SequenceEqual(deserialized.BlockData.Span));
    }

    [Fact]
    public void Deserialize_WithEmptyData_ThrowsInvalidDataException()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => BlockProposalMessage.Deserialize(emptyData));
    }

    [Fact]
    public void Deserialize_WithTooLargeData_ThrowsInvalidDataException()
    {
        // Arrange
        var largeData = new byte[BlockProposalMessage.MaxBlockSize + 1];

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => BlockProposalMessage.Deserialize(largeData));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var blockData = new byte[1000];
        var message = new BlockProposalMessage(blockData);

        // Act
        var result = message.ToString();

        // Assert
        Assert.Contains("1000", result);
    }
}
