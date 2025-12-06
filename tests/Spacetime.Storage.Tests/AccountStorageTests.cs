using System.Security.Cryptography;

namespace Spacetime.Storage.Tests;

public class AccountStorageTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly RocksDbChainStorage _storage;

    public AccountStorageTests()
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
    public async Task StoreAccountAsync_WithValidAccount_StoresSuccessfully()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);
        var account = new AccountState(1000, 1);

        // Act
        await _storage.Accounts.StoreAccountAsync(address, account);

        // Assert
        var retrieved = await _storage.Accounts.GetAccountAsync(address);
        Assert.NotNull(retrieved);
        Assert.Equal(account.Balance, retrieved.Balance);
        Assert.Equal(account.Nonce, retrieved.Nonce);
    }

    [Fact]
    public async Task StoreAccountAsync_WithNullAccount_ThrowsArgumentNullException()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);
        AccountState account = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.Accounts.StoreAccountAsync(address, account));
    }

    [Fact]
    public async Task StoreAccountAsync_WithEmptyAddress_ThrowsArgumentException()
    {
        // Arrange
        var address = Array.Empty<byte>();
        var account = new AccountState(1000, 1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.Accounts.StoreAccountAsync(address, account));
    }

    [Fact]
    public async Task GetAccountAsync_WithNonExistentAccount_ReturnsNull()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);

        // Act
        var result = await _storage.Accounts.GetAccountAsync(address);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAccountAsync_WithEmptyAddress_ThrowsArgumentException()
    {
        // Arrange
        var address = Array.Empty<byte>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.Accounts.GetAccountAsync(address));
    }

    [Fact]
    public async Task ExistsAsync_WithExistingAccount_ReturnsTrue()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);
        var account = new AccountState(1000, 1);
        await _storage.Accounts.StoreAccountAsync(address, account);

        // Act
        var exists = await _storage.Accounts.ExistsAsync(address);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentAccount_ReturnsFalse()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);

        // Act
        var exists = await _storage.Accounts.ExistsAsync(address);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithEmptyAddress_ThrowsArgumentException()
    {
        // Arrange
        var address = Array.Empty<byte>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.Accounts.ExistsAsync(address));
    }

    [Fact]
    public async Task DeleteAccountAsync_WithExistingAccount_DeletesSuccessfully()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);
        var account = new AccountState(1000, 1);
        await _storage.Accounts.StoreAccountAsync(address, account);

        // Act
        await _storage.Accounts.DeleteAccountAsync(address);

        // Assert
        var exists = await _storage.Accounts.ExistsAsync(address);
        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteAccountAsync_WithEmptyAddress_ThrowsArgumentException()
    {
        // Arrange
        var address = Array.Empty<byte>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.Accounts.DeleteAccountAsync(address));
    }

    [Fact]
    public async Task StoreAccountAsync_UpdatesExistingAccount()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);
        var account1 = new AccountState(1000, 1);
        var account2 = new AccountState(2000, 2);
        await _storage.Accounts.StoreAccountAsync(address, account1);

        // Act
        await _storage.Accounts.StoreAccountAsync(address, account2);

        // Assert
        var retrieved = await _storage.Accounts.GetAccountAsync(address);
        Assert.NotNull(retrieved);
        Assert.Equal(account2.Balance, retrieved.Balance);
        Assert.Equal(account2.Nonce, retrieved.Nonce);
    }
}
