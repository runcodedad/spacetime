using Spacetime.Core;
using System.Security.Cryptography;

namespace Spacetime.Storage.Tests;

public class RocksDbChainStorageTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly RocksDbChainStorage _storage;

    public RocksDbChainStorageTests()
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
    public void Open_WithValidPath_CreatesDatabase()
    {
        // Arrange & Act is done in constructor

        // Assert
        Assert.NotNull(_storage);
        Assert.NotNull(_storage.Blocks);
        Assert.NotNull(_storage.Transactions);
        Assert.NotNull(_storage.Accounts);
        Assert.NotNull(_storage.Metadata);
        Assert.True(Directory.Exists(_testDbPath));
    }

    [Fact]
    public void Open_WithNullPath_ThrowsArgumentNullException()
    {
        // Arrange
        string path = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => RocksDbChainStorage.Open(path));
    }

    [Fact]
    public void Open_WithEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var path = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => RocksDbChainStorage.Open(path));
    }

    [Fact]
    public void CreateWriteBatch_ReturnsValidBatch()
    {
        // Act
        using var batch = _storage.CreateWriteBatch();

        // Assert
        Assert.NotNull(batch);
    }

    [Fact]
    public void CommitBatchAsync_WithValidBatch_Succeeds()
    {
        // Arrange
        using var batch = _storage.CreateWriteBatch();
        var key = RandomNumberGenerator.GetBytes(32);
        var value = RandomNumberGenerator.GetBytes(64);
        batch.Put(key, value);

        // Act
        _storage.CommitBatch(batch);

        // Assert - No exception thrown
    }

    [Fact]
    public void CommitBatchAsync_WithNullBatch_ThrowsArgumentNullException()
    {
        // Arrange
        IWriteBatch batch = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _storage.CommitBatch(batch));
    }

    [Fact]
    public void CompactAsync_Succeeds()
    {
        // Act
        _storage.Compact();

        // Assert - No exception thrown
    }

    [Fact]
    public void CheckIntegrityAsync_ReturnsTrue()
    {
        // Act
        var result = _storage.CheckIntegrity();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DisposeAsync_DisposesResources()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"spacetime_test_{Guid.NewGuid():N}");
        var storage = RocksDbChainStorage.Open(tempPath);

        // Act
        storage.DisposeAsync().AsTask().Wait();

        // Assert
        // Verify we can't use the storage after disposal
        Assert.Throws<ObjectDisposedException>(() => storage.CreateWriteBatch());

        // Cleanup
        if (Directory.Exists(tempPath))
        {
            Directory.Delete(tempPath, recursive: true);
        }
    }
}
