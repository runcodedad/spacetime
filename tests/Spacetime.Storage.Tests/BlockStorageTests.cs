using Spacetime.Core;
using System.Security.Cryptography;

namespace Spacetime.Storage.Tests;

public class BlockStorageTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly RocksDbChainStorage _storage;

    public BlockStorageTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"spacetime_test_{Guid.NewGuid():N}");
        _storage = RocksDbChainStorage.Open(_testDbPath);
    }

    public void Dispose()
    {
        _storage.Dispose();
        if (Directory.Exists(_testDbPath))
        {
            Directory.Delete(_testDbPath, recursive: true);
        }
    }

    private static BlockHeader CreateTestHeader(long height = 100)
    {
        return new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: height,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32),
            txRoot: RandomNumberGenerator.GetBytes(32),
            minerId: RandomNumberGenerator.GetBytes(33),
            signature: RandomNumberGenerator.GetBytes(64));
    }

    private static BlockBody CreateTestBody()
    {
        var metadata = BlockPlotMetadata.Create(
            1000,
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32),
            1);

        var proof = new BlockProof(
            RandomNumberGenerator.GetBytes(32),
            42,
            new[] { RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32) },
            new[] { true, false },
            metadata);

        var tx = new Transaction(
            RandomNumberGenerator.GetBytes(33),
            RandomNumberGenerator.GetBytes(33),
            1000,
            1,
            10,
            RandomNumberGenerator.GetBytes(64));

        return new BlockBody(new[] { tx }, proof);
    }

    private static Block CreateTestBlock(long height = 100)
    {
        return new Block(CreateTestHeader(height), CreateTestBody());
    }

    [Fact]
    public void StoreHeaderAsync_WithValidHeader_StoresSuccessfully()
    {
        // Arrange
        var header = CreateTestHeader();
        var hash = header.ComputeHash();

        // Act
        _storage.Blocks.StoreHeader(header);

        // Assert
        var retrieved = _storage.Blocks.GetHeaderByHash(hash);
        Assert.NotNull(retrieved);
        Assert.Equal(header.Height, retrieved.Height);
        Assert.Equal(header.Version, retrieved.Version);
    }

    [Fact]
    public void StoreHeaderAsync_WithNullHeader_ThrowsArgumentNullException()
    {
        // Arrange
        BlockHeader header = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _storage.Blocks.StoreHeader(header));
    }

    [Fact]
    public void GetHeaderByHashAsync_WithNonExistentHash_ReturnsNull()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(32);

        // Act
        var result = _storage.Blocks.GetHeaderByHash(hash);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetHeaderByHashAsync_WithInvalidHashSize_ThrowsArgumentException()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(16);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _storage.Blocks.GetHeaderByHash(hash));
    }

    [Fact]
    public void GetHeaderByHeightAsync_WithValidHeight_ReturnsHeader()
    {
        // Arrange
        var header = CreateTestHeader(123);
        _storage.Blocks.StoreHeader(header);

        // Act
        var retrieved = _storage.Blocks.GetHeaderByHeight(123);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(123, retrieved.Height);
    }

    [Fact]
    public void GetHeaderByHeightAsync_WithNonExistentHeight_ReturnsNull()
    {
        // Act
        var result = _storage.Blocks.GetHeaderByHeight(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetHeaderByHeightAsync_WithNegativeHeight_ThrowsArgumentException()
    {
        // Arrange
        var height = -1L;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _storage.Blocks.GetHeaderByHeight(height));
    }

    [Fact]
    public void StoreBodyAsync_WithValidBody_StoresSuccessfully()
    {
        // Arrange
        var body = CreateTestBody();
        var hash = RandomNumberGenerator.GetBytes(32);

        // Act
        _storage.Blocks.StoreBody(hash, body);

        // Assert
        var retrieved = _storage.Blocks.GetBodyByHash(hash);
        Assert.NotNull(retrieved);
        Assert.Equal(body.Transactions.Count, retrieved.Transactions.Count);
    }

    [Fact]
    public void StoreBodyAsync_WithNullBody_ThrowsArgumentNullException()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(32);
        BlockBody body = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _storage.Blocks.StoreBody(hash, body));
    }

    [Fact]
    public void StoreBodyAsync_WithInvalidHashSize_ThrowsArgumentException()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(16);
        var body = CreateTestBody();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _storage.Blocks.StoreBody(hash, body));
    }

    [Fact]
    public void StoreBlockAsync_WithValidBlock_StoresSuccessfully()
    {
        // Arrange
        var block = CreateTestBlock();
        var hash = block.Header.ComputeHash();

        // Act
        _storage.Blocks.StoreBlock(block);

        // Assert
        var retrieved = _storage.Blocks.GetBlockByHash(hash);
        Assert.NotNull(retrieved);
        Assert.Equal(block.Header.Height, retrieved.Header.Height);
        Assert.Equal(block.Body.Transactions.Count, retrieved.Body.Transactions.Count);
    }

    [Fact]
    public void StoreBlockAsync_WithNullBlock_ThrowsArgumentNullException()
    {
        // Arrange
        Block block = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _storage.Blocks.StoreBlock(block));
    }

    [Fact]
    public void GetBlockByHashAsync_WithNonExistentBlock_ReturnsNull()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(32);

        // Act
        var result = _storage.Blocks.GetBlockByHash(hash);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetBlockByHeightAsync_WithValidHeight_ReturnsBlock()
    {
        // Arrange
        var block = CreateTestBlock(456);
        _storage.Blocks.StoreBlock(block);

        // Act
        var retrieved = _storage.Blocks.GetBlockByHeight(456);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(456, retrieved.Header.Height);
    }

    [Fact]
    public void GetBlockByHeightAsync_WithNonExistentHeight_ReturnsNull()
    {
        // Act
        var result = _storage.Blocks.GetBlockByHeight(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExistsAsync_WithExistingBlock_ReturnsTrue()
    {
        // Arrange
        var header = CreateTestHeader();
        var hash = header.ComputeHash();
        _storage.Blocks.StoreHeader(header);

        // Act
        var exists = _storage.Blocks.Exists(hash);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void ExistsAsync_WithNonExistentBlock_ReturnsFalse()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(32);

        // Act
        var exists = _storage.Blocks.Exists(hash);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void ExistsAsync_WithInvalidHashSize_ThrowsArgumentException()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(16);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _storage.Blocks.Exists(hash));
    }
}
