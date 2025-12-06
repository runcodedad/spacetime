using Spacetime.Core;
using System.Security.Cryptography;

namespace Spacetime.Storage.Tests;

public class TransactionIndexTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly RocksDbChainStorage _storage;

    public TransactionIndexTests()
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

    [Fact]
    public async Task IndexTransactionAsync_WithValidData_IndexesSuccessfully()
    {
        // Arrange
        var txHash = RandomNumberGenerator.GetBytes(32);
        var blockHash = RandomNumberGenerator.GetBytes(32);
        var blockHeight = 100L;
        var txIndex = 0;

        // Act
        await _storage.Transactions.IndexTransactionAsync(txHash, blockHash, blockHeight, txIndex);

        // Assert
        var location = await _storage.Transactions.GetTransactionLocationAsync(txHash);
        Assert.NotNull(location);
        Assert.Equal(blockHeight, location.BlockHeight);
        Assert.Equal(txIndex, location.TransactionIndex);
    }

    [Fact]
    public async Task IndexTransactionAsync_WithInvalidTxHashSize_ThrowsArgumentException()
    {
        // Arrange
        var txHash = RandomNumberGenerator.GetBytes(16);
        var blockHash = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _storage.Transactions.IndexTransactionAsync(txHash, blockHash, 100, 0));
    }

    [Fact]
    public async Task IndexTransactionAsync_WithInvalidBlockHashSize_ThrowsArgumentException()
    {
        // Arrange
        var txHash = RandomNumberGenerator.GetBytes(32);
        var blockHash = RandomNumberGenerator.GetBytes(16);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _storage.Transactions.IndexTransactionAsync(txHash, blockHash, 100, 0));
    }

    [Fact]
    public async Task IndexTransactionAsync_WithNegativeHeight_ThrowsArgumentException()
    {
        // Arrange
        var txHash = RandomNumberGenerator.GetBytes(32);
        var blockHash = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _storage.Transactions.IndexTransactionAsync(txHash, blockHash, -1, 0));
    }

    [Fact]
    public async Task IndexTransactionAsync_WithNegativeTxIndex_ThrowsArgumentException()
    {
        // Arrange
        var txHash = RandomNumberGenerator.GetBytes(32);
        var blockHash = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _storage.Transactions.IndexTransactionAsync(txHash, blockHash, 100, -1));
    }

    [Fact]
    public async Task GetTransactionLocationAsync_WithNonExistentHash_ReturnsNull()
    {
        // Arrange
        var txHash = RandomNumberGenerator.GetBytes(32);

        // Act
        var location = await _storage.Transactions.GetTransactionLocationAsync(txHash);

        // Assert
        Assert.Null(location);
    }

    [Fact]
    public async Task GetTransactionLocationAsync_WithInvalidHashSize_ThrowsArgumentException()
    {
        // Arrange
        var txHash = RandomNumberGenerator.GetBytes(16);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _storage.Transactions.GetTransactionLocationAsync(txHash));
    }

    [Fact]
    public async Task GetTransactionAsync_WithExistingTransaction_ReturnsTransaction()
    {
        // Arrange
        var tx = new Transaction(
            RandomNumberGenerator.GetBytes(33),
            RandomNumberGenerator.GetBytes(33),
            1000,
            1,
            10,
            RandomNumberGenerator.GetBytes(64));

        var metadata = BlockPlotMetadata.Create(
            1000,
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32),
            1);

        var proof = new BlockProof(
            RandomNumberGenerator.GetBytes(32),
            42,
            new[] { RandomNumberGenerator.GetBytes(32) },
            new[] { true },
            metadata);

        var body = new BlockBody(new[] { tx }, proof);
        var header = new BlockHeader(
            BlockHeader.CurrentVersion,
            RandomNumberGenerator.GetBytes(32),
            100,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            1000,
            10,
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(33),
            RandomNumberGenerator.GetBytes(64));

        var block = new Block(header, body);
        var blockHash = header.ComputeHash();
        
        await _storage.Blocks.StoreBlockAsync(block);
        
        var txHash = tx.ComputeHash();
        await _storage.Transactions.IndexTransactionAsync(txHash, blockHash, header.Height, 0);

        // Act
        var retrieved = await _storage.Transactions.GetTransactionAsync(txHash);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(tx.Amount, retrieved.Amount);
    }

    [Fact]
    public async Task GetTransactionAsync_WithNonExistentHash_ReturnsNull()
    {
        // Arrange
        var txHash = RandomNumberGenerator.GetBytes(32);

        // Act
        var result = await _storage.Transactions.GetTransactionAsync(txHash);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTransactionAsync_WithInvalidHashSize_ThrowsArgumentException()
    {
        // Arrange
        var txHash = RandomNumberGenerator.GetBytes(16);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _storage.Transactions.GetTransactionAsync(txHash));
    }
}
