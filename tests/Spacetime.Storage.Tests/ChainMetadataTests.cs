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
    public async Task GetBestBlockHashAsync_WithNoData_ReturnsNull()
    {
        // Act
        var result = await _storage.Metadata.GetBestBlockHashAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetBestBlockHashAsync_WithValidHash_StoresSuccessfully()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(32);

        // Act
        await _storage.Metadata.SetBestBlockHashAsync(hash);

        // Assert
        var retrieved = await _storage.Metadata.GetBestBlockHashAsync();
        Assert.NotNull(retrieved);
        Assert.True(hash.AsSpan().SequenceEqual(retrieved.Value.Span));
    }

    [Fact]
    public async Task SetBestBlockHashAsync_WithInvalidHashSize_ThrowsArgumentException()
    {
        // Arrange
        var hash = RandomNumberGenerator.GetBytes(16);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.Metadata.SetBestBlockHashAsync(hash));
    }

    [Fact]
    public async Task SetBestBlockHashAsync_UpdatesExistingHash()
    {
        // Arrange
        var hash1 = RandomNumberGenerator.GetBytes(32);
        var hash2 = RandomNumberGenerator.GetBytes(32);
        await _storage.Metadata.SetBestBlockHashAsync(hash1);

        // Act
        await _storage.Metadata.SetBestBlockHashAsync(hash2);

        // Assert
        var retrieved = await _storage.Metadata.GetBestBlockHashAsync();
        Assert.NotNull(retrieved);
        Assert.True(hash2.AsSpan().SequenceEqual(retrieved.Value.Span));
    }

    [Fact]
    public async Task GetChainHeightAsync_WithNoData_ReturnsNull()
    {
        // Act
        var result = await _storage.Metadata.GetChainHeightAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetChainHeightAsync_WithValidHeight_StoresSuccessfully()
    {
        // Arrange
        var height = 100L;

        // Act
        await _storage.Metadata.SetChainHeightAsync(height);

        // Assert
        var retrieved = await _storage.Metadata.GetChainHeightAsync();
        Assert.NotNull(retrieved);
        Assert.Equal(height, retrieved.Value);
    }

    [Fact]
    public async Task SetChainHeightAsync_WithNegativeHeight_ThrowsArgumentException()
    {
        // Arrange
        var height = -1L;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.Metadata.SetChainHeightAsync(height));
    }

    [Fact]
    public async Task SetChainHeightAsync_UpdatesExistingHeight()
    {
        // Arrange
        await _storage.Metadata.SetChainHeightAsync(100);

        // Act
        await _storage.Metadata.SetChainHeightAsync(200);

        // Assert
        var retrieved = await _storage.Metadata.GetChainHeightAsync();
        Assert.NotNull(retrieved);
        Assert.Equal(200, retrieved.Value);
    }

    [Fact]
    public async Task SetChainHeightAsync_WithZeroHeight_StoresSuccessfully()
    {
        // Arrange
        var height = 0L;

        // Act
        await _storage.Metadata.SetChainHeightAsync(height);

        // Assert
        var retrieved = await _storage.Metadata.GetChainHeightAsync();
        Assert.NotNull(retrieved);
        Assert.Equal(height, retrieved.Value);
    }
}
