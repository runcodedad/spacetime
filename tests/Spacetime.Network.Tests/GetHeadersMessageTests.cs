namespace Spacetime.Network.Tests;

public class GetHeadersMessageTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesGetHeadersMessage()
    {
        // Arrange
        var locatorHash = new byte[32];
        Array.Fill(locatorHash, (byte)0xAA);
        var stopHash = new byte[32];
        Array.Fill(stopHash, (byte)0xBB);

        // Act
        var message = new GetHeadersMessage(locatorHash, stopHash, 100);

        // Assert
        Assert.Equal(32, message.LocatorHash.Length);
        Assert.Equal(32, message.StopHash.Length);
        Assert.Equal(100, message.MaxHeaders);
    }

    [Fact]
    public void Constructor_WithEmptyStopHash_CreatesGetHeadersMessage()
    {
        // Arrange
        var locatorHash = new byte[32];
        Array.Fill(locatorHash, (byte)0xAA);

        // Act
        var message = new GetHeadersMessage(locatorHash, ReadOnlyMemory<byte>.Empty, 100);

        // Assert
        Assert.Equal(32, message.LocatorHash.Length);
        Assert.Equal(0, message.StopHash.Length);
        Assert.Equal(100, message.MaxHeaders);
    }

    [Fact]
    public void Constructor_WithInvalidLocatorHashSize_ThrowsArgumentException()
    {
        // Arrange
        var invalidHash = new byte[16];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new GetHeadersMessage(invalidHash, ReadOnlyMemory<byte>.Empty, 100));
    }

    [Fact]
    public void Constructor_WithInvalidStopHashSize_ThrowsArgumentException()
    {
        // Arrange
        var locatorHash = new byte[32];
        var invalidStopHash = new byte[16];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new GetHeadersMessage(locatorHash, invalidStopHash, 100));
    }

    [Fact]
    public void Constructor_WithZeroMaxHeaders_ThrowsArgumentException()
    {
        // Arrange
        var locatorHash = new byte[32];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new GetHeadersMessage(locatorHash, ReadOnlyMemory<byte>.Empty, 0));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var locatorHash = new byte[32];
        Array.Fill(locatorHash, (byte)0xAA);
        var stopHash = new byte[32];
        Array.Fill(stopHash, (byte)0xBB);
        var original = new GetHeadersMessage(locatorHash, stopHash, 100);

        // Act
        var serialized = original.Payload;
        var deserialized = GetHeadersMessage.Deserialize(serialized);

        // Assert
        Assert.True(original.LocatorHash.Span.SequenceEqual(deserialized.LocatorHash.Span));
        Assert.True(original.StopHash.Span.SequenceEqual(deserialized.StopHash.Span));
        Assert.Equal(original.MaxHeaders, deserialized.MaxHeaders);
    }

    [Fact]
    public void SerializeDeserialize_WithEmptyStopHash_PreservesData()
    {
        // Arrange
        var locatorHash = new byte[32];
        Array.Fill(locatorHash, (byte)0xAA);
        var original = new GetHeadersMessage(locatorHash, ReadOnlyMemory<byte>.Empty, 100);

        // Act
        var serialized = original.Payload;
        var deserialized = GetHeadersMessage.Deserialize(serialized);

        // Assert
        Assert.True(original.LocatorHash.Span.SequenceEqual(deserialized.LocatorHash.Span));
        Assert.Equal(0, deserialized.StopHash.Length);
        Assert.Equal(original.MaxHeaders, deserialized.MaxHeaders);
    }

    [Fact]
    public void Deserialize_WithInvalidData_ThrowsInvalidDataException()
    {
        // Arrange
        var invalidData = new byte[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => GetHeadersMessage.Deserialize(invalidData));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var locatorHash = new byte[32];
        Array.Fill(locatorHash, (byte)0xAA);
        var message = new GetHeadersMessage(locatorHash, ReadOnlyMemory<byte>.Empty, 100);

        // Act
        var result = message.ToString();

        // Assert
        Assert.Contains("100", result);
    }
}
