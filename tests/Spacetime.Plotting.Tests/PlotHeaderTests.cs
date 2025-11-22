using System.Security.Cryptography;

namespace Spacetime.Plotting.Tests;

public class PlotHeaderTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesHeader()
    {
        // Arrange
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var merkleRoot = RandomNumberGenerator.GetBytes(32);

        // Act
        var header = new PlotHeader(plotSeed, 1000, 32, 10, merkleRoot);

        // Assert
        Assert.Equal(plotSeed, header.PlotSeed);
        Assert.Equal(1000, header.LeafCount);
        Assert.Equal(32, header.LeafSize);
        Assert.Equal(10, header.TreeHeight);
        Assert.Equal(merkleRoot, header.MerkleRoot);
    }

    [Fact]
    public void Constructor_InvalidPlotSeedSize_ThrowsArgumentException()
    {
        // Arrange
        var plotSeed = new byte[16]; // Wrong size
        var merkleRoot = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new PlotHeader(plotSeed, 1000, 32, 10, merkleRoot));
    }

    [Fact]
    public void Constructor_InvalidMerkleRootSize_ThrowsArgumentException()
    {
        // Arrange
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var merkleRoot = new byte[16]; // Wrong size

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new PlotHeader(plotSeed, 1000, 32, 10, merkleRoot));
    }

    [Fact]
    public void Constructor_NegativeLeafCount_ThrowsArgumentException()
    {
        // Arrange
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var merkleRoot = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new PlotHeader(plotSeed, -1, 32, 10, merkleRoot));
    }

    [Fact]
    public void ComputeChecksum_SetsChecksumProperty()
    {
        // Arrange
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var header = new PlotHeader(plotSeed, 1000, 32, 10, merkleRoot);

        // Act
        header.ComputeChecksum();

        // Assert
        Assert.NotNull(header.Checksum);
        Assert.Equal(PlotHeader.ChecksumSize, header.Checksum.Length);
    }

    [Fact]
    public void VerifyChecksum_WithCorrectChecksum_ReturnsTrue()
    {
        // Arrange
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var header = new PlotHeader(plotSeed, 1000, 32, 10, merkleRoot);
        header.ComputeChecksum();

        // Act
        var isValid = header.VerifyChecksum();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Serialize_WithComputedChecksum_ProducesCorrectSize()
    {
        // Arrange
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var header = new PlotHeader(plotSeed, 1000, 32, 10, merkleRoot);
        header.ComputeChecksum();

        // Act
        var serialized = header.Serialize();

        // Assert
        Assert.Equal(PlotHeader.TotalHeaderSize, serialized.Length);
    }

    [Fact]
    public void Serialize_WithoutChecksum_ThrowsInvalidOperationException()
    {
        // Arrange
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var header = new PlotHeader(plotSeed, 1000, 32, 10, merkleRoot);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => header.Serialize());
    }

    [Fact]
    public void Deserialize_WithValidData_ReconstructsHeader()
    {
        // Arrange
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var original = new PlotHeader(plotSeed, 1000, 32, 10, merkleRoot);
        original.ComputeChecksum();
        var serialized = original.Serialize();

        // Act
        var deserialized = PlotHeader.Deserialize(serialized);

        // Assert
        Assert.Equal(original.PlotSeed, deserialized.PlotSeed);
        Assert.Equal(original.LeafCount, deserialized.LeafCount);
        Assert.Equal(original.LeafSize, deserialized.LeafSize);
        Assert.Equal(original.TreeHeight, deserialized.TreeHeight);
        Assert.Equal(original.MerkleRoot, deserialized.MerkleRoot);
        Assert.Equal(original.Checksum, deserialized.Checksum);
    }

    [Fact]
    public void Deserialize_WithInvalidMagicBytes_ThrowsInvalidOperationException()
    {
        // Arrange
        var data = new byte[PlotHeader.TotalHeaderSize];
        data[0] = 0xFF; // Invalid magic byte

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => PlotHeader.Deserialize(data));
    }

    [Fact]
    public void Deserialize_WithInvalidChecksum_ThrowsInvalidOperationException()
    {
        // Arrange
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var header = new PlotHeader(plotSeed, 1000, 32, 10, merkleRoot);
        header.ComputeChecksum();
        var serialized = header.Serialize();

        // Corrupt the checksum
        serialized[PlotHeader.HeaderSize] ^= 0xFF;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => PlotHeader.Deserialize(serialized));
    }

    [Fact]
    public void Deserialize_WithUnsupportedVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var header = new PlotHeader(plotSeed, 1000, 32, 10, merkleRoot);
        header.ComputeChecksum();
        var serialized = header.Serialize();

        // Change version to unsupported value
        serialized[4] = 99;

        // Recompute checksum for the modified header
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var headerBytes = serialized.AsSpan(0, PlotHeader.HeaderSize).ToArray();
        var newChecksum = sha256.ComputeHash(headerBytes);
        Array.Copy(newChecksum, 0, serialized, PlotHeader.HeaderSize, PlotHeader.ChecksumSize);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => PlotHeader.Deserialize(serialized));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var original = new PlotHeader(plotSeed, 123456, 32, 17, merkleRoot);
        original.ComputeChecksum();

        // Act
        var serialized = original.Serialize();
        var deserialized = PlotHeader.Deserialize(serialized);

        // Assert - All fields match
        Assert.Equal(original.PlotSeed, deserialized.PlotSeed);
        Assert.Equal(original.LeafCount, deserialized.LeafCount);
        Assert.Equal(original.LeafSize, deserialized.LeafSize);
        Assert.Equal(original.TreeHeight, deserialized.TreeHeight);
        Assert.Equal(original.MerkleRoot, deserialized.MerkleRoot);
        Assert.Equal(original.Checksum, deserialized.Checksum);
    }
}
