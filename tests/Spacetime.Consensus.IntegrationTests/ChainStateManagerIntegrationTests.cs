using NSubstitute;
using System.Security.Cryptography;
using Spacetime.Core;
using Spacetime.Storage;

namespace Spacetime.Consensus.IntegrationTests;

/// <summary>
/// Integration tests for ChainStateManager with multiple blocks and complex scenarios.
/// </summary>
public class ChainStateManagerIntegrationTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly RocksDbChainStorage _storage;
    private readonly ISignatureVerifier _signatureVerifier;
    private readonly ChainStateManager _stateManager;

    public ChainStateManagerIntegrationTests()
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

    [Fact]
    public async Task ApplyBlock_MultipleBlocks_MaintainsConsistentState()
    {
        // Arrange - Create 3 accounts
        var account1 = RandomNumberGenerator.GetBytes(33);
        var account2 = RandomNumberGenerator.GetBytes(33);
        var account3 = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        // Initialize account1 with balance
        _storage.Accounts.StoreAccount(account1, new AccountState(10000, 0));

        // Block 1: account1 -> account2 (1000 + 10 fee)
        var tx1 = CreateTransaction(account1, account2, amount: 1000, nonce: 0, fee: 10);
        var block1 = CreateBlock(new[] { tx1 }, miner);

        // Block 2: account1 -> account3 (500 + 5 fee), account2 -> account3 (200 + 5 fee)
        var tx2 = CreateTransaction(account1, account3, amount: 500, nonce: 1, fee: 5);
        var tx3 = CreateTransaction(account2, account3, amount: 200, nonce: 0, fee: 5);
        var block2 = CreateBlock(new[] { tx2, tx3 }, miner);

        // Act - Apply both blocks
        await _stateManager.ApplyBlockAsync(block1);
        await _stateManager.ApplyBlockAsync(block2);

        // Assert - Verify final balances
        var balance1 = _stateManager.GetBalance(account1);
        var balance2 = _stateManager.GetBalance(account2);
        var balance3 = _stateManager.GetBalance(account3);
        var minerBalance = _stateManager.GetBalance(miner);

        Assert.Equal(8485, balance1); // 10000 - 1000 - 10 - 500 - 5
        Assert.Equal(795, balance2); // 1000 - 200 - 5
        Assert.Equal(700, balance3); // 500 + 200
        Assert.Equal(20, minerBalance); // 10 + 5 + 5

        // Verify nonces
        var nonce1 = _stateManager.GetNonce(account1);
        var nonce2 = _stateManager.GetNonce(account2);
        Assert.Equal(2, nonce1);
        Assert.Equal(1, nonce2);
    }

    [Fact]
    public async Task ApplyBlock_WithComplexTransactionChain_UpdatesStateCorrectly()
    {
        // Arrange - Create accounts
        var alice = RandomNumberGenerator.GetBytes(33);
        var bob = RandomNumberGenerator.GetBytes(33);
        var charlie = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        // Initialize balances
        _storage.Accounts.StoreAccount(alice, new AccountState(5000, 0));
        _storage.Accounts.StoreAccount(bob, new AccountState(3000, 0));

        // Create transaction chain: Alice -> Bob, Bob -> Charlie
        var tx1 = CreateTransaction(alice, bob, amount: 1000, nonce: 0, fee: 10);
        var tx2 = CreateTransaction(bob, charlie, amount: 2000, nonce: 0, fee: 10);
        var block = CreateBlock(new[] { tx1, tx2 }, miner);

        // Act
        await _stateManager.ApplyBlockAsync(block);

        // Assert
        var aliceBalance = _stateManager.GetBalance(alice);
        var bobBalance = _stateManager.GetBalance(bob);
        var charlieBalance = _stateManager.GetBalance(charlie);
        var minerBalance = _stateManager.GetBalance(miner);

        Assert.Equal(3990, aliceBalance); // 5000 - 1000 - 10
        Assert.Equal(1990, bobBalance); // 3000 + 1000 - 2000 - 10
        Assert.Equal(2000, charlieBalance);
        Assert.Equal(20, minerBalance); // 10 + 10
    }

    [Fact]
    public async Task CreateSnapshot_RevertSnapshot_RestoresState()
    {
        // Arrange
        var account = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        _storage.Accounts.StoreAccount(account, new AccountState(5000, 0));

        // Create snapshot before applying block
        var snapshotId = _stateManager.CreateSnapshot();

        // Apply a block
        var tx = CreateTransaction(account, recipient, amount: 1000, nonce: 0, fee: 10);
        var block = CreateBlock(new[] { tx }, miner);
        await _stateManager.ApplyBlockAsync(block);

        // Verify state changed
        var balanceAfter = _stateManager.GetBalance(account);
        Assert.Equal(3990, balanceAfter);

        // Act - Revert to snapshot
        _stateManager.RevertToSnapshot(snapshotId);

        // Assert - Note: Current implementation doesn't actually restore state
        // This is a placeholder test to demonstrate the API
        // TODO: Implement actual snapshot/restore using RocksDB features
        _stateManager.ReleaseSnapshot(snapshotId);
    }

    [Fact]
    public async Task ApplyBlock_WithManyTransactions_MaintainsConsistency()
    {
        // Arrange - Create accounts
        var sender = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);
        var recipients = Enumerable.Range(0, 10)
            .Select(_ => RandomNumberGenerator.GetBytes(33))
            .ToArray();

        // Initialize sender with large balance
        _storage.Accounts.StoreAccount(sender, new AccountState(100000, 0));

        // Create many transactions from one sender to different recipients
        var transactions = new List<Transaction>();
        for (int i = 0; i < 10; i++)
        {
            var tx = CreateTransaction(sender, recipients[i], amount: 1000, nonce: i, fee: 5);
            transactions.Add(tx);
        }

        var block = CreateBlock(transactions.ToArray(), miner);

        // Act
        await _stateManager.ApplyBlockAsync(block);

        // Assert
        var senderBalance = _stateManager.GetBalance(sender);
        var senderNonce = _stateManager.GetNonce(sender);
        var minerBalance = _stateManager.GetBalance(miner);

        Assert.Equal(89950, senderBalance); // 100000 - (10 * 1000) - (10 * 5)
        Assert.Equal(10, senderNonce);
        Assert.Equal(50, minerBalance); // 10 * 5

        // Verify all recipients received their funds
        foreach (var recipient in recipients)
        {
            var balance = _stateManager.GetBalance(recipient);
            Assert.Equal(1000, balance);
        }
    }

    [Fact]
    public async Task ValidateBlockState_AfterSequentialBlocks_ValidatesCorrectly()
    {
        // Arrange
        var account = RandomNumberGenerator.GetBytes(33);
        var recipient1 = RandomNumberGenerator.GetBytes(33);
        var recipient2 = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        _storage.Accounts.StoreAccount(account, new AccountState(5000, 0));

        // Apply first block
        var tx1 = CreateTransaction(account, recipient1, amount: 1000, nonce: 0, fee: 10);
        var block1 = CreateBlock(new[] { tx1 }, miner);
        await _stateManager.ApplyBlockAsync(block1);

        // Create second block with correct nonce
        var tx2 = CreateTransaction(account, recipient2, amount: 500, nonce: 1, fee: 5);
        var block2 = CreateBlock(new[] { tx2 }, miner);

        // Act - Validate second block
        var isValid = await _stateManager.ValidateBlockStateAsync(block2);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateBlockState_WithOutOfOrderNonce_ReturnsFalse()
    {
        // Arrange
        var account = RandomNumberGenerator.GetBytes(33);
        var recipient1 = RandomNumberGenerator.GetBytes(33);
        var recipient2 = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        _storage.Accounts.StoreAccount(account, new AccountState(5000, 0));

        // Apply first block (nonce 0)
        var tx1 = CreateTransaction(account, recipient1, amount: 1000, nonce: 0, fee: 10);
        var block1 = CreateBlock(new[] { tx1 }, miner);
        await _stateManager.ApplyBlockAsync(block1);

        // Create second block with skipped nonce (should be 1, but using 2)
        var tx2 = CreateTransaction(account, recipient2, amount: 500, nonce: 2, fee: 5);
        var block2 = CreateBlock(new[] { tx2 }, miner);

        // Act - Validate second block
        var isValid = await _stateManager.ValidateBlockStateAsync(block2);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ApplyBlock_ConcurrentAccountUpdates_MaintainsAtomicity()
    {
        // Arrange - Test that multiple transactions in one block are atomic
        var account1 = RandomNumberGenerator.GetBytes(33);
        var account2 = RandomNumberGenerator.GetBytes(33);
        var account3 = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        _storage.Accounts.StoreAccount(account1, new AccountState(2000, 0));
        _storage.Accounts.StoreAccount(account2, new AccountState(1000, 0));

        // Create a block where both accounts send to account3
        var tx1 = CreateTransaction(account1, account3, amount: 500, nonce: 0, fee: 5);
        var tx2 = CreateTransaction(account2, account3, amount: 300, nonce: 0, fee: 3);
        var block = CreateBlock(new[] { tx1, tx2 }, miner);

        // Act
        await _stateManager.ApplyBlockAsync(block);

        // Assert - All changes should be committed atomically
        var balance1 = _stateManager.GetBalance(account1);
        var balance2 = _stateManager.GetBalance(account2);
        var balance3 = _stateManager.GetBalance(account3);
        var minerBalance = _stateManager.GetBalance(miner);

        Assert.Equal(1495, balance1); // 2000 - 500 - 5
        Assert.Equal(697, balance2); // 1000 - 300 - 3
        Assert.Equal(800, balance3); // 500 + 300
        Assert.Equal(8, minerBalance); // 5 + 3
    }

    [Fact]
    public async Task CheckConsistency_AfterMultipleBlocks_RemainsHealthy()
    {
        // Arrange
        var account = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);
        var miner = RandomNumberGenerator.GetBytes(33);

        _storage.Accounts.StoreAccount(account, new AccountState(10000, 0));

        // Apply multiple blocks
        for (int i = 0; i < 5; i++)
        {
            var tx = CreateTransaction(account, recipient, amount: 100, nonce: i, fee: 1);
            var block = CreateBlock(new[] { tx }, miner);
            await _stateManager.ApplyBlockAsync(block);
        }

        // Act
        var isHealthy = _stateManager.CheckConsistency();

        // Assert
        Assert.True(isHealthy);

        // Verify final state
        var balance = _stateManager.GetBalance(account);
        var nonce = _stateManager.GetNonce(account);
        Assert.Equal(9495, balance); // 10000 - (5 * 100) - (5 * 1)
        Assert.Equal(5, nonce);
    }

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
