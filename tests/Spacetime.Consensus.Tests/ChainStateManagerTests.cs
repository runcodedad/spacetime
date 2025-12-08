using NSubstitute;
using System.Security.Cryptography;
using Spacetime.Core;
using Spacetime.Storage;

namespace Spacetime.Consensus.Tests;

/// <summary>
/// Unit tests for ChainStateManager.
/// </summary>
public class ChainStateManagerTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly RocksDbChainStorage _storage;
    private readonly ISignatureVerifier _signatureVerifier;
    private readonly ChainStateManager _stateManager;

    public ChainStateManagerTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"spacetime_test_{Guid.NewGuid():N}");
        _storage = RocksDbChainStorage.Open(_testDbPath);
        _signatureVerifier = Substitute.For<ISignatureVerifier>();
        _stateManager = new ChainStateManager(_storage, _signatureVerifier);

        // Setup default signature verification to succeed
        _signatureVerifier.VerifySignature(
            Arg.Any<byte[]>(),
            Arg.Any<byte[]>(),
            Arg.Any<byte[]>())
            .ReturnsForAnyArgs(true);
    }

    public void Dispose()
    {
        _storage.Dispose();
        if (Directory.Exists(_testDbPath))
        {
            Directory.Delete(_testDbPath, recursive: true);
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullStorage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ChainStateManager(null!, _signatureVerifier));
    }

    [Fact]
    public void Constructor_WithNullSignatureVerifier_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ChainStateManager(_storage, null!));
    }

    #endregion

    #region GetBalance Tests

    [Fact]
    public void GetBalance_WithNonExistentAccount_ReturnsZero()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);

        // Act
        var balance = _stateManager.GetBalance(address);

        // Assert
        Assert.Equal(0, balance);
    }

    [Fact]
    public void GetBalance_WithExistingAccount_ReturnsBalance()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);
        _storage.Accounts.StoreAccount(address, new AccountState(1000, 0));

        // Act
        var balance = _stateManager.GetBalance(address);

        // Assert
        Assert.Equal(1000, balance);
    }

    #endregion

    #region GetNonce Tests

    [Fact]
    public void GetNonce_WithNonExistentAccount_ReturnsZero()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);

        // Act
        var nonce = _stateManager.GetNonce(address);

        // Assert
        Assert.Equal(0, nonce);
    }

    [Fact]
    public void GetNonce_WithExistingAccount_ReturnsNonce()
    {
        // Arrange
        var address = RandomNumberGenerator.GetBytes(33);
        _storage.Accounts.StoreAccount(address, new AccountState(1000, 5));

        // Act
        var nonce = _stateManager.GetNonce(address);

        // Assert
        Assert.Equal(5, nonce);
    }

    #endregion

    #region ValidateBlockStateAsync Tests

    [Fact]
    public async Task ValidateBlockStateAsync_WithNullBlock_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _stateManager.ValidateBlockStateAsync(null!));
    }

    [Fact]
    public async Task ValidateBlockStateAsync_WithValidBlock_ReturnsTrue()
    {
        // Arrange
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        // Setup sender with sufficient balance
        _storage.Accounts.StoreAccount(sender, new AccountState(2000, 0));

        var tx = CreateTransaction(sender, recipient, amount: 1000, nonce: 0, fee: 10);
        var block = CreateBlock(new[] { tx }, miner);

        // Act
        var result = await _stateManager.ValidateBlockStateAsync(block);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateBlockStateAsync_WithInsufficientBalance_ReturnsFalse()
    {
        // Arrange
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        // Setup sender with insufficient balance
        _storage.Accounts.StoreAccount(sender, new AccountState(500, 0));

        var tx = CreateTransaction(sender, recipient, amount: 1000, nonce: 0, fee: 10);
        var block = CreateBlock(new[] { tx }, miner);

        // Act
        var result = await _stateManager.ValidateBlockStateAsync(block);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateBlockStateAsync_WithIncorrectNonce_ReturnsFalse()
    {
        // Arrange
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        // Setup sender with nonce 5
        _storage.Accounts.StoreAccount(sender, new AccountState(2000, 5));

        // Transaction with wrong nonce
        var tx = CreateTransaction(sender, recipient, amount: 1000, nonce: 0, fee: 10);
        var block = CreateBlock(new[] { tx }, miner);

        // Act
        var result = await _stateManager.ValidateBlockStateAsync(block);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateBlockStateAsync_WithInvalidSignature_ReturnsFalse()
    {
        // Arrange
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        _storage.Accounts.StoreAccount(sender, new AccountState(2000, 0));

        var tx = CreateTransaction(sender, recipient, amount: 1000, nonce: 0, fee: 10);
        var block = CreateBlock(new[] { tx }, miner);

        // Setup signature verification to fail
        _signatureVerifier.VerifySignature(
            Arg.Any<byte[]>(),
            Arg.Any<byte[]>(),
            Arg.Any<byte[]>())
            .Returns(false);

        // Act
        var result = await _stateManager.ValidateBlockStateAsync(block);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateBlockStateAsync_WithMultipleTransactionsFromSameAccount_TracksStateCorrectly()
    {
        // Arrange
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient1 = RandomNumberGenerator.GetBytes(33);
        var recipient2 = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        // Setup sender with balance 3000
        _storage.Accounts.StoreAccount(sender, new AccountState(3000, 0));

        // Two transactions from same sender
        var tx1 = CreateTransaction(sender, recipient1, amount: 1000, nonce: 0, fee: 10);
        var tx2 = CreateTransaction(sender, recipient2, amount: 500, nonce: 1, fee: 10);
        var block = CreateBlock(new[] { tx1, tx2 }, miner);

        // Act
        var result = await _stateManager.ValidateBlockStateAsync(block);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateBlockStateAsync_WithDoubleSpending_ReturnsFalse()
    {
        // Arrange
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient1 = RandomNumberGenerator.GetBytes(33);
        var recipient2 = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        // Setup sender with balance 1500 (not enough for both transactions)
        _storage.Accounts.StoreAccount(sender, new AccountState(1500, 0));

        var tx1 = CreateTransaction(sender, recipient1, amount: 1000, nonce: 0, fee: 10);
        var tx2 = CreateTransaction(sender, recipient2, amount: 600, nonce: 1, fee: 10);
        var block = CreateBlock(new[] { tx1, tx2 }, miner);

        // Act
        var result = await _stateManager.ValidateBlockStateAsync(block);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ApplyBlockAsync Tests

    [Fact]
    public async Task ApplyBlockAsync_WithNullBlock_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _stateManager.ApplyBlockAsync(null!));
    }

    [Fact]
    public async Task ApplyBlockAsync_WithValidBlock_UpdatesAccountStates()
    {
        // Arrange
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        _storage.Accounts.StoreAccount(sender, new AccountState(2000, 0));

        var tx = CreateTransaction(sender, recipient, amount: 1000, nonce: 0, fee: 10);
        var block = CreateBlock(new[] { tx }, miner);

        // Act
        await _stateManager.ApplyBlockAsync(block);

        // Assert
        var senderBalance = _stateManager.GetBalance(sender);
        var senderNonce = _stateManager.GetNonce(sender);
        var recipientBalance = _stateManager.GetBalance(recipient);
        var minerBalance = _stateManager.GetBalance(miner);

        Assert.Equal(990, senderBalance); // 2000 - 1000 - 10
        Assert.Equal(1, senderNonce);
        Assert.Equal(1000, recipientBalance);
        Assert.Equal(10, minerBalance); // Fee reward
    }

    [Fact]
    public async Task ApplyBlockAsync_WithMultipleTransactions_UpdatesAllAccounts()
    {
        // Arrange
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient1 = RandomNumberGenerator.GetBytes(33);
        var recipient2 = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        _storage.Accounts.StoreAccount(sender, new AccountState(3000, 0));

        var tx1 = CreateTransaction(sender, recipient1, amount: 1000, nonce: 0, fee: 10);
        var tx2 = CreateTransaction(sender, recipient2, amount: 500, nonce: 1, fee: 5);
        var block = CreateBlock(new[] { tx1, tx2 }, miner);

        // Act
        await _stateManager.ApplyBlockAsync(block);

        // Assert
        var senderBalance = _stateManager.GetBalance(sender);
        var senderNonce = _stateManager.GetNonce(sender);
        var recipient1Balance = _stateManager.GetBalance(recipient1);
        var recipient2Balance = _stateManager.GetBalance(recipient2);
        var minerBalance = _stateManager.GetBalance(miner);

        Assert.Equal(1485, senderBalance); // 3000 - 1000 - 10 - 500 - 5
        Assert.Equal(2, senderNonce);
        Assert.Equal(1000, recipient1Balance);
        Assert.Equal(500, recipient2Balance);
        Assert.Equal(15, minerBalance); // Total fees: 10 + 5
    }

    [Fact]
    public async Task ApplyBlockAsync_WithInvalidBlock_ThrowsInvalidOperationException()
    {
        // Arrange
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        // Don't setup sender account (insufficient balance)
        var tx = CreateTransaction(sender, recipient, amount: 1000, nonce: 0, fee: 10);
        var block = CreateBlock(new[] { tx }, miner);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _stateManager.ApplyBlockAsync(block));
    }

    #endregion

    #region Snapshot Tests

    [Fact]
    public void CreateSnapshot_ReturnsUniqueIds()
    {
        // Act
        var snapshot1 = _stateManager.CreateSnapshot();
        var snapshot2 = _stateManager.CreateSnapshot();

        // Assert
        Assert.NotEqual(snapshot1, snapshot2);
        Assert.True(snapshot1 > 0);
        Assert.True(snapshot2 > 0);
    }

    [Fact]
    public void ReleaseSnapshot_RemovesSnapshot()
    {
        // Arrange
        var snapshotId = _stateManager.CreateSnapshot();

        // Act
        _stateManager.ReleaseSnapshot(snapshotId);

        // Assert - releasing again should not throw
        _stateManager.ReleaseSnapshot(snapshotId);
    }

    [Fact]
    public void RevertToSnapshot_WithInvalidSnapshot_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => _stateManager.RevertToSnapshot(999));
    }

    #endregion

    #region ComputeStateRoot Tests

    [Fact]
    public void ComputeStateRoot_ReturnsHash()
    {
        // Act
        var stateRoot = _stateManager.ComputeStateRoot();

        // Assert
        Assert.NotNull(stateRoot);
        Assert.Equal(32, stateRoot.Length);
    }

    #endregion

    #region CheckConsistency Tests

    [Fact]
    public void CheckConsistency_WithHealthyStorage_ReturnsTrue()
    {
        // Act
        var result = _stateManager.CheckConsistency();

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Helper Methods

    private static Transaction CreateTransaction(
        byte[] sender,
        byte[] recipient,
        long amount,
        long nonce,
        long fee)
    {
        var signature = RandomNumberGenerator.GetBytes(64);
        return new Transaction(
            sender: sender,
            recipient: recipient,
            amount: amount,
            nonce: nonce,
            fee: fee,
            signature: signature);
    }

    private static Block CreateBlock(Transaction[] transactions, byte[] minerId)
    {
        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 1,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            difficulty: 1000,
            epoch: 1,
            challenge: RandomNumberGenerator.GetBytes(32),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32),
            txRoot: RandomNumberGenerator.GetBytes(32),
            minerId: minerId,
            signature: RandomNumberGenerator.GetBytes(64));

        var plotMetadata = BlockPlotMetadata.Create(
            leafCount: 1024,
            plotId: RandomNumberGenerator.GetBytes(32),
            plotHeaderHash: RandomNumberGenerator.GetBytes(32),
            version: 1);

        var proof = new BlockProof(
            leafValue: RandomNumberGenerator.GetBytes(32),
            leafIndex: 0,
            merkleProofPath: Array.Empty<byte[]>(),
            orientationBits: Array.Empty<bool>(),
            plotMetadata: plotMetadata);

        var body = new BlockBody(transactions, proof);
        return new Block(header, body);
    }

    #endregion
}
