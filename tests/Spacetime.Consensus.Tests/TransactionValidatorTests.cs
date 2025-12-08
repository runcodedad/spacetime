using System.Security.Cryptography;
using NSubstitute;
using Spacetime.Core;
using Spacetime.Storage;

namespace Spacetime.Consensus.Tests;

/// <summary>
/// Unit tests for the TransactionValidator class.
/// </summary>
public class TransactionValidatorTests
{
    private readonly ISignatureVerifier _signatureVerifier;
    private readonly IAccountStorage _accountStorage;
    private readonly ITransactionIndex _transactionIndex;
    private readonly TransactionValidator _validator;

    public TransactionValidatorTests()
    {
        _signatureVerifier = Substitute.For<ISignatureVerifier>();
        _accountStorage = Substitute.For<IAccountStorage>();
        _transactionIndex = Substitute.For<ITransactionIndex>();
        
        var config = TransactionValidationConfig.Default;
        _validator = new TransactionValidator(
            _signatureVerifier,
            _accountStorage,
            _transactionIndex,
            config);
        
        // Setup default returns to avoid arg spec issues
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>()).Returns((AccountState?)null);
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>()).Returns((TransactionLocation?)null);
    }

    private static byte[] CreateValidPublicKey() => RandomNumberGenerator.GetBytes(33);

    private static Transaction CreateValidTransaction(bool signed = true)
    {
        return new Transaction(
            sender: CreateValidPublicKey(),
            recipient: CreateValidPublicKey(),
            amount: 1000,
            nonce: 0,
            fee: 10,
            signature: signed ? RandomNumberGenerator.GetBytes(64) : Array.Empty<byte>());
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSignatureVerifier_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TransactionValidator(null!, _accountStorage, _transactionIndex, TransactionValidationConfig.Default));
    }

    [Fact]
    public void Constructor_WithNullAccountStorage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TransactionValidator(_signatureVerifier, null!, _transactionIndex, TransactionValidationConfig.Default));
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TransactionValidator(_signatureVerifier, _accountStorage, _transactionIndex, null!));
    }

    [Fact]
    public void Constructor_WithNullTransactionIndex_Succeeds()
    {
        // Act
        var validator = new TransactionValidator(
            _signatureVerifier,
            _accountStorage,
            null,
            TransactionValidationConfig.Default);

        // Assert
        Assert.NotNull(validator);
    }

    #endregion

    #region ValidateTransactionAsync - Success Tests

    [Fact]
    public async Task ValidateTransactionAsync_WithValidTransaction_ReturnsSuccess()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 10, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 2000, Nonce: 0));
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
    }

    #endregion

    #region ValidateTransactionAsync - Basic Validation Tests

    [Fact]
    public async Task ValidateTransactionAsync_WithUnsignedTransaction_ReturnsFailure()
    {
        // Arrange
        var tx = CreateValidTransaction(signed: false);

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(TransactionValidationErrorType.BasicValidationFailed, result.Error?.Type);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithSameSenderAndRecipient_ReturnsFailure()
    {
        // Arrange
        var sameKey = CreateValidPublicKey();
        var tx = new Transaction(sameKey, sameKey, 1000, 0, 10, RandomNumberGenerator.GetBytes(64));

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(TransactionValidationErrorType.BasicValidationFailed, result.Error?.Type);
    }

    #endregion

    #region ValidateTransactionAsync - Version Tests

    [Fact]
    public async Task ValidateTransactionAsync_WithUnsupportedVersion_ReturnsFailure()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(
            version: 99, // Unsupported version
            sender: sender,
            recipient: recipient,
            amount: 1000,
            nonce: 0,
            fee: 10,
            signature: RandomNumberGenerator.GetBytes(64));

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(TransactionValidationErrorType.UnsupportedVersion, result.Error?.Type);
    }

    #endregion

    #region ValidateTransactionAsync - Fee Tests

    [Fact]
    public async Task ValidateTransactionAsync_WithFeeBelowMinimum_ReturnsFailure()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 0, RandomNumberGenerator.GetBytes(64));

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(TransactionValidationErrorType.FeeTooLow, result.Error?.Type);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithFeeAtMinimum_Succeeds()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 1, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 2000, Nonce: 0));
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithFeeAboveMaximum_ReturnsFailure()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 1_000_001, RandomNumberGenerator.GetBytes(64));

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(TransactionValidationErrorType.FeeTooHigh, result.Error?.Type);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithFeeAtMaximum_Succeeds()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 1_000_000, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 1_002_000, Nonce: 0));
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateTransactionAsync - Signature Tests

    [Fact]
    public async Task ValidateTransactionAsync_WithInvalidSignature_ReturnsFailure()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 10, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(false); // Invalid signature

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(TransactionValidationErrorType.InvalidSignature, result.Error?.Type);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithValidSignature_PassesSignatureCheck()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 10, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 2000, Nonce: 0));
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.True(result.IsValid);
        _signatureVerifier.Received(1).VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>());
    }

    [Fact]
    public async Task ValidateTransactionAsync_WhenSignatureVerificationThrows_ReturnsFailure()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 10, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.When(x => x.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>()))
            .Do(x => throw new InvalidOperationException("Verification failed"));

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(TransactionValidationErrorType.InvalidSignature, result.Error?.Type);
        Assert.Contains("threw exception", result.ErrorMessage);
    }

    #endregion

    #region ValidateTransactionAsync - Duplicate Tests

    [Fact]
    public async Task ValidateTransactionAsync_WithDuplicateTransaction_ReturnsFailure()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 10, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new TransactionLocation(
                BlockHash: RandomNumberGenerator.GetBytes(32),
                BlockHeight: 100,
                TransactionIndex: 0));

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(TransactionValidationErrorType.DuplicateTransaction, result.Error?.Type);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithNonDuplicateTransaction_PassesDuplicateCheck()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 10, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 2000, Nonce: 0));
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WhenTransactionIndexThrows_ContinuesValidation()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 10, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 2000, Nonce: 0));
        _transactionIndex.When(x => x.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>()))
            .Do(x => throw new InvalidOperationException("Index unavailable"));

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert - Should continue and succeed despite index error
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateTransactionAsync - Account State Tests

    [Fact]
    public async Task ValidateTransactionAsync_WithInvalidNonce_ReturnsFailure()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 5, 10, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 2000, Nonce: 0)); // Nonce mismatch
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(TransactionValidationErrorType.InvalidNonce, result.Error?.Type);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithCorrectNonce_PassesNonceCheck()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 5, 10, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 2000, Nonce: 5)); // Matching nonce
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithInsufficientBalance_ReturnsFailure()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 10, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 500, Nonce: 0)); // Insufficient balance
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(TransactionValidationErrorType.InsufficientBalance, result.Error?.Type);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithExactBalance_Succeeds()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 10, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 1010, Nonce: 0)); // Exact amount + fee
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithNewAccount_UsesZeroBalanceAndNonce()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 10, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((AccountState?)null); // Account doesn't exist
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act
        var result = await _validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.False(result.IsValid); // Should fail due to insufficient balance (0)
        Assert.Equal(TransactionValidationErrorType.InsufficientBalance, result.Error?.Type);
    }

    #endregion

    #region ValidateTransactionInBlockAsync Tests

    [Fact]
    public async Task ValidateTransactionInBlockAsync_WithValidTransaction_ReturnsSuccess()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 10, RandomNumberGenerator.GetBytes(64));
        var context = new BlockValidationContext();

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 2000, Nonce: 0));
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act
        var result = await _validator.ValidateTransactionInBlockAsync(tx, context);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateTransactionInBlockAsync_UpdatesBlockContext()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 10, RandomNumberGenerator.GetBytes(64));
        var context = new BlockValidationContext();

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 2000, Nonce: 0));
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act
        var result = await _validator.ValidateTransactionInBlockAsync(tx, context);

        // Assert
        Assert.True(result.IsValid);
        var trackedState = context.GetTrackedAccountState(tx.Sender);
        Assert.NotNull(trackedState);
        Assert.Equal(990, trackedState.Value.balance); // 2000 - 1000 - 10
        Assert.Equal(1, trackedState.Value.nonce); // 0 + 1
    }

    [Fact]
    public async Task ValidateTransactionInBlockAsync_UsesTrackedStateFromContext()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx1 = new Transaction(sender, recipient, 500, 0, 10, RandomNumberGenerator.GetBytes(64));
        var tx2 = new Transaction(sender, recipient, 400, 1, 10, RandomNumberGenerator.GetBytes(64));
        var context = new BlockValidationContext();

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 2000, Nonce: 0));
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act - Process first transaction
        var result1 = await _validator.ValidateTransactionInBlockAsync(tx1, context);
        
        // Act - Process second transaction with tracked state
        var result2 = await _validator.ValidateTransactionInBlockAsync(tx2, context);

        // Assert
        Assert.True(result1.IsValid);
        Assert.True(result2.IsValid);
        
        var trackedState = context.GetTrackedAccountState(sender);
        Assert.NotNull(trackedState);
        Assert.Equal(1080, trackedState.Value.balance); // 2000 - 500 - 10 - 400 - 10
        Assert.Equal(2, trackedState.Value.nonce); // 0 + 1 + 1
    }

    [Fact]
    public async Task ValidateTransactionInBlockAsync_DetectsDoubleSpendInBlock()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx1 = new Transaction(sender, recipient, 1000, 0, 10, RandomNumberGenerator.GetBytes(64));
        var tx2 = new Transaction(sender, recipient, 1000, 1, 10, RandomNumberGenerator.GetBytes(64));
        var context = new BlockValidationContext();

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 1500, Nonce: 0));
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act - Process first transaction
        var result1 = await _validator.ValidateTransactionInBlockAsync(tx1, context);
        
        // Act - Process second transaction (should fail due to insufficient balance)
        var result2 = await _validator.ValidateTransactionInBlockAsync(tx2, context);

        // Assert
        Assert.True(result1.IsValid);
        Assert.False(result2.IsValid);
        Assert.Equal(TransactionValidationErrorType.InsufficientBalance, result2.Error?.Type);
    }

    [Fact]
    public async Task ValidateTransactionInBlockAsync_WithNullTransaction_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new BlockValidationContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _validator.ValidateTransactionInBlockAsync(null!, context));
    }

    [Fact]
    public async Task ValidateTransactionInBlockAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var tx = CreateValidTransaction();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _validator.ValidateTransactionInBlockAsync(tx, null!));
    }

    #endregion

    #region ValidateTransactionsAsync Tests

    [Fact]
    public async Task ValidateTransactionsAsync_WithEmptyList_ReturnsEmptyResults()
    {
        // Arrange
        var transactions = Array.Empty<Transaction>();

        // Act
        var results = await _validator.ValidateTransactionsAsync(transactions);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ValidateTransactionsAsync_WithValidTransactions_ReturnsAllSuccess()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var transactions = new[]
        {
            new Transaction(sender, recipient, 500, 0, 10, RandomNumberGenerator.GetBytes(64)),
            new Transaction(sender, recipient, 400, 1, 10, RandomNumberGenerator.GetBytes(64))
        };

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 2000, Nonce: 0));
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act
        var results = await _validator.ValidateTransactionsAsync(transactions);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.IsValid));
    }

    [Fact]
    public async Task ValidateTransactionsAsync_StopsOnFirstFailure()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var transactions = new[]
        {
            new Transaction(sender, recipient, 500, 0, 10, RandomNumberGenerator.GetBytes(64)),
            new Transaction(sender, recipient, 400, 0, 10, RandomNumberGenerator.GetBytes(64)), // Wrong nonce
            new Transaction(sender, recipient, 300, 2, 10, RandomNumberGenerator.GetBytes(64))
        };

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 2000, Nonce: 0));
        _transactionIndex.GetTransactionLocation(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns((TransactionLocation?)null);

        // Act
        var results = await _validator.ValidateTransactionsAsync(transactions);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.True(results[0].IsValid);
        Assert.False(results[1].IsValid);
        Assert.False(results[2].IsValid); // Should have failure due to stopping
    }

    [Fact]
    public async Task ValidateTransactionsAsync_WithTooManyTransactions_ReturnsFailure()
    {
        // Arrange
        var config = new TransactionValidationConfig { MaxTransactionsPerBlock = 2 };
        var validator = new TransactionValidator(
            _signatureVerifier,
            _accountStorage,
            _transactionIndex,
            config);

        var transactions = new[]
        {
            CreateValidTransaction(),
            CreateValidTransaction(),
            CreateValidTransaction() // Exceeds limit
        };

        // Act
        var results = await validator.ValidateTransactionsAsync(transactions);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.False(r.IsValid));
    }

    [Fact]
    public async Task ValidateTransactionsAsync_WithNullTransactions_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _validator.ValidateTransactionsAsync(null!));
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task ValidateTransactionAsync_WithPermissiveConfig_AllowsZeroFee()
    {
        // Arrange
        var config = TransactionValidationConfig.Permissive;
        var validator = new TransactionValidator(
            _signatureVerifier,
            _accountStorage,
            _transactionIndex,
            config);

        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 0, RandomNumberGenerator.GetBytes(64));

        _signatureVerifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        _accountStorage.GetAccount(Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(new AccountState(Balance: 2000, Nonce: 0));

        // Act
        var result = await validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithCustomMinFee_EnforcesMinimum()
    {
        // Arrange
        var config = new TransactionValidationConfig { MinimumFee = 100 };
        var validator = new TransactionValidator(
            _signatureVerifier,
            _accountStorage,
            _transactionIndex,
            config);

        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx = new Transaction(sender, recipient, 1000, 0, 50, RandomNumberGenerator.GetBytes(64));

        // Act
        var result = await validator.ValidateTransactionAsync(tx);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(TransactionValidationErrorType.FeeTooLow, result.Error?.Type);
    }

    #endregion
}
