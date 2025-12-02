using System.Security.Cryptography;

namespace Spacetime.Core.Tests;

public class BlockBodyTests
{
    private static BlockProof CreateValidProof()
    {
        var plotMetadata = BlockPlotMetadata.Create(
            1000,
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32),
            1);

        return new BlockProof(
            RandomNumberGenerator.GetBytes(32),
            42,
            new[] { RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32) },
            new[] { true, false },
            plotMetadata);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesBody()
    {
        // Arrange
        var transactions = new[]
        {
            RandomNumberGenerator.GetBytes(100),
            RandomNumberGenerator.GetBytes(200)
        };
        var proof = CreateValidProof();

        // Act
        var body = new BlockBody(transactions, proof);

        // Assert
        Assert.Equal(2, body.Transactions.Count);
        Assert.Equal(proof, body.Proof);
    }

    [Fact]
    public void Constructor_WithEmptyTransactions_CreatesBody()
    {
        // Arrange
        var transactions = Array.Empty<byte[]>();
        var proof = CreateValidProof();

        // Act
        var body = new BlockBody(transactions, proof);

        // Assert
        Assert.Empty(body.Transactions);
        Assert.Equal(proof, body.Proof);
    }

    [Fact]
    public void Constructor_WithNullTransactions_ThrowsArgumentNullException()
    {
        // Arrange
        var proof = CreateValidProof();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BlockBody(null!, proof));
    }

    [Fact]
    public void Constructor_WithNullProof_ThrowsArgumentNullException()
    {
        // Arrange
        var transactions = new[] { RandomNumberGenerator.GetBytes(100) };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BlockBody(transactions, null!));
    }

    [Fact]
    public void Constructor_WithNullTransactionEntry_ThrowsArgumentException()
    {
        // Arrange
        var transactions = new byte[][] { RandomNumberGenerator.GetBytes(100), null! };
        var proof = CreateValidProof();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new BlockBody(transactions, proof));
        Assert.Contains("null", exception.Message);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var transactions = new[]
        {
            RandomNumberGenerator.GetBytes(50),
            RandomNumberGenerator.GetBytes(100),
            RandomNumberGenerator.GetBytes(150)
        };
        var proof = CreateValidProof();
        var original = new BlockBody(transactions, proof);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Act
        original.Serialize(writer);
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var deserialized = BlockBody.Deserialize(reader);

        // Assert
        Assert.Equal(original.Transactions.Count, deserialized.Transactions.Count);
        for (int i = 0; i < original.Transactions.Count; i++)
        {
            Assert.Equal(original.Transactions[i], deserialized.Transactions[i]);
        }
        Assert.Equal(original.Proof.LeafIndex, deserialized.Proof.LeafIndex);
    }

    [Fact]
    public void SerializeDeserialize_WithEmptyTransactions_PreservesData()
    {
        // Arrange
        var original = new BlockBody(Array.Empty<byte[]>(), CreateValidProof());

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Act
        original.Serialize(writer);
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var deserialized = BlockBody.Deserialize(reader);

        // Assert
        Assert.Empty(deserialized.Transactions);
    }

    [Fact]
    public void Serialize_WithNullWriter_ThrowsArgumentNullException()
    {
        // Arrange
        var body = new BlockBody(Array.Empty<byte[]>(), CreateValidProof());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => body.Serialize(null!));
    }

    [Fact]
    public void Deserialize_WithNullReader_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BlockBody.Deserialize(null!));
    }

    [Fact]
    public void Constructor_ClonesTransactions()
    {
        // Arrange
        var tx = RandomNumberGenerator.GetBytes(100);
        var originalTx = (byte[])tx.Clone();
        var transactions = new[] { tx };
        var proof = CreateValidProof();

        // Act
        var body = new BlockBody(transactions, proof);
        tx[0] ^= 0xFF; // Modify original

        // Assert - body should have original value
        Assert.Equal(originalTx, body.Transactions[0]);
    }
}
