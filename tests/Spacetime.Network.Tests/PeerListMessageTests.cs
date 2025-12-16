using System.Net;

namespace Spacetime.Network.Tests;

public class PeerListMessageTests
{
    [Fact]
    public void Constructor_WithValidPeers_CreatesPeerListMessage()
    {
        // Arrange
        var peers = new List<IPEndPoint>
        {
            new IPEndPoint(IPAddress.Parse("192.168.1.1"), 8333),
            new IPEndPoint(IPAddress.Parse("192.168.1.2"), 8334)
        };

        // Act
        var message = new PeerListMessage(peers);

        // Assert
        Assert.Equal(2, message.Peers.Count);
        Assert.Equal(peers[0], message.Peers[0]);
        Assert.Equal(peers[1], message.Peers[1]);
    }

    [Fact]
    public void Constructor_WithNullPeers_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PeerListMessage(null!));
    }

    [Fact]
    public void Constructor_WithTooManyPeers_ThrowsArgumentException()
    {
        // Arrange
        var peers = new List<IPEndPoint>();
        for (var i = 0; i < PeerListMessage.MaxPeers + 1; i++)
        {
            peers.Add(new IPEndPoint(IPAddress.Parse("192.168.1.1"), 8333 + i));
        }

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PeerListMessage(peers));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var peers = new List<IPEndPoint>
        {
            new IPEndPoint(IPAddress.Parse("192.168.1.1"), 8333),
            new IPEndPoint(IPAddress.Parse("10.0.0.1"), 8334),
            new IPEndPoint(IPAddress.Parse("::1"), 8335)
        };
        var original = new PeerListMessage(peers);

        // Act
        var serialized = original.Payload;
        var deserialized = PeerListMessage.Deserialize(serialized);

        // Assert
        Assert.Equal(original.Peers.Count, deserialized.Peers.Count);
        for (var i = 0; i < original.Peers.Count; i++)
        {
            Assert.Equal(original.Peers[i], deserialized.Peers[i]);
        }
    }

    [Fact]
    public void SerializeDeserialize_WithEmptyList_PreservesData()
    {
        // Arrange
        var peers = new List<IPEndPoint>();
        var original = new PeerListMessage(peers);

        // Act
        var serialized = original.Payload;
        var deserialized = PeerListMessage.Deserialize(serialized);

        // Assert
        Assert.Empty(deserialized.Peers);
    }

    [Fact]
    public void Deserialize_WithInvalidPeerCount_ThrowsInvalidDataException()
    {
        // Arrange
        var data = new byte[4];
        Array.Fill(data, (byte)0xFF);

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => PeerListMessage.Deserialize(data));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var peers = new List<IPEndPoint>
        {
            new IPEndPoint(IPAddress.Parse("192.168.1.1"), 8333)
        };
        var message = new PeerListMessage(peers);

        // Act
        var result = message.ToString();

        // Assert
        Assert.Contains("1", result);
    }
}
