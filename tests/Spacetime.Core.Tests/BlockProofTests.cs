using System.Security.Cryptography;

namespace Spacetime.Core.Tests;

public class BlockProofTests
{
    private static BlockPlotMetadata CreateValidMetadata()
    {
        return BlockPlotMetadata.Create(
            1000,
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32),
            1);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesProof()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var leafIndex = 42L;
        var merkleProofPath = new[] { RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32) };
        var orientationBits = new[] { true, false };
        var plotMetadata = CreateValidMetadata();

        // Act
        var proof = new BlockProof(leafValue, leafIndex, merkleProofPath, orientationBits, plotMetadata);

        // Assert
        Assert.Equal(leafValue, proof.LeafValue.ToArray());
        Assert.Equal(leafIndex, proof.LeafIndex);
        Assert.Equal(merkleProofPath.Length, proof.MerkleProofPath.Count);
        Assert.Equal(orientationBits, proof.OrientationBits);
        Assert.Equal(plotMetadata, proof.PlotMetadata);
    }

    [Fact]
    public void Constructor_WithEmptyMerkleProof_CreatesProof()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var merkleProofPath = Array.Empty<byte[]>();
        var orientationBits = Array.Empty<bool>();
        var plotMetadata = CreateValidMetadata();

        // Act
        var proof = new BlockProof(leafValue, 0, merkleProofPath, orientationBits, plotMetadata);

        // Assert
        Assert.Empty(proof.MerkleProofPath);
        Assert.Empty(proof.OrientationBits);
    }

    [Fact]
    public void Constructor_WithInvalidLeafValueSize_ThrowsArgumentException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(16); // Wrong size
        var merkleProofPath = new[] { RandomNumberGenerator.GetBytes(32) };
        var orientationBits = new[] { true };
        var plotMetadata = CreateValidMetadata();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new BlockProof(leafValue, 0, merkleProofPath, orientationBits, plotMetadata));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void Constructor_WithNegativeLeafIndex_ThrowsArgumentException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var merkleProofPath = new[] { RandomNumberGenerator.GetBytes(32) };
        var orientationBits = new[] { true };
        var plotMetadata = CreateValidMetadata();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new BlockProof(leafValue, -1, merkleProofPath, orientationBits, plotMetadata));
        Assert.Contains("non-negative", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullMerkleProofPath_ThrowsArgumentNullException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var orientationBits = new[] { true };
        var plotMetadata = CreateValidMetadata();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BlockProof(leafValue, 0, null!, orientationBits, plotMetadata));
    }

    [Fact]
    public void Constructor_WithNullOrientationBits_ThrowsArgumentNullException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var merkleProofPath = new[] { RandomNumberGenerator.GetBytes(32) };
        var plotMetadata = CreateValidMetadata();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BlockProof(leafValue, 0, merkleProofPath, null!, plotMetadata));
    }

    [Fact]
    public void Constructor_WithNullPlotMetadata_ThrowsArgumentNullException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var merkleProofPath = new[] { RandomNumberGenerator.GetBytes(32) };
        var orientationBits = new[] { true };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BlockProof(leafValue, 0, merkleProofPath, orientationBits, null!));
    }

    [Fact]
    public void Constructor_WithMismatchedProofPathAndOrientationBits_ThrowsArgumentException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var merkleProofPath = new[] { RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32) };
        var orientationBits = new[] { true }; // Mismatched count
        var plotMetadata = CreateValidMetadata();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new BlockProof(leafValue, 0, merkleProofPath, orientationBits, plotMetadata));
        Assert.Contains("same count", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidMerkleProofHashSize_ThrowsArgumentException()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var merkleProofPath = new[] { RandomNumberGenerator.GetBytes(16) }; // Wrong size
        var orientationBits = new[] { true };
        var plotMetadata = CreateValidMetadata();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new BlockProof(leafValue, 0, merkleProofPath, orientationBits, plotMetadata));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var leafIndex = 12345L;
        var merkleProofPath = new[]
        {
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32)
        };
        var orientationBits = new[] { true, false, true };
        var plotMetadata = CreateValidMetadata();

        var original = new BlockProof(leafValue, leafIndex, merkleProofPath, orientationBits, plotMetadata);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Act
        original.Serialize(writer);
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var deserialized = BlockProof.Deserialize(reader);

        // Assert
        Assert.Equal(original.LeafValue.ToArray(), deserialized.LeafValue.ToArray());
        Assert.Equal(original.LeafIndex, deserialized.LeafIndex);
        Assert.Equal(original.MerkleProofPath.Count, deserialized.MerkleProofPath.Count);
        for (int i = 0; i < original.MerkleProofPath.Count; i++)
        {
            Assert.Equal(original.MerkleProofPath[i], deserialized.MerkleProofPath[i]);
        }
        Assert.Equal(original.OrientationBits, deserialized.OrientationBits);
        Assert.Equal(original.PlotMetadata.LeafCount, deserialized.PlotMetadata.LeafCount);
    }

    [Fact]
    public void Serialize_WithNullWriter_ThrowsArgumentNullException()
    {
        // Arrange
        var proof = new BlockProof(
            RandomNumberGenerator.GetBytes(32),
            0,
            Array.Empty<byte[]>(),
            Array.Empty<bool>(),
            CreateValidMetadata());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => proof.Serialize(null!));
    }

    [Fact]
    public void Deserialize_WithNullReader_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BlockProof.Deserialize(null!));
    }

    [Fact]
    public void Constructor_DoesNotModifyOriginalArrays()
    {
        // Arrange
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var originalLeafValue = (byte[])leafValue.Clone();
        var merkleProofPath = new[] { RandomNumberGenerator.GetBytes(32) };
        var originalHash = (byte[])merkleProofPath[0].Clone();
        var orientationBits = new[] { true };
        var plotMetadata = CreateValidMetadata();

        // Act
        var proof = new BlockProof(leafValue, 0, merkleProofPath, orientationBits, plotMetadata);

        // Modify original arrays
        leafValue[0] ^= 0xFF;
        merkleProofPath[0][0] ^= 0xFF;

        // Assert - proof should have original values
        Assert.Equal(originalLeafValue, proof.LeafValue.ToArray());
        Assert.Equal(originalHash, proof.MerkleProofPath[0]);
    }
}
