using System.Security.Cryptography;
using NSubstitute;
using Spacetime.Core;
using Spacetime.Storage;

namespace Spacetime.Consensus.IntegrationTests;

/// <summary>
/// Integration tests for the Mempool class with TransactionValidator.
/// </summary>
public class MempoolIntegrationTests
{
    private static Transaction CreateSignedTransaction(
        byte[] sender,
        byte[] recipient,
        long amount,
        long nonce,
        long fee)
    {
        var signature = RandomNumberGenerator.GetBytes(64);
        return new Transaction(sender, recipient, amount, nonce, fee, signature);
    }

    [Fact]
    public async Task Mempool_WithRealValidator_AcceptsValidTransaction()
    {
        // Arrange
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);

        var signatureVerifier = Substitute.For<ISignatureVerifier>();
        signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);

        var accountStorage = Substitute.For<IAccountStorage>();
        accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(10000, 0));

        var validationConfig = TransactionValidationConfig.Default;
        var validator = new TransactionValidator(
            signatureVerifier,
            accountStorage,
            null,
            validationConfig);

        var mempoolConfig = MempoolConfig.Default;
        var mempool = new Mempool(validator, mempoolConfig);

        var transaction = CreateSignedTransaction(sender, recipient, 1000, 0, 10);

        // Act
        var added = await mempool.AddTransactionAsync(transaction);

        // Assert
        Assert.True(added);
        Assert.Equal(1, mempool.Count);
    }

    [Fact]
    public async Task Mempool_WithRealValidator_RejectsInvalidSignature()
    {
        // Arrange
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);

        var signatureVerifier = Substitute.For<ISignatureVerifier>();
        signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(false); // Invalid signature

        var accountStorage = Substitute.For<IAccountStorage>();
        accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(10000, 0));

        var validationConfig = TransactionValidationConfig.Default;
        var validator = new TransactionValidator(
            signatureVerifier,
            accountStorage,
            null,
            validationConfig);

        var mempoolConfig = MempoolConfig.Default;
        var mempool = new Mempool(validator, mempoolConfig);

        var transaction = CreateSignedTransaction(sender, recipient, 1000, 0, 10);

        // Act
        var added = await mempool.AddTransactionAsync(transaction);

        // Assert
        Assert.False(added);
        Assert.Equal(0, mempool.Count);
    }

    [Fact]
    public async Task Mempool_WithRealValidator_RejectsInsufficientBalance()
    {
        // Arrange
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);

        var signatureVerifier = Substitute.For<ISignatureVerifier>();
        signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);

        var accountStorage = Substitute.For<IAccountStorage>();
        accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(500, 0)); // Insufficient balance

        var validationConfig = TransactionValidationConfig.Default;
        var validator = new TransactionValidator(
            signatureVerifier,
            accountStorage,
            null,
            validationConfig);

        var mempoolConfig = MempoolConfig.Default;
        var mempool = new Mempool(validator, mempoolConfig);

        var transaction = CreateSignedTransaction(sender, recipient, 1000, 0, 10);

        // Act
        var added = await mempool.AddTransactionAsync(transaction);

        // Assert
        Assert.False(added);
        Assert.Equal(0, mempool.Count);
    }

    [Fact]
    public async Task Mempool_WithRealValidator_RejectsInvalidNonce()
    {
        // Arrange
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);

        var signatureVerifier = Substitute.For<ISignatureVerifier>();
        signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);

        var accountStorage = Substitute.For<IAccountStorage>();
        accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(10000, 5)); // Account nonce is 5

        var validationConfig = TransactionValidationConfig.Default;
        var validator = new TransactionValidator(
            signatureVerifier,
            accountStorage,
            null,
            validationConfig);

        var mempoolConfig = MempoolConfig.Default;
        var mempool = new Mempool(validator, mempoolConfig);

        var transaction = CreateSignedTransaction(sender, recipient, 1000, 0, 10); // Wrong nonce

        // Act
        var added = await mempool.AddTransactionAsync(transaction);

        // Assert
        Assert.False(added);
        Assert.Equal(0, mempool.Count);
    }

    [Fact]
    public async Task Mempool_WithRealValidator_RejectsDuplicateTransaction()
    {
        // Arrange
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);

        var signatureVerifier = Substitute.For<ISignatureVerifier>();
        signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);

        var accountStorage = Substitute.For<IAccountStorage>();
        accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(10000, 0));

        var transactionIndex = Substitute.For<ITransactionIndex>();
        var transaction = CreateSignedTransaction(sender, recipient, 1000, 0, 10);
        var txHash = transaction.ComputeHash();
        
        // First call returns null (not found), second call returns a location (duplicate)
        transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(
                x => null,  // First call - transaction doesn't exist
                x => new TransactionLocation(RandomNumberGenerator.GetBytes(32), 1, 0)); // Second call - duplicate detected

        var validationConfig = TransactionValidationConfig.Default;
        var validator = new TransactionValidator(
            signatureVerifier,
            accountStorage,
            transactionIndex,
            validationConfig);

        var mempoolConfig = MempoolConfig.Default;
        var mempool = new Mempool(validator, mempoolConfig);

        // Act
        var firstAdd = await mempool.AddTransactionAsync(transaction);
        var secondAdd = await mempool.AddTransactionAsync(transaction);

        // Assert
        Assert.True(firstAdd);
        Assert.False(secondAdd); // Rejected as duplicate by mempool
        Assert.Equal(1, mempool.Count);
    }

    [Fact]
    public async Task Mempool_BlockBuildingScenario_ReturnsHighestFeeTransactions()
    {
        // Arrange
        var signatureVerifier = Substitute.For<ISignatureVerifier>();
        signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);

        var accountStorage = Substitute.For<IAccountStorage>();
        accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(1_000_000, 0));

        var validationConfig = TransactionValidationConfig.Default;
        var validator = new TransactionValidator(
            signatureVerifier,
            accountStorage,
            null,
            validationConfig);

        var mempoolConfig = MempoolConfig.Default;
        var mempool = new Mempool(validator, mempoolConfig);

        // Add transactions with varying fees
        var transactions = new List<Transaction>();
        for (int i = 0; i < 20; i++)
        {
            var sender = RandomNumberGenerator.GetBytes(33);
            var recipient = RandomNumberGenerator.GetBytes(33);
            var tx = CreateSignedTransaction(sender, recipient, 1000, 0, i * 10); // Fees: 0, 10, 20, ...
            transactions.Add(tx);
            await mempool.AddTransactionAsync(tx);
        }

        // Act - Build a block with top 5 transactions
        var topTransactions = await mempool.GetPendingTransactionsAsync(5);

        // Assert
        Assert.Equal(5, topTransactions.Count);
        Assert.Equal(190, topTransactions[0].Fee); // Highest fee first
        Assert.Equal(180, topTransactions[1].Fee);
        Assert.Equal(170, topTransactions[2].Fee);
        Assert.Equal(160, topTransactions[3].Fee);
        Assert.Equal(150, topTransactions[4].Fee);
    }

    [Fact]
    public async Task Mempool_AfterBlockInclusion_RemovesIncludedTransactions()
    {
        // Arrange
        var signatureVerifier = Substitute.For<ISignatureVerifier>();
        signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);

        var accountStorage = Substitute.For<IAccountStorage>();
        accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(100_000, 0));

        var validationConfig = TransactionValidationConfig.Default;
        var validator = new TransactionValidator(
            signatureVerifier,
            accountStorage,
            null,
            validationConfig);

        var mempoolConfig = MempoolConfig.Default;
        var mempool = new Mempool(validator, mempoolConfig);

        // Add transactions
        var tx1 = CreateSignedTransaction(
            RandomNumberGenerator.GetBytes(33),
            RandomNumberGenerator.GetBytes(33),
            1000, 0, 100);
        var tx2 = CreateSignedTransaction(
            RandomNumberGenerator.GetBytes(33),
            RandomNumberGenerator.GetBytes(33),
            1000, 0, 50);
        var tx3 = CreateSignedTransaction(
            RandomNumberGenerator.GetBytes(33),
            RandomNumberGenerator.GetBytes(33),
            1000, 0, 75);

        await mempool.AddTransactionAsync(tx1);
        await mempool.AddTransactionAsync(tx2);
        await mempool.AddTransactionAsync(tx3);

        // Simulate block building
        var includedTransactions = await mempool.GetPendingTransactionsAsync(2);
        Assert.Equal(2, includedTransactions.Count);

        // Act - Remove transactions that were included in the block
        var hashes = includedTransactions.Select(tx => tx.ComputeHash()).ToList();
        var removed = await mempool.RemoveTransactionsAsync(hashes);

        // Assert
        Assert.Equal(2, removed);
        Assert.Equal(1, mempool.Count);

        var remaining = await mempool.GetPendingTransactionsAsync(10);
        Assert.Single(remaining);
        Assert.Equal(50, remaining[0].Fee); // Lowest fee transaction remains
    }

    [Fact]
    public async Task Mempool_FullPoolScenario_EvictsLowestFeeTransactions()
    {
        // Arrange
        var signatureVerifier = Substitute.For<ISignatureVerifier>();
        signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);

        var accountStorage = Substitute.For<IAccountStorage>();
        accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(1_000_000, 0));

        var validationConfig = TransactionValidationConfig.Default;
        var validator = new TransactionValidator(
            signatureVerifier,
            accountStorage,
            null,
            validationConfig);

        var mempoolConfig = new MempoolConfig { MaxTransactions = 10 };
        var mempool = new Mempool(validator, mempoolConfig);

        // Fill the pool
        var lowFeeTxs = new List<Transaction>();
        for (int i = 0; i < 10; i++)
        {
            var tx = CreateSignedTransaction(
                RandomNumberGenerator.GetBytes(33),
                RandomNumberGenerator.GetBytes(33),
                1000, 0, 10 + i); // Fees: 10-19
            lowFeeTxs.Add(tx);
            await mempool.AddTransactionAsync(tx);
        }

        Assert.Equal(10, mempool.Count);

        // Act - Add higher fee transaction
        var highFeeTx = CreateSignedTransaction(
            RandomNumberGenerator.GetBytes(33),
            RandomNumberGenerator.GetBytes(33),
            1000, 0, 100);
        var added = await mempool.AddTransactionAsync(highFeeTx);

        // Assert
        Assert.True(added);
        Assert.Equal(10, mempool.Count); // Still at max capacity

        // Lowest fee transaction (fee=10) should be evicted
        var lowestFeeTxHash = lowFeeTxs[0].ComputeHash();
        var contains = await mempool.ContainsTransactionAsync(lowestFeeTxHash);
        Assert.False(contains);

        // High fee transaction should be in the pool
        var highFeeTxHash = highFeeTx.ComputeHash();
        var containsHighFee = await mempool.ContainsTransactionAsync(highFeeTxHash);
        Assert.True(containsHighFee);
    }

    [Fact]
    public async Task Mempool_ClearScenario_RemovesAllTransactions()
    {
        // Arrange
        var signatureVerifier = Substitute.For<ISignatureVerifier>();
        signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);

        var accountStorage = Substitute.For<IAccountStorage>();
        accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(100_000, 0));

        var validationConfig = TransactionValidationConfig.Default;
        var validator = new TransactionValidator(
            signatureVerifier,
            accountStorage,
            null,
            validationConfig);

        var mempoolConfig = MempoolConfig.Default;
        var mempool = new Mempool(validator, mempoolConfig);

        // Add transactions
        for (int i = 0; i < 10; i++)
        {
            var tx = CreateSignedTransaction(
                RandomNumberGenerator.GetBytes(33),
                RandomNumberGenerator.GetBytes(33),
                1000, 0, 10 + i);
            await mempool.AddTransactionAsync(tx);
        }

        Assert.Equal(10, mempool.Count);

        // Act - Clear the mempool
        await mempool.ClearAsync();

        // Assert
        Assert.Equal(0, mempool.Count);
        var transactions = await mempool.GetPendingTransactionsAsync(100);
        Assert.Empty(transactions);
    }
}
