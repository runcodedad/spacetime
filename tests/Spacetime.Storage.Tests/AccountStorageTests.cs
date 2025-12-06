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
    public void StoreAccount_WithValidAccount_StoresSuccessfully()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);
        var account = new AccountState(1000, 1);

        // Act
        _storage.Accounts.StoreAccount(address, account);

        // Assert
        var retrieved = _storage.Accounts.GetAccount(address);
        Assert.NotNull(retrieved);
        Assert.Equal(account.Balance, retrieved.Balance);
        Assert.Equal(account.Nonce, retrieved.Nonce);
    }

    [Fact]
    public void StoreAccount_WithNullAccount_ThrowsArgumentNullException()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);
        AccountState account = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _storage.Accounts.StoreAccount(address, account));
    }

    [Fact]
    public void StoreAccount_WithEmptyAddress_ThrowsArgumentException()
    {
        // Arrange
        var address = Array.Empty<byte>();
        var account = new AccountState(1000, 1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _storage.Accounts.StoreAccount(address, account));
    }

    [Fact]
    public void GetAccount_WithNonExistentAccount_ReturnsNull()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);

        // Act
        var result = _storage.Accounts.GetAccount(address);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAccount_WithEmptyAddress_ThrowsArgumentException()
    {
        // Arrange
        var address = Array.Empty<byte>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _storage.Accounts.GetAccount(address));
    }

    [Fact]
    public void Exists_WithExistingAccount_ReturnsTrue()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);
        var account = new AccountState(1000, 1);
        _storage.Accounts.StoreAccount(address, account);

        // Act
        var exists = _storage.Accounts.Exists(address);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void Exists_WithNonExistentAccount_ReturnsFalse()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);

        // Act
        var exists = _storage.Accounts.Exists(address);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void Exists_WithEmptyAddress_ThrowsArgumentException()
    {
        // Arrange
        var address = Array.Empty<byte>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _storage.Accounts.Exists(address));
    }

    [Fact]
    public void DeleteAccount_WithExistingAccount_DeletesSuccessfully()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);
        var account = new AccountState(1000, 1);
        _storage.Accounts.StoreAccount(address, account);

        // Act
        _storage.Accounts.DeleteAccount(address);

        // Assert
        var exists = _storage.Accounts.Exists(address);
        Assert.False(exists);
    }

    [Fact]
    public void DeleteAccount_WithEmptyAddress_ThrowsArgumentException()
    {
        // Arrange
        var address = Array.Empty<byte>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _storage.Accounts.DeleteAccount(address));
    }

    [Fact]
    public void StoreAccount_UpdatesExistingAccount()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);
        var account1 = new AccountState(1000, 1);
        var account2 = new AccountState(2000, 2);
        _storage.Accounts.StoreAccount(address, account1);

        // Act
        _storage.Accounts.StoreAccount(address, account2);

        // Assert
        var retrieved = _storage.Accounts.GetAccount(address);
        Assert.NotNull(retrieved);
        Assert.Equal(account2.Balance, retrieved.Balance);
        Assert.Equal(account2.Nonce, retrieved.Nonce);
    }
}
