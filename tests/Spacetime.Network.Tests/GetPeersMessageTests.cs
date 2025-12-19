namespace Spacetime.Network.Tests;

public class GetPeersMessageTests
{
    [Fact]
    public void Constructor_WithDefaultParameters_CreatesInstance()
    {
        // Act
        var message = new GetPeersMessage();

        // Assert
        Assert.Equal(MessageType.GetPeers, message.Type);
        Assert.Equal(100, message.MaxCount);
        Assert.Empty(message.ExcludeAddresses);
    }

    [Fact]
    public void Constructor_WithCustomMaxCount_SetsMaxCount()
    {
        // Act
        var message = new GetPeersMessage(maxCount: 50);

        // Assert
        Assert.Equal(50, message.MaxCount);
    }

    [Fact]
    public void Constructor_WithExcludeAddresses_SetsExcludeList()
    {
        // Arrange
        var excludeList = new List<string> { "192.168.1.1:8000", "192.168.1.2:8000" };

        // Act
        var message = new GetPeersMessage(excludeAddresses: excludeList);

        // Assert
        Assert.Equal(2, message.ExcludeAddresses.Count);
        Assert.Contains("192.168.1.1:8000", message.ExcludeAddresses);
    }

    [Fact]
    public void Constructor_WithMaxCountTooLarge_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GetPeersMessage(maxCount: 10000));
    }

    [Fact]
    public void Constructor_WithMaxCountZero_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GetPeersMessage(maxCount: 0));
    }

    [Fact]
    public void Constructor_WithMaxCountNegative_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GetPeersMessage(maxCount: -1));
    }

    [Fact]
    public void Serialize_WithDefaultParameters_CreatesValidPayload()
    {
        // Arrange
        var message = new GetPeersMessage();

        // Act
        var payload = message.Payload;

        // Assert
        Assert.NotEmpty(payload.ToArray());
    }

    [Fact]
    public void Deserialize_WithValidData_CreatesMessage()
    {
        // Arrange
        var original = new GetPeersMessage(maxCount: 50, excludeAddresses: new List<string> { "192.168.1.1:8000" });
        var payload = original.Payload;

        // Act
        var deserialized = GetPeersMessage.Deserialize(payload);

        // Assert
        Assert.Equal(original.MaxCount, deserialized.MaxCount);
        Assert.Equal(original.ExcludeAddresses.Count, deserialized.ExcludeAddresses.Count);
    }

    [Fact]
    public void Deserialize_WithTooShortData_ThrowsInvalidDataException()
    {
        // Arrange
        var data = new byte[4]; // Too short

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => GetPeersMessage.Deserialize(data));
    }

    [Fact]
    public void Deserialize_WithInvalidMaxCount_ThrowsInvalidDataException()
    {
        // Arrange
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(-1); // Invalid maxCount
        writer.Write(0);  // excludeCount

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => GetPeersMessage.Deserialize(ms.ToArray()));
    }

    [Fact]
    public void Deserialize_WithExceedingMaxCount_ThrowsInvalidDataException()
    {
        // Arrange
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(2000); // Exceeds MaxRequestCount
        writer.Write(0);    // excludeCount

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => GetPeersMessage.Deserialize(ms.ToArray()));
    }

    [Fact]
    public void Deserialize_WithInvalidExcludeCount_ThrowsInvalidDataException()
    {
        // Arrange
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(100);  // maxCount
        writer.Write(-1);   // Invalid excludeCount

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => GetPeersMessage.Deserialize(ms.ToArray()));
    }

    [Fact]
    public void Serialize_AndDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var excludeList = new List<string> { "192.168.1.1:8000", "192.168.1.2:8000", "192.168.1.3:8000" };
        var original = new GetPeersMessage(maxCount: 75, excludeAddresses: excludeList);

        // Act
        var payload = original.Payload;
        var deserialized = GetPeersMessage.Deserialize(payload);

        // Assert
        Assert.Equal(original.MaxCount, deserialized.MaxCount);
        Assert.Equal(original.ExcludeAddresses.Count, deserialized.ExcludeAddresses.Count);
        for (int i = 0; i < original.ExcludeAddresses.Count; i++)
        {
            Assert.Equal(original.ExcludeAddresses[i], deserialized.ExcludeAddresses[i]);
        }
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var message = new GetPeersMessage(maxCount: 50, excludeAddresses: new List<string> { "192.168.1.1:8000" });

        // Act
        var result = message.ToString();

        // Assert
        Assert.Contains("50", result);
        Assert.Contains("1", result);
    }
}
