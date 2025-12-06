using System.Security.Cryptography;
using NSubstitute;

namespace Spacetime.Core.Tests;

public class BlockBuilderTests
{
    private static BlockPlotMetadata CreateValidMetadata()
    {
        return BlockPlotMetadata.Create(
            1000,
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32),
            1);
    }

    private static BlockProof CreateValidProof()
    {
        return new BlockProof(
            RandomNumberGenerator.GetBytes(32),
            42,
            new[] { RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32) },
            new[] { true, false },
            CreateValidMetadata());
    }

    private static Transaction CreateValidTransaction()
    {
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);
        return new Transaction(sender, recipient, 1000, 1, 10, RandomNumberGenerator.GetBytes(64));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesBuilder()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();

        // Act
        var builder = new BlockBuilder(mempool, signer, validator);

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void Constructor_WithNullMempool_ThrowsArgumentNullException()
    {
        // Arrange
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BlockBuilder(null!, signer, validator));
    }

    [Fact]
    public void Constructor_WithNullSigner_ThrowsArgumentNullException()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var validator = Substitute.For<IBlockValidator>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BlockBuilder(mempool, null!, validator));
    }

    [Fact]
    public void Constructor_WithNullValidator_ThrowsArgumentNullException()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BlockBuilder(mempool, signer, null!));
    }

    [Fact]
    public async Task BuildBlockAsync_WithValidParameters_ReturnsSignedBlock()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();

        var transactions = new[] { CreateValidTransaction(), CreateValidTransaction() };
        mempool.GetPendingTransactionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(transactions);

        var minerPublicKey = RandomNumberGenerator.GetBytes(33);
        signer.GetPublicKey().Returns(minerPublicKey);

        var signature = RandomNumberGenerator.GetBytes(64);
        signer.SignBlockHeaderAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(signature);

        validator.ValidateBlockAsync(Arg.Any<Block>(), Arg.Any<CancellationToken>())
            .Returns(BlockValidationResult.Success());

        var builder = new BlockBuilder(mempool, signer, validator);

        // Act
        var block = await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 100,
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: CreateValidProof(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32),
            maxTransactions: 1000);

        // Assert
        Assert.NotNull(block);
        Assert.NotNull(block.Header);
        Assert.NotNull(block.Body);
        Assert.True(block.Header.IsSigned());
        Assert.Equal(100, block.Header.Height);
        Assert.Equal(1000, block.Header.Difficulty);
        Assert.Equal(10, block.Header.Epoch);
        Assert.Equal(2, block.Body.Transactions.Count);
    }

    [Fact]
    public async Task BuildBlockAsync_WithEmptyMempool_ReturnsBlockWithZeroTransactions()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();

        mempool.GetPendingTransactionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Transaction>());

        var minerPublicKey = RandomNumberGenerator.GetBytes(33);
        signer.GetPublicKey().Returns(minerPublicKey);

        var signature = RandomNumberGenerator.GetBytes(64);
        signer.SignBlockHeaderAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(signature);

        validator.ValidateBlockAsync(Arg.Any<Block>(), Arg.Any<CancellationToken>())
            .Returns(BlockValidationResult.Success());

        var builder = new BlockBuilder(mempool, signer, validator);

        // Act
        var block = await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 100,
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: CreateValidProof(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32));

        // Assert
        Assert.NotNull(block);
        Assert.Empty(block.Body.Transactions);
        Assert.True(block.Header.IsSigned());
    }

    [Fact]
    public async Task BuildBlockAsync_CallsMempoolWithCorrectMaxTransactions()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();

        mempool.GetPendingTransactionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Transaction>());

        signer.GetPublicKey().Returns(RandomNumberGenerator.GetBytes(33));
        signer.SignBlockHeaderAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(RandomNumberGenerator.GetBytes(64));

        validator.ValidateBlockAsync(Arg.Any<Block>(), Arg.Any<CancellationToken>())
            .Returns(BlockValidationResult.Success());

        var builder = new BlockBuilder(mempool, signer, validator);

        // Act
        await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 100,
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: CreateValidProof(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32),
            maxTransactions: 500);

        // Assert
        await mempool.Received(1).GetPendingTransactionsAsync(500, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BuildBlockAsync_CallsSignerToSignHeader()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();

        mempool.GetPendingTransactionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Transaction>());

        signer.GetPublicKey().Returns(RandomNumberGenerator.GetBytes(33));
        signer.SignBlockHeaderAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(RandomNumberGenerator.GetBytes(64));

        validator.ValidateBlockAsync(Arg.Any<Block>(), Arg.Any<CancellationToken>())
            .Returns(BlockValidationResult.Success());

        var builder = new BlockBuilder(mempool, signer, validator);

        // Act
        await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 100,
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: CreateValidProof(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32));

        // Assert
        await signer.Received(1).SignBlockHeaderAsync(
            Arg.Is<ReadOnlyMemory<byte>>(h => h.Length == 32),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BuildBlockAsync_CallsValidatorToValidateBlock()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();

        mempool.GetPendingTransactionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Transaction>());

        signer.GetPublicKey().Returns(RandomNumberGenerator.GetBytes(33));
        signer.SignBlockHeaderAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(RandomNumberGenerator.GetBytes(64));

        validator.ValidateBlockAsync(Arg.Any<Block>(), Arg.Any<CancellationToken>())
            .Returns(BlockValidationResult.Success());

        var builder = new BlockBuilder(mempool, signer, validator);

        // Act
        await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 100,
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: CreateValidProof(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32));

        // Assert
        await validator.Received(1).ValidateBlockAsync(Arg.Any<Block>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BuildBlockAsync_WhenValidationFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();

        mempool.GetPendingTransactionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Transaction>());

        signer.GetPublicKey().Returns(RandomNumberGenerator.GetBytes(33));
        signer.SignBlockHeaderAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(RandomNumberGenerator.GetBytes(64));

        validator.ValidateBlockAsync(Arg.Any<Block>(), Arg.Any<CancellationToken>())
            .Returns(BlockValidationResult.Failure(new BlockValidationError(BlockValidationErrorType.InvalidProof, "Test failure")));

        var builder = new BlockBuilder(mempool, signer, validator);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            builder.BuildBlockAsync(
                parentHash: RandomNumberGenerator.GetBytes(32),
                height: 100,
                difficulty: 1000,
                epoch: 10,
                challenge: RandomNumberGenerator.GetBytes(32),
                proof: CreateValidProof(),
                plotRoot: RandomNumberGenerator.GetBytes(32),
                proofScore: RandomNumberGenerator.GetBytes(32)));
    }

    [Fact]
    public async Task BuildBlockAsync_WithInvalidParentHashSize_ThrowsArgumentException()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();
        var builder = new BlockBuilder(mempool, signer, validator);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            builder.BuildBlockAsync(
                parentHash: RandomNumberGenerator.GetBytes(16), // Invalid size
                height: 100,
                difficulty: 1000,
                epoch: 10,
                challenge: RandomNumberGenerator.GetBytes(32),
                proof: CreateValidProof(),
                plotRoot: RandomNumberGenerator.GetBytes(32),
                proofScore: RandomNumberGenerator.GetBytes(32)));
    }

    [Fact]
    public async Task BuildBlockAsync_WithNegativeHeight_ThrowsArgumentException()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();
        var builder = new BlockBuilder(mempool, signer, validator);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            builder.BuildBlockAsync(
                parentHash: RandomNumberGenerator.GetBytes(32),
                height: -1, // Invalid
                difficulty: 1000,
                epoch: 10,
                challenge: RandomNumberGenerator.GetBytes(32),
                proof: CreateValidProof(),
                plotRoot: RandomNumberGenerator.GetBytes(32),
                proofScore: RandomNumberGenerator.GetBytes(32)));
    }

    [Fact]
    public async Task BuildBlockAsync_WithNegativeDifficulty_ThrowsArgumentException()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();
        var builder = new BlockBuilder(mempool, signer, validator);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            builder.BuildBlockAsync(
                parentHash: RandomNumberGenerator.GetBytes(32),
                height: 100,
                difficulty: -1, // Invalid
                epoch: 10,
                challenge: RandomNumberGenerator.GetBytes(32),
                proof: CreateValidProof(),
                plotRoot: RandomNumberGenerator.GetBytes(32),
                proofScore: RandomNumberGenerator.GetBytes(32)));
    }

    [Fact]
    public async Task BuildBlockAsync_WithNegativeEpoch_ThrowsArgumentException()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();
        var builder = new BlockBuilder(mempool, signer, validator);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            builder.BuildBlockAsync(
                parentHash: RandomNumberGenerator.GetBytes(32),
                height: 100,
                difficulty: 1000,
                epoch: -1, // Invalid
                challenge: RandomNumberGenerator.GetBytes(32),
                proof: CreateValidProof(),
                plotRoot: RandomNumberGenerator.GetBytes(32),
                proofScore: RandomNumberGenerator.GetBytes(32)));
    }

    [Fact]
    public async Task BuildBlockAsync_WithInvalidChallengeSize_ThrowsArgumentException()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();
        var builder = new BlockBuilder(mempool, signer, validator);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            builder.BuildBlockAsync(
                parentHash: RandomNumberGenerator.GetBytes(32),
                height: 100,
                difficulty: 1000,
                epoch: 10,
                challenge: RandomNumberGenerator.GetBytes(16), // Invalid size
                proof: CreateValidProof(),
                plotRoot: RandomNumberGenerator.GetBytes(32),
                proofScore: RandomNumberGenerator.GetBytes(32)));
    }

    [Fact]
    public async Task BuildBlockAsync_WithNullProof_ThrowsArgumentNullException()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();
        var builder = new BlockBuilder(mempool, signer, validator);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            builder.BuildBlockAsync(
                parentHash: RandomNumberGenerator.GetBytes(32),
                height: 100,
                difficulty: 1000,
                epoch: 10,
                challenge: RandomNumberGenerator.GetBytes(32),
                proof: null!, // Invalid
                plotRoot: RandomNumberGenerator.GetBytes(32),
                proofScore: RandomNumberGenerator.GetBytes(32)));
    }

    [Fact]
    public async Task BuildBlockAsync_WithInvalidPlotRootSize_ThrowsArgumentException()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();
        var builder = new BlockBuilder(mempool, signer, validator);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            builder.BuildBlockAsync(
                parentHash: RandomNumberGenerator.GetBytes(32),
                height: 100,
                difficulty: 1000,
                epoch: 10,
                challenge: RandomNumberGenerator.GetBytes(32),
                proof: CreateValidProof(),
                plotRoot: RandomNumberGenerator.GetBytes(16), // Invalid size
                proofScore: RandomNumberGenerator.GetBytes(32)));
    }

    [Fact]
    public async Task BuildBlockAsync_WithInvalidProofScoreSize_ThrowsArgumentException()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();
        var builder = new BlockBuilder(mempool, signer, validator);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            builder.BuildBlockAsync(
                parentHash: RandomNumberGenerator.GetBytes(32),
                height: 100,
                difficulty: 1000,
                epoch: 10,
                challenge: RandomNumberGenerator.GetBytes(32),
                proof: CreateValidProof(),
                plotRoot: RandomNumberGenerator.GetBytes(32),
                proofScore: RandomNumberGenerator.GetBytes(16))); // Invalid size
    }

    [Fact]
    public async Task BuildBlockAsync_WithZeroMaxTransactions_ThrowsArgumentException()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();
        var builder = new BlockBuilder(mempool, signer, validator);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            builder.BuildBlockAsync(
                parentHash: RandomNumberGenerator.GetBytes(32),
                height: 100,
                difficulty: 1000,
                epoch: 10,
                challenge: RandomNumberGenerator.GetBytes(32),
                proof: CreateValidProof(),
                plotRoot: RandomNumberGenerator.GetBytes(32),
                proofScore: RandomNumberGenerator.GetBytes(32),
                maxTransactions: 0)); // Invalid
    }

    [Fact]
    public async Task BuildBlockAsync_SetsMinerIdFromSigner()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();

        mempool.GetPendingTransactionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Transaction>());

        var expectedMinerPublicKey = RandomNumberGenerator.GetBytes(33);
        signer.GetPublicKey().Returns(expectedMinerPublicKey);
        signer.SignBlockHeaderAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(RandomNumberGenerator.GetBytes(64));

        validator.ValidateBlockAsync(Arg.Any<Block>(), Arg.Any<CancellationToken>())
            .Returns(BlockValidationResult.Success());

        var builder = new BlockBuilder(mempool, signer, validator);

        // Act
        var block = await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 100,
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: CreateValidProof(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32));

        // Assert
        Assert.Equal(expectedMinerPublicKey, block.Header.MinerId.ToArray());
    }

    [Fact]
    public async Task BuildBlockAsync_ComputesTransactionMerkleRoot()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();

        var transactions = new[] { CreateValidTransaction(), CreateValidTransaction() };
        mempool.GetPendingTransactionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(transactions);

        signer.GetPublicKey().Returns(RandomNumberGenerator.GetBytes(33));
        signer.SignBlockHeaderAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(RandomNumberGenerator.GetBytes(64));

        validator.ValidateBlockAsync(Arg.Any<Block>(), Arg.Any<CancellationToken>())
            .Returns(BlockValidationResult.Success());

        var builder = new BlockBuilder(mempool, signer, validator);

        // Act
        var block = await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 100,
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: CreateValidProof(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32));

        // Assert
        Assert.Equal(32, block.Header.TxRoot.Length);
        Assert.NotEqual(new byte[32], block.Header.TxRoot.ToArray()); // Should not be zero hash with transactions
    }

    [Fact]
    public async Task BuildBlockAsync_WithEmptyTransactions_ProducesZeroTxRoot()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();

        mempool.GetPendingTransactionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Transaction>());

        signer.GetPublicKey().Returns(RandomNumberGenerator.GetBytes(33));
        signer.SignBlockHeaderAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(RandomNumberGenerator.GetBytes(64));

        validator.ValidateBlockAsync(Arg.Any<Block>(), Arg.Any<CancellationToken>())
            .Returns(BlockValidationResult.Success());

        var builder = new BlockBuilder(mempool, signer, validator);

        // Act
        var block = await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 100,
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: CreateValidProof(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32));

        // Assert
        Assert.Equal(new byte[32], block.Header.TxRoot.ToArray()); // Zero hash for empty transaction list
    }

    [Fact]
    public async Task BuildBlockAsync_SetsCurrentTimestamp()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();

        mempool.GetPendingTransactionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Transaction>());

        signer.GetPublicKey().Returns(RandomNumberGenerator.GetBytes(33));
        signer.SignBlockHeaderAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(RandomNumberGenerator.GetBytes(64));

        validator.ValidateBlockAsync(Arg.Any<Block>(), Arg.Any<CancellationToken>())
            .Returns(BlockValidationResult.Success());

        var builder = new BlockBuilder(mempool, signer, validator);

        var beforeTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act
        var block = await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 100,
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: CreateValidProof(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32));

        var afterTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Assert
        Assert.InRange(block.Header.Timestamp, beforeTime, afterTime);
    }

    [Fact]
    public async Task BuildBlockAsync_SetsBlockVersion()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();

        mempool.GetPendingTransactionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Transaction>());

        signer.GetPublicKey().Returns(RandomNumberGenerator.GetBytes(33));
        signer.SignBlockHeaderAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(RandomNumberGenerator.GetBytes(64));

        validator.ValidateBlockAsync(Arg.Any<Block>(), Arg.Any<CancellationToken>())
            .Returns(BlockValidationResult.Success());

        var builder = new BlockBuilder(mempool, signer, validator);

        // Act
        var block = await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 100,
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: CreateValidProof(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32));

        // Assert
        Assert.Equal(BlockHeader.CurrentVersion, block.Header.Version);
    }

    [Fact]
    public async Task BuildBlockAsync_RespectsCancellationToken()
    {
        // Arrange
        var mempool = Substitute.For<IMempool>();
        var signer = Substitute.For<IBlockSigner>();
        var validator = Substitute.For<IBlockValidator>();

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var builder = new BlockBuilder(mempool, signer, validator);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            builder.BuildBlockAsync(
                parentHash: RandomNumberGenerator.GetBytes(32),
                height: 100,
                difficulty: 1000,
                epoch: 10,
                challenge: RandomNumberGenerator.GetBytes(32),
                proof: CreateValidProof(),
                plotRoot: RandomNumberGenerator.GetBytes(32),
                proofScore: RandomNumberGenerator.GetBytes(32),
                cancellationToken: cts.Token));
    }
}
