namespace Spacetime.Network.Tests;

public class PingPongMessageTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesPingPongMessage()
    {
        // Arrange & Act
        var message = new PingPongMessage(12345L, 1234567890L);

        // Assert
        Assert.Equal(MessageType.Ping, message.Type);
        Assert.Equal(12345L, message.Nonce);
        Assert.Equal(1234567890L, message.Timestamp);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new PingPongMessage(98765L, 9876543210L);

        // Act
        var serialized = original.Payload;
        var deserialized = PingPongMessage.Deserialize(serialized);

        // Assert
        Assert.Equal(original.Type, deserialized.Type);
        Assert.Equal(original.Nonce, deserialized.Nonce);
        Assert.Equal(original.Timestamp, deserialized.Timestamp);
    }

    [Fact]
    public void Serialize_ProducesCorrectSize()
    {
        // Arrange
        var message = new PingPongMessage(123L, 456L);

        // Act
        var serialized = message.Payload;

        // Assert
        Assert.Equal(16, serialized.Length); // 8 bytes nonce + 8 bytes timestamp
    }

    [Fact]
    public void Deserialize_WithInvalidData_ThrowsInvalidDataException()
    {
        // Arrange
        var invalidData = new byte[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => 
            PingPongMessage.Deserialize(invalidData));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var message = new PingPongMessage(12345L, 1234567890L);

        // Act
        var result = message.ToString();

        // Assert
        Assert.Contains("12345", result);
        Assert.Contains("1234567890", result);
    }
}
