using System.Security.Cryptography;
using NSubstitute;
using Spacetime.Core;
using Spacetime.Storage;

namespace Spacetime.Consensus.Tests;

/// <summary>
/// Unit tests for the Mempool class.
/// </summary>
public class MempoolTests
{
    private readonly ITransactionValidator _validator;
    private readonly MempoolConfig _config;

    public MempoolTests()
    {
        _validator = CreateMockValidator();
        _config = MempoolConfig.Default;
    }

    private static ITransactionValidator CreateMockValidator()
    {
        var validator = Substitute.For<ITransactionValidator>();
        // By default, all transactions are valid
        validator.ValidateTransaction(Arg.Any<Transaction>(), Arg.Any<CancellationToken>())
            .Returns(TransactionValidationResult.Success());
        return validator;
    }

    private static Transaction CreateValidTransaction(long fee = 10, long amount = 1000, long nonce = 0)
    {
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);
        var signature = RandomNumberGenerator.GetBytes(64);
        return new Transaction(sender, recipient, amount, nonce, fee, signature);
    }

    [Fact]
    public void Constructor_WithNullValidator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Mempool(null!, _config));
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Mempool(_validator, null!));
    }

    [Fact]
    public void Count_WhenEmpty_ReturnsZero()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);

        // Act & Assert
        Assert.Equal(0, mempool.Count);
    }

    [Fact]
    public void AddTransactionAsync_WithNullTransaction_ThrowsArgumentNullException()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => mempool.AddTransaction(null!));
    }

    [Fact]
    public void AddTransactionAsync_WithValidTransaction_ReturnsTrue()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);
        var transaction = CreateValidTransaction(fee: 100);

        // Act
        var added = mempool.AddTransaction(transaction);

        // Assert
        Assert.True(added);
        Assert.Equal(1, mempool.Count);
    }

    [Fact]
    public void AddTransactionAsync_WithInvalidTransaction_ReturnsFalse()
    {
        // Arrange
        var validator = CreateMockValidator();
        validator.ValidateTransaction(Arg.Any<Transaction>(), Arg.Any<CancellationToken>())
            .Returns(TransactionValidationResult.Failure(
                TransactionValidationErrorType.InvalidSignature, "Invalid signature"));
        
        var mempool = new Mempool(validator, _config);
        var transaction = CreateValidTransaction();

        // Act
        var added = mempool.AddTransaction(transaction);

        // Assert
        Assert.False(added);
        Assert.Equal(0, mempool.Count);
    }

    [Fact]
    public void AddTransactionAsync_WithDuplicateTransaction_ReturnsFalse()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);
        var transaction = CreateValidTransaction(fee: 100);

        // Act
        var firstAdd = mempool.AddTransaction(transaction);
        var secondAdd = mempool.AddTransaction(transaction);

        // Assert
        Assert.True(firstAdd);
        Assert.False(secondAdd);
        Assert.Equal(1, mempool.Count);
    }

    [Fact]
    public void AddTransactionAsync_WithFeeBelowMinimum_ReturnsFalse()
    {
        // Arrange
        var config = new MempoolConfig { MinimumFee = 10 };
        var mempool = new Mempool(_validator, config);
        var transaction = CreateValidTransaction(fee: 5); // Below minimum

        // Act
        var added = mempool.AddTransaction(transaction);

        // Assert
        Assert.False(added);
        Assert.Equal(0, mempool.Count);
    }

    [Fact]
    public void AddTransactionAsync_WhenFull_EvictsLowestFeeTransaction()
    {
        // Arrange
        var config = new MempoolConfig { MaxTransactions = 3 };
        var mempool = new Mempool(_validator, config);
        
        var tx1 = CreateValidTransaction(fee: 10);
        var tx2 = CreateValidTransaction(fee: 20);
        var tx3 = CreateValidTransaction(fee: 30);
        var tx4 = CreateValidTransaction(fee: 25); // Higher than tx1, should evict it

        // Act
        mempool.AddTransaction(tx1);
        mempool.AddTransaction(tx2);
        mempool.AddTransaction(tx3);
        var added = mempool.AddTransaction(tx4);

        // Assert
        Assert.True(added);
        Assert.Equal(3, mempool.Count);
        
        // tx1 should be evicted
        var tx1Hash = tx1.ComputeHash();
        var containsTx1 = mempool.ContainsTransaction(tx1Hash);
        Assert.False(containsTx1);
        
        // tx4 should be in the pool
        var tx4Hash = tx4.ComputeHash();
        var containsTx4 = mempool.ContainsTransaction(tx4Hash);
        Assert.True(containsTx4);
    }

    [Fact]
    public void AddTransactionAsync_WhenFull_DoesNotAddLowerFeeTransaction()
    {
        // Arrange
        var config = new MempoolConfig { MaxTransactions = 3 };
        var mempool = new Mempool(_validator, config);
        
        var tx1 = CreateValidTransaction(fee: 10);
        var tx2 = CreateValidTransaction(fee: 20);
        var tx3 = CreateValidTransaction(fee: 30);
        var tx4 = CreateValidTransaction(fee: 5); // Lower than all existing

        // Act
        mempool.AddTransaction(tx1);
        mempool.AddTransaction(tx2);
        mempool.AddTransaction(tx3);
        var added = mempool.AddTransaction(tx4);

        // Assert
        Assert.False(added);
        Assert.Equal(3, mempool.Count);
        
        // tx4 should not be in the pool
        var tx4Hash = tx4.ComputeHash();
        var containsTx4 = mempool.ContainsTransaction(tx4Hash);
        Assert.False(containsTx4);
    }

    [Fact]
    public void GetPendingTransactionsAsync_ReturnsTransactionsInFeeOrder()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);
        var tx1 = CreateValidTransaction(fee: 10);
        var tx2 = CreateValidTransaction(fee: 50);
        var tx3 = CreateValidTransaction(fee: 30);

        mempool.AddTransaction(tx1);
        mempool.AddTransaction(tx2);
        mempool.AddTransaction(tx3);

        // Act
        var transactions = mempool.GetPendingTransactions(10);

        // Assert
        Assert.Equal(3, transactions.Count);
        Assert.Equal(50, transactions[0].Fee); // Highest fee first
        Assert.Equal(30, transactions[1].Fee);
        Assert.Equal(10, transactions[2].Fee); // Lowest fee last
    }

    [Fact]
    public void GetPendingTransactionsAsync_RespectsMaxCount()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);
        mempool.AddTransaction(CreateValidTransaction(fee: 10));
        mempool.AddTransaction(CreateValidTransaction(fee: 20));
        mempool.AddTransaction(CreateValidTransaction(fee: 30));

        // Act
        var transactions = mempool.GetPendingTransactions(2);

        // Assert
        Assert.Equal(2, transactions.Count);
        Assert.Equal(30, transactions[0].Fee); // Highest fees
        Assert.Equal(20, transactions[1].Fee);
    }

    [Fact]
    public void GetPendingTransactionsAsync_WithZeroMaxCount_ReturnsEmpty()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);
        mempool.AddTransaction(CreateValidTransaction(fee: 10));

        // Act
        var transactions = mempool.GetPendingTransactions(0);

        // Assert
        Assert.Empty(transactions);
    }

    [Fact]
    public void GetPendingTransactionsAsync_WhenEmpty_ReturnsEmpty()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);

        // Act
        var transactions = mempool.GetPendingTransactions(10);

        // Assert
        Assert.Empty(transactions);
    }

    [Fact]
    public void RemoveTransactionsAsync_WithNullHashes_ThrowsArgumentNullException()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => mempool.RemoveTransactions(null!));
    }

    [Fact]
    public void RemoveTransactionsAsync_RemovesExistingTransactions()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);
        var tx1 = CreateValidTransaction(fee: 10);
        var tx2 = CreateValidTransaction(fee: 20);
        var tx3 = CreateValidTransaction(fee: 30);

        mempool.AddTransaction(tx1);
        mempool.AddTransaction(tx2);
        mempool.AddTransaction(tx3);

        var hashesToRemove = new[] { tx1.ComputeHash(), tx3.ComputeHash() };

        // Act
        var removedCount = mempool.RemoveTransactions(hashesToRemove);

        // Assert
        Assert.Equal(2, removedCount);
        Assert.Equal(1, mempool.Count);
        
        var remaining = mempool.GetPendingTransactions(10);
        Assert.Single(remaining);
        Assert.Equal(20, remaining[0].Fee);
    }

    [Fact]
    public void RemoveTransactionsAsync_WithNonExistentHash_ReturnsZero()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);
        var tx = CreateValidTransaction(fee: 10);
        mempool.AddTransaction(tx);

        var nonExistentHash = RandomNumberGenerator.GetBytes(32);

        // Act
        var removedCount = mempool.RemoveTransactions(new[] { nonExistentHash });

        // Assert
        Assert.Equal(0, removedCount);
        Assert.Equal(1, mempool.Count);
    }

    [Fact]
    public void ContainsTransactionAsync_WithNullHash_ThrowsArgumentNullException()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => mempool.ContainsTransaction(null!));
    }

    [Fact]
    public void ContainsTransactionAsync_WithExistingTransaction_ReturnsTrue()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);
        var tx = CreateValidTransaction(fee: 10);
        mempool.AddTransaction(tx);

        // Act
        var contains = mempool.ContainsTransaction(tx.ComputeHash());

        // Assert
        Assert.True(contains);
    }

    [Fact]
    public void ContainsTransactionAsync_WithNonExistentTransaction_ReturnsFalse()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);
        var hash = RandomNumberGenerator.GetBytes(32);

        // Act
        var contains = mempool.ContainsTransaction(hash);

        // Assert
        Assert.False(contains);
    }

    [Fact]
    public void ClearAsync_RemovesAllTransactions()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);
        mempool.AddTransaction(CreateValidTransaction(fee: 10));
        mempool.AddTransaction(CreateValidTransaction(fee: 20));
        mempool.AddTransaction(CreateValidTransaction(fee: 30));

        // Act
        mempool.Clear();

        // Assert
        Assert.Equal(0, mempool.Count);
        var transactions = mempool.GetPendingTransactions(10);
        Assert.Empty(transactions);
    }

    [Fact]
    public async Task Mempool_ThreadSafety_ConcurrentOperations()
    {
        // Arrange
        var config = new MempoolConfig { MaxTransactions = 1000 };
        var mempool = new Mempool(_validator, config);
        const int taskCount = 10;
        const int transactionsPerTask = 100;

        // Act - Concurrent adds
        var tasks = Enumerable.Range(0, taskCount).Select(async taskId =>
        {
            for (int i = 0; i < transactionsPerTask; i++)
            {
                var tx = CreateValidTransaction(fee: taskId * 100 + i);
                mempool.AddTransaction(tx);
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        Assert.True(mempool.Count <= config.MaxTransactions);
        Assert.True(mempool.Count > 0);

        // Verify transactions are properly ordered
        var allTxs = mempool.GetPendingTransactions(mempool.Count);
        for (int i = 0; i < allTxs.Count - 1; i++)
        {
            Assert.True(allTxs[i].Fee >= allTxs[i + 1].Fee);
        }
    }

    [Fact]
    public void GetPendingTransactionsAsync_RespectsMaxTransactionsPerBlock()
    {
        // Arrange
        var config = new MempoolConfig
        {
            MaxTransactions = 100,
            MaxTransactionsPerBlock = 10
        };
        var mempool = new Mempool(_validator, config);

        // Add more transactions than MaxTransactionsPerBlock
        for (int i = 0; i < 50; i++)
        {
            mempool.AddTransaction(CreateValidTransaction(fee: i));
        }

        // Act - Request more than MaxTransactionsPerBlock
        var transactions = mempool.GetPendingTransactions(1000);

        // Assert - Should be limited by MaxTransactionsPerBlock
        Assert.Equal(10, transactions.Count);
    }

    [Fact]
    public void AddTransactionAsync_WithEqualFees_MaintainsConsistentOrdering()
    {
        // Arrange
        var mempool = new Mempool(_validator, _config);
        var tx1 = CreateValidTransaction(fee: 100);
        var tx2 = CreateValidTransaction(fee: 100);
        var tx3 = CreateValidTransaction(fee: 100);

        // Act
        mempool.AddTransaction(tx1);
        mempool.AddTransaction(tx2);
        mempool.AddTransaction(tx3);

        // Assert - All should be added
        Assert.Equal(3, mempool.Count);

        // Get transactions multiple times - order should be consistent
        var result1 = mempool.GetPendingTransactions(10);
        var result2 = mempool.GetPendingTransactions(10);

        Assert.Equal(3, result1.Count);
        Assert.Equal(3, result2.Count);
        
        // Order should be the same
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(result1[i].ComputeHash(), result2[i].ComputeHash());
        }
    }
}
