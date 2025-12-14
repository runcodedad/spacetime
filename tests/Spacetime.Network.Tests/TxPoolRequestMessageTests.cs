namespace Spacetime.Network.Tests;

public class TxPoolRequestMessageTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesTxPoolRequestMessage()
    {
        // Arrange & Act
        var message = new TxPoolRequestMessage(100, true);

        // Assert
        Assert.Equal(100, message.MaxTransactions);
        Assert.True(message.IncludeTransactionData);
    }

    [Fact]
    public void Constructor_WithZeroMaxTransactions_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TxPoolRequestMessage(0, true));
    }

    [Fact]
    public void Constructor_WithNegativeMaxTransactions_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TxPoolRequestMessage(-1, true));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new TxPoolRequestMessage(100, true);

        // Act
        var serialized = original.Serialize();
        var deserialized = TxPoolRequestMessage.Deserialize(serialized);

        // Assert
        Assert.Equal(original.MaxTransactions, deserialized.MaxTransactions);
        Assert.Equal(original.IncludeTransactionData, deserialized.IncludeTransactionData);
    }

    [Fact]
    public void SerializeDeserialize_WithFalseFlag_PreservesData()
    {
        // Arrange
        var original = new TxPoolRequestMessage(50, false);

        // Act
        var serialized = original.Serialize();
        var deserialized = TxPoolRequestMessage.Deserialize(serialized);

        // Assert
        Assert.Equal(original.MaxTransactions, deserialized.MaxTransactions);
        Assert.False(deserialized.IncludeTransactionData);
    }

    [Fact]
    public void Deserialize_WithInvalidMaxTransactions_ThrowsInvalidDataException()
    {
        // Arrange
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(-1); // invalid max transactions
        writer.Write(true);

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => TxPoolRequestMessage.Deserialize(ms.ToArray()));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var message = new TxPoolRequestMessage(100, true);

        // Act
        var result = message.ToString();

        // Assert
        Assert.Contains("100", result);
        Assert.Contains("True", result, StringComparison.OrdinalIgnoreCase);
    }
}
