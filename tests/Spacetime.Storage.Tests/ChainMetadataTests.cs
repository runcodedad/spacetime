using System.Security.Cryptography;

namespace Spacetime.Storage.Tests;

public class ChainMetadataTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly RocksDbChainStorage _storage;

    public ChainMetadataTests()
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
    public void GetBestBlockHashAsync_WithNoData_ReturnsNull()
    {
        // Act
        var result = _storage.Metadata.GetBestBlockHash();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SetBestBlockHashAsync_WithValidHash_StoresSuccessfully()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(32);

        // Act
        _storage.Metadata.SetBestBlockHash(hash);

        // Assert
        var retrieved = _storage.Metadata.GetBestBlockHash();
        Assert.NotNull(retrieved);
        Assert.True(hash.AsSpan().SequenceEqual(retrieved.Value.Span));
    }

    [Fact]
    public void SetBestBlockHashAsync_WithInvalidHashSize_ThrowsArgumentException()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(16);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _storage.Metadata.SetBestBlockHash(hash));
    }

    [Fact]
    public void SetBestBlockHashAsync_UpdatesExistingHash()
    {
        // Arrange
        var hash1 = RandomNumberGenerator.GetBytes(32);
        var hash2 = RandomNumberGenerator.GetBytes(32);
        _storage.Metadata.SetBestBlockHash(hash1);

        // Act
        _storage.Metadata.SetBestBlockHash(hash2);

        // Assert
        var retrieved = _storage.Metadata.GetBestBlockHash();
        Assert.NotNull(retrieved);
        Assert.True(hash2.AsSpan().SequenceEqual(retrieved.Value.Span));
    }

    [Fact]
    public void GetChainHeightAsync_WithNoData_ReturnsNull()
    {
        // Act
        var result = _storage.Metadata.GetChainHeight();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SetChainHeightAsync_WithValidHeight_StoresSuccessfully()
    {
        // Arrange
        var height = 100L;

        // Act
        _storage.Metadata.SetChainHeight(height);

        // Assert
        var retrieved = _storage.Metadata.GetChainHeight();
        Assert.NotNull(retrieved);
        Assert.Equal(height, retrieved.Value);
    }

    [Fact]
    public void SetChainHeightAsync_WithNegativeHeight_ThrowsArgumentException()
    {
        // Arrange
        var height = -1L;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _storage.Metadata.SetChainHeight(height));
    }

    [Fact]
    public void SetChainHeightAsync_UpdatesExistingHeight()
    {
        // Arrange
        _storage.Metadata.SetChainHeight(100);

        // Act
        _storage.Metadata.SetChainHeight(200);

        // Assert
        var retrieved = _storage.Metadata.GetChainHeight();
        Assert.NotNull(retrieved);
        Assert.Equal(200, retrieved.Value);
    }

    [Fact]
    public void SetChainHeightAsync_WithZeroHeight_StoresSuccessfully()
    {
        // Arrange
        var height = 0L;

        // Act
        _storage.Metadata.SetChainHeight(height);

        // Assert
        var retrieved = _storage.Metadata.GetChainHeight();
        Assert.NotNull(retrieved);
        Assert.Equal(height, retrieved.Value);
    }
}
