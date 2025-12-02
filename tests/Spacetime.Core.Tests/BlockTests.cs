using System.Security.Cryptography;

namespace Spacetime.Core.Tests;

public class BlockTests
{
    private static BlockPlotMetadata CreateValidMetadata()
    {
        return BlockPlotMetadata.Create(
            1000,
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32),
            1);
    }

    private static BlockProof CreateValidProof()
    {
        return new BlockProof(
            RandomNumberGenerator.GetBytes(32),
            42,
            new[] { RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32) },
            new[] { true, false },
            CreateValidMetadata());
    }

    private static BlockHeader CreateValidHeader(bool signed = true)
    {
        return new BlockHeader(
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
    }

    private static BlockBody CreateValidBody()
    {
        var transactions = new[]
        {
            RandomNumberGenerator.GetBytes(100),
            RandomNumberGenerator.GetBytes(200)
        };
        return new BlockBody(transactions, CreateValidProof());
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesBlock()
    {
        // Arrange
        var header = CreateValidHeader();
        var body = CreateValidBody();

        // Act
        var block = new Block(header, body);

        // Assert
        Assert.Equal(header, block.Header);
        Assert.Equal(body, block.Body);
    }

    [Fact]
    public void Constructor_WithNullHeader_ThrowsArgumentNullException()
    {
        // Arrange
        var body = CreateValidBody();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Block(null!, body));
    }

    [Fact]
    public void Constructor_WithNullBody_ThrowsArgumentNullException()
    {
        // Arrange
        var header = CreateValidHeader();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Block(header, null!));
    }

    [Fact]
    public void ComputeHash_ReturnsConsistentHash()
    {
        // Arrange
        var block = new Block(CreateValidHeader(), CreateValidBody());

        // Act
        var hash1 = block.ComputeHash();
        var hash2 = block.ComputeHash();

        // Assert
        Assert.Equal(32, hash1.Length);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DelegatesToHeader()
    {
        // Arrange
        var header = CreateValidHeader();
        var body = CreateValidBody();
        var block = new Block(header, body);

        // Act
        var blockHash = block.ComputeHash();
        var headerHash = header.ComputeHash();

        // Assert
        Assert.Equal(headerHash, blockHash);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new Block(CreateValidHeader(), CreateValidBody());

        // Act
        var serialized = original.Serialize();
        var deserialized = Block.Deserialize(serialized);

        // Assert
        Assert.Equal(original.Header.Version, deserialized.Header.Version);
        Assert.Equal(original.Header.Height, deserialized.Header.Height);
        Assert.Equal(original.Header.Timestamp, deserialized.Header.Timestamp);
        Assert.Equal(original.Header.Difficulty, deserialized.Header.Difficulty);
        Assert.Equal(original.Header.Epoch, deserialized.Header.Epoch);
        Assert.Equal(original.Header.ParentHash, deserialized.Header.ParentHash);
        Assert.Equal(original.Header.Challenge, deserialized.Header.Challenge);
        Assert.Equal(original.Header.PlotRoot, deserialized.Header.PlotRoot);
        Assert.Equal(original.Header.ProofScore, deserialized.Header.ProofScore);
        Assert.Equal(original.Header.TxRoot, deserialized.Header.TxRoot);
        Assert.Equal(original.Header.MinerId, deserialized.Header.MinerId);
        Assert.Equal(original.Header.Signature, deserialized.Header.Signature);

        Assert.Equal(original.Body.Transactions.Count, deserialized.Body.Transactions.Count);
        for (int i = 0; i < original.Body.Transactions.Count; i++)
        {
            Assert.Equal(original.Body.Transactions[i], deserialized.Body.Transactions[i]);
        }
    }

    [Fact]
    public void SerializeDeserialize_WithBinaryReader_PreservesData()
    {
        // Arrange
        var original = new Block(CreateValidHeader(), CreateValidBody());
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Act
        original.Serialize(writer);
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var deserialized = Block.Deserialize(reader);

        // Assert
        Assert.Equal(original.Header.Height, deserialized.Header.Height);
        Assert.Equal(original.Body.Transactions.Count, deserialized.Body.Transactions.Count);
    }

    [Fact]
    public void Serialize_WithUnsignedHeader_ThrowsInvalidOperationException()
    {
        // Arrange
        var block = new Block(CreateValidHeader(signed: false), CreateValidBody());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => block.Serialize());
    }

    [Fact]
    public void Serialize_WithNullWriter_ThrowsArgumentNullException()
    {
        // Arrange
        var block = new Block(CreateValidHeader(), CreateValidBody());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => block.Serialize(null!));
    }

    [Fact]
    public void Deserialize_ByteArray_WithNullData_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Block.Deserialize((byte[])null!));
    }

    [Fact]
    public void Deserialize_BinaryReader_WithNullReader_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Block.Deserialize((BinaryReader)null!));
    }

    [Fact]
    public void Serialize_ProducesNonEmptyBytes()
    {
        // Arrange
        var block = new Block(CreateValidHeader(), CreateValidBody());

        // Act
        var serialized = block.Serialize();

        // Assert
        Assert.NotEmpty(serialized);
        Assert.True(serialized.Length > BlockHeader.SerializedSize);
    }

    [Fact]
    public void ComputeHash_SameHeader_DifferentBody_ReturnsSameHash()
    {
        // Arrange
        var header = CreateValidHeader();
        var body1 = CreateValidBody();
        var body2 = new BlockBody(Array.Empty<byte[]>(), CreateValidProof());

        var block1 = new Block(header, body1);
        var block2 = new Block(header, body2);

        // Act
        var hash1 = block1.ComputeHash();
        var hash2 = block2.ComputeHash();

        // Assert - block hash only depends on header
        Assert.Equal(hash1, hash2);
    }
}
