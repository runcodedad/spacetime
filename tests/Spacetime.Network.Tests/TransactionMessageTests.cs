namespace Spacetime.Network.Tests;

public class TransactionMessageTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesTransactionMessage()
    {
        // Arrange
        var txData = new byte[200];
        Array.Fill(txData, (byte)0xAA);

        // Act
        var message = new TransactionMessage(txData);

        // Assert
        Assert.Equal(200, message.TransactionData.Length);
    }

    [Fact]
    public void Constructor_WithEmptyData_ThrowsArgumentException()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TransactionMessage(emptyData));
    }

    [Fact]
    public void Constructor_WithTooLargeData_ThrowsArgumentException()
    {
        // Arrange
        var largeData = new byte[TransactionMessage.MaxTransactionSize + 1];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TransactionMessage(largeData));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var txData = new byte[200];
        Array.Fill(txData, (byte)0xBB);
        var original = new TransactionMessage(txData);

        // Act
        var serialized = original.Payload;
        var deserialized = TransactionMessage.Deserialize(serialized);

        // Assert
        Assert.True(original.TransactionData.Span.SequenceEqual(deserialized.TransactionData.Span));
    }

    [Fact]
    public void Deserialize_WithEmptyData_ThrowsInvalidDataException()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => TransactionMessage.Deserialize(emptyData));
    }

    [Fact]
    public void Deserialize_WithTooLargeData_ThrowsInvalidDataException()
    {
        // Arrange
        var largeData = new byte[TransactionMessage.MaxTransactionSize + 1];

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => TransactionMessage.Deserialize(largeData));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var txData = new byte[200];
        var message = new TransactionMessage(txData);

        // Act
        var result = message.ToString();

        // Assert
        Assert.Contains("200", result);
    }
}
