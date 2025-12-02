using System.Security.Cryptography;

namespace Spacetime.Core.Tests;

public class BlockPlotMetadataTests
{
    [Fact]
    public void Create_WithValidParameters_CreatesMetadata()
    {
        // Arrange
        var plotId = RandomNumberGenerator.GetBytes(32);
        var plotHeaderHash = RandomNumberGenerator.GetBytes(32);

        // Act
        var metadata = BlockPlotMetadata.Create(1000, plotId, plotHeaderHash, 1);

        // Assert
        Assert.Equal(1000, metadata.LeafCount);
        Assert.Equal(plotId, metadata.PlotId);
        Assert.Equal(plotHeaderHash, metadata.PlotHeaderHash);
        Assert.Equal(1, metadata.Version);
    }

    [Fact]
    public void Create_WithNullPlotId_ThrowsArgumentNullException()
    {
        // Arrange
        var plotHeaderHash = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            BlockPlotMetadata.Create(1000, null!, plotHeaderHash, 1));
    }

    [Fact]
    public void Create_WithNullPlotHeaderHash_ThrowsArgumentNullException()
    {
        // Arrange
        var plotId = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            BlockPlotMetadata.Create(1000, plotId, null!, 1));
    }

    [Fact]
    public void Create_WithZeroLeafCount_ThrowsArgumentException()
    {
        // Arrange
        var plotId = RandomNumberGenerator.GetBytes(32);
        var plotHeaderHash = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            BlockPlotMetadata.Create(0, plotId, plotHeaderHash, 1));
        Assert.Contains("positive", exception.Message);
    }

    [Fact]
    public void Create_WithNegativeLeafCount_ThrowsArgumentException()
    {
        // Arrange
        var plotId = RandomNumberGenerator.GetBytes(32);
        var plotHeaderHash = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            BlockPlotMetadata.Create(-1, plotId, plotHeaderHash, 1));
        Assert.Contains("positive", exception.Message);
    }

    [Fact]
    public void Create_WithInvalidPlotIdSize_ThrowsArgumentException()
    {
        // Arrange
        var plotId = RandomNumberGenerator.GetBytes(16); // Wrong size
        var plotHeaderHash = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            BlockPlotMetadata.Create(1000, plotId, plotHeaderHash, 1));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void Create_WithInvalidPlotHeaderHashSize_ThrowsArgumentException()
    {
        // Arrange
        var plotId = RandomNumberGenerator.GetBytes(32);
        var plotHeaderHash = RandomNumberGenerator.GetBytes(16); // Wrong size

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            BlockPlotMetadata.Create(1000, plotId, plotHeaderHash, 1));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var plotId = RandomNumberGenerator.GetBytes(32);
        var plotHeaderHash = RandomNumberGenerator.GetBytes(32);
        var original = BlockPlotMetadata.Create(123456, plotId, plotHeaderHash, 2);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Act
        original.Serialize(writer);
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var deserialized = BlockPlotMetadata.Deserialize(reader);

        // Assert
        Assert.Equal(original.LeafCount, deserialized.LeafCount);
        Assert.Equal(original.PlotId, deserialized.PlotId);
        Assert.Equal(original.PlotHeaderHash, deserialized.PlotHeaderHash);
        Assert.Equal(original.Version, deserialized.Version);
    }

    [Fact]
    public void Serialize_WithNullWriter_ThrowsArgumentNullException()
    {
        // Arrange
        var metadata = BlockPlotMetadata.Create(
            1000,
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32),
            1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => metadata.Serialize(null!));
    }

    [Fact]
    public void Deserialize_WithNullReader_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BlockPlotMetadata.Deserialize(null!));
    }

    [Fact]
    public void SerializedSize_ReturnsCorrectValue()
    {
        // Expected: long (8) + 32 + 32 + byte (1) = 73
        Assert.Equal(73, BlockPlotMetadata.SerializedSize);
    }
}
