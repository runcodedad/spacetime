using System.Security.Cryptography;

namespace Spacetime.Core.Tests;

public class BlockHeaderTests
{
    private static BlockHeader CreateValidHeader(bool signed = true)
    {
        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 100,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32),
            txRoot: RandomNumberGenerator.GetBytes(32),
            minerId: RandomNumberGenerator.GetBytes(33),
            signature: signed ? RandomNumberGenerator.GetBytes(64) : Array.Empty<byte>());

        return header;
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesHeader()
    {
        // Arrange
        var parentHash = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var plotRoot = RandomNumberGenerator.GetBytes(32);
        var proofScore = RandomNumberGenerator.GetBytes(32);
        var txRoot = RandomNumberGenerator.GetBytes(32);
        var minerId = RandomNumberGenerator.GetBytes(33);
        var signature = RandomNumberGenerator.GetBytes(64);

        // Act
        var header = new BlockHeader(
            BlockHeader.CurrentVersion,
            parentHash,
            100,
            1234567890,
            1000,
            10,
            challenge,
            plotRoot,
            proofScore,
            txRoot,
            minerId,
            signature);

        // Assert
        Assert.Equal(BlockHeader.CurrentVersion, header.Version);
        Assert.Equal(parentHash, header.ParentHash.ToArray());
        Assert.Equal(100, header.Height);
        Assert.Equal(1234567890, header.Timestamp);
        Assert.Equal(1000, header.Difficulty);
        Assert.Equal(10, header.Epoch);
        Assert.Equal(challenge, header.Challenge.ToArray());
        Assert.Equal(plotRoot, header.PlotRoot.ToArray());
        Assert.Equal(proofScore, header.ProofScore.ToArray());
        Assert.Equal(txRoot, header.TxRoot.ToArray());
        Assert.Equal(minerId, header.MinerId.ToArray());
        Assert.Equal(signature, header.Signature.ToArray());
    }

    [Fact]
    public void Constructor_WithEmptySignature_CreatesUnsignedHeader()
    {
        // Arrange & Act
        var header = CreateValidHeader(signed: false);

        // Assert
        Assert.False(header.IsSigned());
        Assert.Empty(header.Signature.ToArray());
    }

    [Fact]
    public void Constructor_WithInvalidParentHashSize_ThrowsArgumentException()
    {
        // Arrange
        var parentHash = RandomNumberGenerator.GetBytes(16); // Wrong size

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new BlockHeader(
                1,
                parentHash,
                0,
                0,
                0,
                0,
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(33),
                Array.Empty<byte>()));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void Constructor_WithNegativeHeight_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new BlockHeader(
                1,
                RandomNumberGenerator.GetBytes(32),
                -1, // Invalid
                0,
                0,
                0,
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(33),
                Array.Empty<byte>()));
        Assert.Contains("non-negative", exception.Message);
    }

    [Fact]
    public void Constructor_WithNegativeTimestamp_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new BlockHeader(
                1,
                RandomNumberGenerator.GetBytes(32),
                0,
                -1, // Invalid
                0,
                0,
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(33),
                Array.Empty<byte>()));
        Assert.Contains("non-negative", exception.Message);
    }

    [Fact]
    public void Constructor_WithNegativeDifficulty_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new BlockHeader(
                1,
                RandomNumberGenerator.GetBytes(32),
                0,
                0,
                -1, // Invalid
                0,
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(33),
                Array.Empty<byte>()));
        Assert.Contains("non-negative", exception.Message);
    }

    [Fact]
    public void Constructor_WithNegativeEpoch_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new BlockHeader(
                1,
                RandomNumberGenerator.GetBytes(32),
                0,
                0,
                0,
                -1, // Invalid
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(33),
                Array.Empty<byte>()));
        Assert.Contains("non-negative", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidChallengeSize_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new BlockHeader(
                1,
                RandomNumberGenerator.GetBytes(32),
                0,
                0,
                0,
                0,
                RandomNumberGenerator.GetBytes(16), // Wrong size
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(33),
                Array.Empty<byte>()));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidMinerIdSize_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new BlockHeader(
                1,
                RandomNumberGenerator.GetBytes(32),
                0,
                0,
                0,
                0,
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32), // Wrong size (should be 33)
                Array.Empty<byte>()));
        Assert.Contains("33 bytes", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidSignatureSize_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new BlockHeader(
                1,
                RandomNumberGenerator.GetBytes(32),
                0,
                0,
                0,
                0,
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(32),
                RandomNumberGenerator.GetBytes(33),
                RandomNumberGenerator.GetBytes(32))); // Wrong size (should be 0 or 64)
        Assert.Contains("64 bytes", exception.Message);
    }

    [Fact]
    public void SetSignature_WithValidSignature_SetsSignature()
    {
        // Arrange
        var header = CreateValidHeader(signed: false);
        var signature = RandomNumberGenerator.GetBytes(64);

        // Act
        header.SetSignature(signature);

        // Assert
        Assert.True(header.IsSigned());
        Assert.Equal(signature, header.Signature.ToArray());
    }

    [Fact]
    public void SetSignature_WithInvalidSize_ThrowsArgumentException()
    {
        // Arrange
        var header = CreateValidHeader(signed: false);
        var signature = RandomNumberGenerator.GetBytes(32); // Wrong size

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => header.SetSignature(signature));
        Assert.Contains("64 bytes", exception.Message);
    }

    [Fact]
    public void ComputeHash_ReturnsConsistentHash()
    {
        // Arrange
        var header = CreateValidHeader();

        // Act
        var hash1 = header.ComputeHash();
        var hash2 = header.ComputeHash();

        // Assert
        Assert.Equal(32, hash1.Length);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentHeaders_ReturnDifferentHashes()
    {
        // Arrange
        var header1 = CreateValidHeader();
        var header2 = CreateValidHeader(); // Different random values

        // Act
        var hash1 = header1.ComputeHash();
        var hash2 = header2.ComputeHash();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var original = CreateValidHeader();

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Act
        original.Serialize(writer);
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var deserialized = BlockHeader.Deserialize(reader);

        // Assert
        Assert.Equal(original.Version, deserialized.Version);
        Assert.Equal(original.ParentHash.ToArray(), deserialized.ParentHash.ToArray());
        Assert.Equal(original.Height, deserialized.Height);
        Assert.Equal(original.Timestamp, deserialized.Timestamp);
        Assert.Equal(original.Difficulty, deserialized.Difficulty);
        Assert.Equal(original.Epoch, deserialized.Epoch);
        Assert.Equal(original.Challenge.ToArray(), deserialized.Challenge.ToArray());
        Assert.Equal(original.PlotRoot.ToArray(), deserialized.PlotRoot.ToArray());
        Assert.Equal(original.ProofScore.ToArray(), deserialized.ProofScore.ToArray());
        Assert.Equal(original.TxRoot.ToArray(), deserialized.TxRoot.ToArray());
        Assert.Equal(original.MinerId.ToArray(), deserialized.MinerId.ToArray());
        Assert.Equal(original.Signature.ToArray(), deserialized.Signature.ToArray());
    }

    [Fact]
    public void Serialize_WithoutSignature_ThrowsInvalidOperationException()
    {
        // Arrange
        var header = CreateValidHeader(signed: false);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => header.Serialize(writer));
    }

    [Fact]
    public void Serialize_WithNullWriter_ThrowsArgumentNullException()
    {
        // Arrange
        var header = CreateValidHeader();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => header.Serialize(null!));
    }

    [Fact]
    public void Deserialize_WithNullReader_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BlockHeader.Deserialize(null!));
    }

    [Fact]
    public void SerializedSize_ReturnsCorrectValue()
    {
        // Expected: 1 + 32 + 8 + 8 + 8 + 8 + 32 + 32 + 32 + 32 + 33 + 64 = 290
        Assert.Equal(290, BlockHeader.SerializedSize);
    }

    [Fact]
    public void SerializeWithoutSignature_ReturnsCorrectSize()
    {
        // Arrange
        var header = CreateValidHeader();

        // Act
        var bytes = header.SerializeWithoutSignature();

        // Assert
        // Expected: 290 - 64 (signature) = 226
        Assert.Equal(226, bytes.Length);
    }

    [Fact]
    public void ComputeHash_ExcludesSignature()
    {
        // Arrange
        var header = CreateValidHeader(signed: false);
        var hash1 = header.ComputeHash();

        // Add signature
        header.SetSignature(RandomNumberGenerator.GetBytes(64));
        var hash2 = header.ComputeHash();

        // Assert - hash should be the same regardless of signature
        Assert.Equal(hash1, hash2);
    }
}
