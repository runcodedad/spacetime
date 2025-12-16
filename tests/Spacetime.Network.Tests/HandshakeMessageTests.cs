namespace Spacetime.Network.Tests;

public class HandshakeMessageTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesHandshakeMessage()
    {
        // Arrange & Act
        var message = new HandshakeMessage(1, "node123", "Spacetime/1.0", 1234567890);

        // Assert
        Assert.Equal(1, message.ProtocolVersion);
        Assert.Equal("node123", message.NodeId);
        Assert.Equal("Spacetime/1.0", message.UserAgent);
        Assert.Equal(1234567890, message.Timestamp);
    }

    [Fact]
    public void Constructor_WithNullNodeId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new HandshakeMessage(1, null!, "Spacetime/1.0", 1234567890));
    }

    [Fact]
    public void Constructor_WithNullUserAgent_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new HandshakeMessage(1, "node123", null!, 1234567890));
    }

    [Fact]
    public void Constructor_WithEmptyNodeId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new HandshakeMessage(1, "", "Spacetime/1.0", 1234567890));
    }

    [Fact]
    public void Constructor_WithEmptyUserAgent_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new HandshakeMessage(1, "node123", "", 1234567890));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new HandshakeMessage(1, "node123", "Spacetime/1.0", 1234567890);

        // Act
        var serialized = original.Payload;
        var deserialized = HandshakeMessage.Deserialize(serialized);

        // Assert
        Assert.Equal(original.ProtocolVersion, deserialized.ProtocolVersion);
        Assert.Equal(original.NodeId, deserialized.NodeId);
        Assert.Equal(original.UserAgent, deserialized.UserAgent);
        Assert.Equal(original.Timestamp, deserialized.Timestamp);
    }

    [Fact]
    public void SerializeDeserialize_WithLongStrings_PreservesData()
    {
        // Arrange
        var nodeId = new string('A', 100);
        var userAgent = new string('B', 200);
        var original = new HandshakeMessage(1, nodeId, userAgent, 1234567890);

        // Act
        var serialized = original.Payload;
        var deserialized = HandshakeMessage.Deserialize(serialized);

        // Assert
        Assert.Equal(original.NodeId, deserialized.NodeId);
        Assert.Equal(original.UserAgent, deserialized.UserAgent);
    }

    [Fact]
    public void Deserialize_WithInvalidData_ThrowsInvalidDataException()
    {
        // Arrange
        var invalidData = new byte[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => HandshakeMessage.Deserialize(invalidData));
    }

    [Fact]
    public void Deserialize_WithInvalidNodeIdLength_ThrowsInvalidDataException()
    {
        // Arrange
        var data = new byte[16];
        // Set invalid node ID length
        data[12] = 0xFF;
        data[13] = 0xFF;
        data[14] = 0xFF;
        data[15] = 0xFF;

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => HandshakeMessage.Deserialize(data));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var message = new HandshakeMessage(1, "node123", "Spacetime/1.0", 1234567890);

        // Act
        var result = message.ToString();

        // Assert
        Assert.Contains("node123", result);
        Assert.Contains("Spacetime/1.0", result);
        Assert.Contains("1", result);
    }

    [Fact]
    public void Serialize_ProducesNonEmptyArray()
    {
        // Arrange
        var message = new HandshakeMessage(1, "node123", "Spacetime/1.0", 1234567890);

        // Act
        var serialized = message.Payload;

        // Assert
        Assert.False(serialized.IsEmpty);
        Assert.True(serialized.Length > 16); // Minimum size
    }
}
