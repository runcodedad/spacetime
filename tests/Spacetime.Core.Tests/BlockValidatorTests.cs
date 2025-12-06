using NSubstitute;
using System.Security.Cryptography;
using Spacetime.Consensus;

namespace Spacetime.Core.Tests;

public class BlockValidatorTests
{
    private readonly ISignatureVerifier _signatureVerifier;
    private readonly ProofValidator _proofValidator;
    private readonly IChainState _chainState;
    private readonly BlockValidator _validator;

    public BlockValidatorTests()
    {
        _signatureVerifier = Substitute.For<ISignatureVerifier>();
        _proofValidator = new ProofValidator(new MerkleTree.Hashing.Sha256HashFunction());
        _chainState = Substitute.For<IChainState>();
        var hashFunction = new MerkleTree.Hashing.Sha256HashFunction();
        _validator = new BlockValidator(_signatureVerifier, _proofValidator, _chainState, hashFunction);

        // Setup default chain state
        _chainState.GetChainTipHashAsync(Arg.Any<CancellationToken>())
            .Returns(RandomNumberGenerator.GetBytes(32));
        _chainState.GetChainTipHeightAsync(Arg.Any<CancellationToken>())
            .Returns(0L);
        _chainState.GetExpectedDifficultyAsync(Arg.Any<CancellationToken>())
            .Returns(1000L);
        _chainState.GetExpectedEpochAsync(Arg.Any<CancellationToken>())
            .Returns(1L);
        _chainState.GetExpectedChallengeAsync(Arg.Any<CancellationToken>())
            .Returns(RandomNumberGenerator.GetBytes(32));

        // Setup default signature verification to succeed
        _signatureVerifier.VerifySignature(
            Arg.Any<byte[]>(),
            Arg.Any<byte[]>(),
            Arg.Any<byte[]>())
            .ReturnsForAnyArgs(true);
    }

    [Fact]
    public async Task ValidateBlockAsync_WithNullBlock_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _validator.ValidateBlockAsync(null!));
    }

    [Fact]
    public async Task ValidateBlockAsync_WithUnsupportedVersion_ReturnsFalse()
    {
        // Arrange
        var block = CreateValidBlock();
        var invalidHeader = new BlockHeader(
            version: 99, // Unsupported version
            block.Header.ParentHash,
            block.Header.Height,
            block.Header.Timestamp,
            block.Header.Difficulty,
            block.Header.Epoch,
            block.Header.Challenge,
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            block.Header.Signature);
        var invalidBlock = new Block(invalidHeader, block.Body);

        // Act
        var result = await _validator.ValidateBlockAsync(invalidBlock);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.UnsupportedVersion, result.Errors[0].ErrorType);
    }



    [Fact]
    public async Task ValidateBlockAsync_WithUnsignedHeader_ReturnsFalse()
    {
        // Arrange
        var block = CreateValidBlock();
        var unsignedHeader = new BlockHeader(
            block.Header.Version,
            block.Header.ParentHash,
            block.Header.Height,
            block.Header.Timestamp,
            block.Header.Difficulty,
            block.Header.Epoch,
            block.Header.Challenge,
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            Array.Empty<byte>()); // No signature
        var unsignedBlock = new Block(unsignedHeader, block.Body);

        // Act
        var result = await _validator.ValidateBlockAsync(unsignedBlock);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.HeaderNotSigned, result.Errors[0].ErrorType);
    }



    [Fact]
    public async Task ValidateBlockAsync_WithTimestampTooFarInFuture_ReturnsFalse()
    {
        // Arrange
        var block = CreateValidBlock();
        var futureTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 300; // 5 minutes in future
        var invalidHeader = new BlockHeader(
            block.Header.Version,
            block.Header.ParentHash,
            block.Header.Height,
            timestamp: futureTimestamp,
            block.Header.Difficulty,
            block.Header.Epoch,
            block.Header.Challenge,
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            block.Header.Signature);
        var invalidBlock = new Block(invalidHeader, block.Body);

        // Act
        var result = await _validator.ValidateBlockAsync(invalidBlock);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.InvalidTimestamp, result.Errors[0].ErrorType);
    }



    [Fact]
    public async Task ValidateBlockAsync_WithInvalidSignature_ReturnsFalse()
    {
        // Arrange
        var block = CreateValidBlock();
        _signatureVerifier.VerifySignature(
            Arg.Any<byte[]>(),
            Arg.Any<byte[]>(),
            Arg.Any<byte[]>())
            .Returns(false);

        // Act
        var result = await _validator.ValidateBlockAsync(block);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.InvalidHeaderSignature, result.Errors[0].ErrorType);
    }

    [Fact]
    public async Task ValidateBlockAsync_WithWrongParentHash_ReturnsFalse()
    {
        // Arrange
        var expectedParentHash = RandomNumberGenerator.GetBytes(32);
        _chainState.GetChainTipHashAsync(Arg.Any<CancellationToken>())
            .Returns(expectedParentHash);

        var block = CreateValidBlock();
        var wrongParentHash = RandomNumberGenerator.GetBytes(32);
        var invalidHeader = new BlockHeader(
            block.Header.Version,
            wrongParentHash, // Wrong parent hash
            block.Header.Height,
            block.Header.Timestamp,
            block.Header.Difficulty,
            block.Header.Epoch,
            block.Header.Challenge,
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            block.Header.Signature);
        var invalidBlock = new Block(invalidHeader, block.Body);

        // Act
        var result = await _validator.ValidateBlockAsync(invalidBlock);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.InvalidParentHash, result.Errors[0].ErrorType);
    }

    [Fact]
    public async Task ValidateBlockAsync_WithWrongHeight_ReturnsFalse()
    {
        // Arrange
        _chainState.GetChainTipHeightAsync(Arg.Any<CancellationToken>())
            .Returns(5L);

        var block = CreateValidBlock();
        var invalidHeader = new BlockHeader(
            block.Header.Version,
            block.Header.ParentHash,
            height: 10, // Wrong height (expected 6)
            block.Header.Timestamp,
            block.Header.Difficulty,
            block.Header.Epoch,
            block.Header.Challenge,
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            block.Header.Signature);
        var invalidBlock = new Block(invalidHeader, block.Body);

        // Act
        var result = await _validator.ValidateBlockAsync(invalidBlock);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.InvalidHeight, result.Errors[0].ErrorType);
    }

    [Fact]
    public async Task ValidateBlockAsync_WithWrongDifficulty_ReturnsFalse()
    {
        // Arrange
        _chainState.GetExpectedDifficultyAsync(Arg.Any<CancellationToken>())
            .Returns(5000L);

        var block = CreateValidBlock();
        var invalidHeader = new BlockHeader(
            block.Header.Version,
            block.Header.ParentHash,
            block.Header.Height,
            block.Header.Timestamp,
            difficulty: 1000, // Wrong difficulty
            block.Header.Epoch,
            block.Header.Challenge,
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            block.Header.Signature);
        var invalidBlock = new Block(invalidHeader, block.Body);

        // Act
        var result = await _validator.ValidateBlockAsync(invalidBlock);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.InvalidDifficulty, result.Errors[0].ErrorType);
    }

    [Fact]
    public async Task ValidateBlockAsync_WithWrongEpoch_ReturnsFalse()
    {
        // Arrange
        _chainState.GetExpectedEpochAsync(Arg.Any<CancellationToken>())
            .Returns(5L);

        var block = CreateValidBlock();
        var invalidHeader = new BlockHeader(
            block.Header.Version,
            block.Header.ParentHash,
            block.Header.Height,
            block.Header.Timestamp,
            block.Header.Difficulty,
            epoch: 1, // Wrong epoch
            block.Header.Challenge,
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            block.Header.Signature);
        var invalidBlock = new Block(invalidHeader, block.Body);

        // Act
        var result = await _validator.ValidateBlockAsync(invalidBlock);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.InvalidEpoch, result.Errors[0].ErrorType);
    }

    [Fact]
    public async Task ValidateBlockAsync_WithWrongChallenge_ReturnsFalse()
    {
        // Arrange
        var expectedChallenge = RandomNumberGenerator.GetBytes(32);
        _chainState.GetExpectedChallengeAsync(Arg.Any<CancellationToken>())
            .Returns(expectedChallenge);

        var block = CreateValidBlock();
        var wrongChallenge = RandomNumberGenerator.GetBytes(32);
        var invalidHeader = new BlockHeader(
            block.Header.Version,
            block.Header.ParentHash,
            block.Header.Height,
            block.Header.Timestamp,
            block.Header.Difficulty,
            block.Header.Epoch,
            wrongChallenge, // Wrong challenge
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            block.Header.Signature);
        var invalidBlock = new Block(invalidHeader, block.Body);

        // Act
        var result = await _validator.ValidateBlockAsync(invalidBlock);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.InvalidChallenge, result.Errors[0].ErrorType);
    }

    [Fact]
    public async Task ValidateBlockAsync_WithInvalidTransactionSignature_ReturnsFalse()
    {
        // Arrange
        var tx = CreateValidTransaction();
        var block = CreateValidBlockWithTransactions(new[] { tx });

        // Make transaction signature verification fail for this specific transaction
        _signatureVerifier.VerifySignature(
            Arg.Any<byte[]>(),
            Arg.Any<byte[]>(),
            Arg.Any<byte[]>())
            .Returns(callInfo =>
            {
                var sig = callInfo.ArgAt<byte[]>(1);
                var pk = callInfo.ArgAt<byte[]>(2);
                // Return false if it's the transaction signature
                if (sig.AsSpan().SequenceEqual(tx.Signature) && pk.AsSpan().SequenceEqual(tx.Sender))
                {
                    return false;
                }
                // Return true for block signature
                return true;
            });

        // Act
        var result = await _validator.ValidateBlockAsync(block);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.InvalidTransactionSignature, result.Errors[0].ErrorType);
    }

    // Helper methods

    private Block CreateValidBlock()
    {
        var chainTipHash = _chainState.GetChainTipHashAsync().GetAwaiter().GetResult() ?? RandomNumberGenerator.GetBytes(32);
        var chainTipHeight = _chainState.GetChainTipHeightAsync().GetAwaiter().GetResult();
        var difficulty = _chainState.GetExpectedDifficultyAsync().GetAwaiter().GetResult();
        var epoch = _chainState.GetExpectedEpochAsync().GetAwaiter().GetResult();
        var challenge = _chainState.GetExpectedChallengeAsync().GetAwaiter().GetResult() ?? RandomNumberGenerator.GetBytes(32);

        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: chainTipHash,
            height: chainTipHeight + 1,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            difficulty: difficulty,
            epoch: epoch,
            challenge: challenge,
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32),
            txRoot: new byte[32], // Empty transaction list
            minerId: RandomNumberGenerator.GetBytes(33),
            signature: RandomNumberGenerator.GetBytes(64));

        var proof = CreateValidBlockProof();
        var body = new BlockBody(Array.Empty<Transaction>(), proof);

        return new Block(header, body);
    }

    private Block CreateValidBlockWithTransactions(IReadOnlyList<Transaction> transactions)
    {
        var chainTipHash = _chainState.GetChainTipHashAsync().GetAwaiter().GetResult() ?? RandomNumberGenerator.GetBytes(32);
        var chainTipHeight = _chainState.GetChainTipHeightAsync().GetAwaiter().GetResult();
        var difficulty = _chainState.GetExpectedDifficultyAsync().GetAwaiter().GetResult();
        var epoch = _chainState.GetExpectedEpochAsync().GetAwaiter().GetResult();
        var challenge = _chainState.GetExpectedChallengeAsync().GetAwaiter().GetResult() ?? RandomNumberGenerator.GetBytes(32);

        // Compute transaction root
        var hashFunction = new MerkleTree.Hashing.Sha256HashFunction();
        var txHashes = transactions.Select(tx => tx.ComputeHash()).ToList();
        var merkleTreeStream = new MerkleTree.Core.MerkleTreeStream(hashFunction);
        var leaves = ToAsyncEnumerable(txHashes);
        var metadata = merkleTreeStream.BuildAsync(leaves, cacheConfig: null, CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: chainTipHash,
            height: chainTipHeight + 1,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            difficulty: difficulty,
            epoch: epoch,
            challenge: challenge,
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32),
            txRoot: metadata.RootHash,
            minerId: RandomNumberGenerator.GetBytes(33),
            signature: RandomNumberGenerator.GetBytes(64));

        var proof = CreateValidBlockProof();
        var body = new BlockBody(transactions, proof);

        return new Block(header, body);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }

    private Transaction CreateValidTransaction()
    {
        return new Transaction(
            sender: RandomNumberGenerator.GetBytes(33),
            recipient: RandomNumberGenerator.GetBytes(33),
            amount: 1000,
            nonce: 1,
            fee: 10,
            signature: RandomNumberGenerator.GetBytes(64));
    }

    private BlockProof CreateValidBlockProof()
    {
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var siblingHashes = new List<byte[]>
        {
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32)
        };
        var orientationBits = new List<bool> { false, true };
        var plotMetadata = BlockPlotMetadata.Create(
            leafCount: 1024,
            plotId: RandomNumberGenerator.GetBytes(32),
            plotHeaderHash: RandomNumberGenerator.GetBytes(32),
            version: 1);

        return new BlockProof(
            leafValue,
            leafIndex: 0,
            siblingHashes,
            orientationBits,
            plotMetadata);
    }
}
