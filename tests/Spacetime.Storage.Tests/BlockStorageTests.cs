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
        _storage.DisposeAsync().AsTask().Wait();
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
    public async Task StoreHeaderAsync_WithValidHeader_StoresSuccessfully()
    {
        // Arrange
        var header = CreateTestHeader();
        var hash = header.ComputeHash();

        // Act
        await _storage.Blocks.StoreHeaderAsync(header);

        // Assert
        var retrieved = await _storage.Blocks.GetHeaderByHashAsync(hash);
        Assert.NotNull(retrieved);
        Assert.Equal(header.Height, retrieved.Height);
        Assert.Equal(header.Version, retrieved.Version);
    }

    [Fact]
    public async Task StoreHeaderAsync_WithNullHeader_ThrowsArgumentNullException()
    {
        // Arrange
        BlockHeader header = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.Blocks.StoreHeaderAsync(header));
    }

    [Fact]
    public async Task GetHeaderByHashAsync_WithNonExistentHash_ReturnsNull()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(32);

        // Act
        var result = await _storage.Blocks.GetHeaderByHashAsync(hash);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetHeaderByHashAsync_WithInvalidHashSize_ThrowsArgumentException()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(16);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.Blocks.GetHeaderByHashAsync(hash));
    }

    [Fact]
    public async Task GetHeaderByHeightAsync_WithValidHeight_ReturnsHeader()
    {
        // Arrange
        var header = CreateTestHeader(123);
        await _storage.Blocks.StoreHeaderAsync(header);

        // Act
        var retrieved = await _storage.Blocks.GetHeaderByHeightAsync(123);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(123, retrieved.Height);
    }

    [Fact]
    public async Task GetHeaderByHeightAsync_WithNonExistentHeight_ReturnsNull()
    {
        // Act
        var result = await _storage.Blocks.GetHeaderByHeightAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetHeaderByHeightAsync_WithNegativeHeight_ThrowsArgumentException()
    {
        // Arrange
        var height = -1L;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.Blocks.GetHeaderByHeightAsync(height));
    }

    [Fact]
    public async Task StoreBodyAsync_WithValidBody_StoresSuccessfully()
    {
        // Arrange
        var body = CreateTestBody();
        var hash = RandomNumberGenerator.GetBytes(32);

        // Act
        await _storage.Blocks.StoreBodyAsync(hash, body);

        // Assert
        var retrieved = await _storage.Blocks.GetBodyByHashAsync(hash);
        Assert.NotNull(retrieved);
        Assert.Equal(body.Transactions.Count, retrieved.Transactions.Count);
    }

    [Fact]
    public async Task StoreBodyAsync_WithNullBody_ThrowsArgumentNullException()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(32);
        BlockBody body = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.Blocks.StoreBodyAsync(hash, body));
    }

    [Fact]
    public async Task StoreBodyAsync_WithInvalidHashSize_ThrowsArgumentException()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(16);
        var body = CreateTestBody();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.Blocks.StoreBodyAsync(hash, body));
    }

    [Fact]
    public async Task StoreBlockAsync_WithValidBlock_StoresSuccessfully()
    {
        // Arrange
        var block = CreateTestBlock();
        var hash = block.Header.ComputeHash();

        // Act
        await _storage.Blocks.StoreBlockAsync(block);

        // Assert
        var retrieved = await _storage.Blocks.GetBlockByHashAsync(hash);
        Assert.NotNull(retrieved);
        Assert.Equal(block.Header.Height, retrieved.Header.Height);
        Assert.Equal(block.Body.Transactions.Count, retrieved.Body.Transactions.Count);
    }

    [Fact]
    public async Task StoreBlockAsync_WithNullBlock_ThrowsArgumentNullException()
    {
        // Arrange
        Block block = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.Blocks.StoreBlockAsync(block));
    }

    [Fact]
    public async Task GetBlockByHashAsync_WithNonExistentBlock_ReturnsNull()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(32);

        // Act
        var result = await _storage.Blocks.GetBlockByHashAsync(hash);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBlockByHeightAsync_WithValidHeight_ReturnsBlock()
    {
        // Arrange
        var block = CreateTestBlock(456);
        await _storage.Blocks.StoreBlockAsync(block);

        // Act
        var retrieved = await _storage.Blocks.GetBlockByHeightAsync(456);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(456, retrieved.Header.Height);
    }

    [Fact]
    public async Task GetBlockByHeightAsync_WithNonExistentHeight_ReturnsNull()
    {
        // Act
        var result = await _storage.Blocks.GetBlockByHeightAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingBlock_ReturnsTrue()
    {
        // Arrange
        var header = CreateTestHeader();
        var hash = header.ComputeHash();
        await _storage.Blocks.StoreHeaderAsync(header);

        // Act
        var exists = await _storage.Blocks.ExistsAsync(hash);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentBlock_ReturnsFalse()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(32);

        // Act
        var exists = await _storage.Blocks.ExistsAsync(hash);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithInvalidHashSize_ThrowsArgumentException()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(16);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.Blocks.ExistsAsync(hash));
    }
}
