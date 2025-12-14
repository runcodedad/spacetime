namespace Spacetime.Network.Tests;

public class HeadersMessageTests
{
    [Fact]
    public void Constructor_WithValidHeaders_CreatesHeadersMessage()
    {
        // Arrange
        var headers = new List<ReadOnlyMemory<byte>>
        {
            new byte[] { 1, 2, 3, 4 },
            new byte[] { 5, 6, 7, 8 }
        };

        // Act
        var message = new HeadersMessage(headers);

        // Assert
        Assert.Equal(2, message.Headers.Count);
    }

    [Fact]
    public void Constructor_WithNullHeaders_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HeadersMessage(null!));
    }

    [Fact]
    public void Constructor_WithTooManyHeaders_ThrowsArgumentException()
    {
        // Arrange
        var headers = new List<ReadOnlyMemory<byte>>();
        for (var i = 0; i < HeadersMessage.MaxHeaders + 1; i++)
        {
            headers.Add(new byte[] { 1, 2, 3 });
        }

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new HeadersMessage(headers));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var headers = new List<ReadOnlyMemory<byte>>
        {
            new byte[] { 1, 2, 3, 4 },
            new byte[] { 5, 6, 7, 8, 9 }
        };
        var original = new HeadersMessage(headers);

        // Act
        var serialized = original.Serialize();
        var deserialized = HeadersMessage.Deserialize(serialized);

        // Assert
        Assert.Equal(original.Headers.Count, deserialized.Headers.Count);
        for (var i = 0; i < original.Headers.Count; i++)
        {
            Assert.True(original.Headers[i].Span.SequenceEqual(deserialized.Headers[i].Span));
        }
    }

    [Fact]
    public void SerializeDeserialize_WithEmptyList_PreservesData()
    {
        // Arrange
        var headers = new List<ReadOnlyMemory<byte>>();
        var original = new HeadersMessage(headers);

        // Act
        var serialized = original.Serialize();
        var deserialized = HeadersMessage.Deserialize(serialized);

        // Assert
        Assert.Empty(deserialized.Headers);
    }

    [Fact]
    public void Deserialize_WithInvalidHeaderCount_ThrowsInvalidDataException()
    {
        // Arrange
        var data = new byte[4];
        Array.Fill(data, (byte)0xFF);

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => HeadersMessage.Deserialize(data));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var headers = new List<ReadOnlyMemory<byte>>
        {
            new byte[] { 1, 2, 3 }
        };
        var message = new HeadersMessage(headers);

        // Act
        var result = message.ToString();

        // Assert
        Assert.Contains("1", result);
    }
}
