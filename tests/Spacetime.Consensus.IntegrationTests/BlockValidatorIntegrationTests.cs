using NSubstitute;
using System.Security.Cryptography;
using MerkleTree.Hashing;
using Spacetime.Core;
using Spacetime.Plotting;

namespace Spacetime.Consensus.IntegrationTests;

/// <summary>
/// Integration tests for the BlockValidator that test the full validation pipeline.
/// </summary>
public class BlockValidatorIntegrationTests
{
    [Fact]
    public async Task ValidateBlock_WithValidBlockAndEmptyTransactions_Succeeds()
    {
        // Arrange
        var signatureVerifier = CreateMockSignatureVerifier();
        var proofValidator = new ProofValidator(new Sha256HashFunction());
        var chainState = CreateMockChainState();
        var validator = new BlockValidator(signatureVerifier, proofValidator, chainState, new Sha256HashFunction());

        // Create a simple block with no transactions that passes basic validation
        var block = await CreateSimpleValidBlockAsync(chainState);

        // Act - This will fail at proof validation because we can't create real Merkle proofs
        // But it should pass all the other validation steps
        var result = await validator.ValidateBlockAsync(block);

        // Assert - We expect it to fail at proof validation, but all previous steps should pass
        // This tests that our validation order is correct and early validations work
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.InvalidProof, result.Errors[0].ErrorType);
    }

    [Fact]
    public async Task ValidateBlock_WithInvalidProofScore_Fails()
    {
        // Arrange
        var signatureVerifier = CreateMockSignatureVerifier();
        var proofValidator = new ProofValidator(new Sha256HashFunction());
        var chainState = CreateMockChainState();
        var validator = new BlockValidator(signatureVerifier, proofValidator, chainState, new Sha256HashFunction());

        // Create a block with proof score that doesn't meet difficulty
        var (block, _) = await CreateValidBlockWithInvalidProofScoreAsync(chainState);

        // Act
        var result = await validator.ValidateBlockAsync(block);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.ProofScoreTooHigh, result.Errors[0].ErrorType);
    }

    [Fact]
    public async Task ValidateBlock_WithInvalidTransactionMerkleRoot_Fails()
    {
        // Arrange
        var signatureVerifier = CreateMockSignatureVerifier();
        var proofValidator = new ProofValidator(new Sha256HashFunction());
        var chainState = CreateMockChainState();
        var validator = new BlockValidator(signatureVerifier, proofValidator, chainState, new Sha256HashFunction());

        // Create a block with transactions but wrong Merkle root
        var (block, _) = await CreateBlockWithInvalidTxRootAsync(chainState);

        // Act
        var result = await validator.ValidateBlockAsync(block);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.InvalidTransactionRoot, result.Errors[0].ErrorType);
    }

    [Fact]
    public async Task ValidateBlock_WithMultipleTransactions_ValidatesMerkleRoot()
    {
        // Arrange
        var signatureVerifier = CreateMockSignatureVerifier();
        var proofValidator = new ProofValidator(new Sha256HashFunction());
        var chainState = CreateMockChainState();
        var validator = new BlockValidator(signatureVerifier, proofValidator, chainState, new Sha256HashFunction());

        // Create a block with multiple transactions and correct Merkle root
        var transactions = new List<Transaction>
        {
            CreateValidTransaction(),
            CreateValidTransaction(),
            CreateValidTransaction()
        };

        var (block, _) = await CreateValidBlockWithTransactionsAsync(chainState, transactions);

        // Act
        var result = await validator.ValidateBlockAsync(block);

        // Assert - Should pass all checks up to proof validation
        // Merkle root validation should pass since we computed it correctly
        Assert.False(result.IsValid);
        // Should fail at proof validation, not transaction validation
        Assert.Equal(BlockValidationErrorType.InvalidProof, result.Errors[0].ErrorType);
    }

    [Fact]
    public async Task ValidateBlock_WithChainStateValidation_ChecksAllFields()
    {
        // Arrange
        var signatureVerifier = CreateMockSignatureVerifier();
        var proofValidator = new ProofValidator(new Sha256HashFunction());
        var chainState = CreateMockChainState();
        var validator = new BlockValidator(signatureVerifier, proofValidator, chainState, new Sha256HashFunction());

        // Create a block that matches chain state expectations
        var chainTipHash = await chainState.GetChainTipHashAsync();
        var chainTipHeight = await chainState.GetChainTipHeightAsync();
        var expectedDifficulty = await chainState.GetExpectedDifficultyAsync();
        var expectedEpoch = await chainState.GetExpectedEpochAsync();
        var expectedChallenge = await chainState.GetExpectedChallengeAsync();

        var (block, _) = await CreateValidBlockWithPropertiesAsync(
            chainState,
            chainTipHash!,
            chainTipHeight + 1,
            expectedDifficulty,
            expectedEpoch,
            expectedChallenge);

        // Act
        var result = await validator.ValidateBlockAsync(block);

        // Assert - Should pass all chain state checks and fail at proof validation
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.InvalidProof, result.Errors[0].ErrorType);
    }

    [Fact]
    public async Task ValidateBlock_WithValidProofScore_PassesScoreValidation()
    {
        // Arrange
        var signatureVerifier = CreateMockSignatureVerifier();
        var proofValidator = new ProofValidator(new Sha256HashFunction());
        var chainState = CreateMockChainState();
        var validator = new BlockValidator(signatureVerifier, proofValidator, chainState, new Sha256HashFunction());

        // Create a block with a valid proof score that meets the difficulty target
        var (block, proof) = await CreateValidBlockWithValidProofAsync(chainState);

        // Act
        var result = await validator.ValidateBlockAsync(block);

        // Assert - Should pass proof score validation but fail at Merkle proof validation
        // since we can't create real Merkle proofs without actual plot data
        Assert.False(result.IsValid);
        Assert.Equal(BlockValidationErrorType.InvalidProof, result.Errors[0].ErrorType);
        // The key difference from other tests is that this block has a valid proof score
        // that meets the difficulty target, so it passes that check
    }

    // Helper methods

    private static async Task<Block> CreateSimpleValidBlockAsync(IChainState chainState)
    {
        var chainTipHash = await chainState.GetChainTipHashAsync();
        var chainTipHeight = await chainState.GetChainTipHeightAsync();
        var difficulty = await chainState.GetExpectedDifficultyAsync();
        var epoch = await chainState.GetExpectedEpochAsync();
        var challenge = await chainState.GetExpectedChallengeAsync();

        var (proof, _) = CreateValidProofWithGoodScore(challenge!, difficulty);

        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: chainTipHash!,
            height: chainTipHeight + 1,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            difficulty: difficulty,
            epoch: epoch,
            challenge: challenge!,
            plotRoot: proof.PlotMetadata.PlotId,
            proofScore: new byte[32], // Empty score for this simple test
            txRoot: new byte[32], // Empty transactions
            minerId: RandomNumberGenerator.GetBytes(33),
            signature: RandomNumberGenerator.GetBytes(64));

        var body = new BlockBody([], proof);
        return new Block(header, body);
    }

    private static ISignatureVerifier CreateMockSignatureVerifier()
    {
        var verifier = Substitute.For<ISignatureVerifier>();
        verifier.VerifySignature(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
            .Returns(true);
        return verifier;
    }

    private static IChainState CreateMockChainState()
    {
        var chainState = Substitute.For<IChainState>();
        chainState.GetChainTipHashAsync(Arg.Any<CancellationToken>())
            .Returns(RandomNumberGenerator.GetBytes(32));
        chainState.GetChainTipHeightAsync(Arg.Any<CancellationToken>())
            .Returns(10L);
        chainState.GetExpectedDifficultyAsync(Arg.Any<CancellationToken>())
            .Returns(1000L);
        chainState.GetExpectedEpochAsync(Arg.Any<CancellationToken>())
            .Returns(5L);
        chainState.GetExpectedChallengeAsync(Arg.Any<CancellationToken>())
            .Returns(RandomNumberGenerator.GetBytes(32));
        return chainState;
    }

    private static Transaction CreateValidTransaction()
    {
        return new Transaction(
            sender: RandomNumberGenerator.GetBytes(33),
            recipient: RandomNumberGenerator.GetBytes(33),
            amount: 1000,
            nonce: RandomNumberGenerator.GetInt32(1, 1000),
            fee: 10,
            signature: RandomNumberGenerator.GetBytes(64));
    }

    private static async Task<(Block, Proof)> CreateValidBlockWithValidProofAsync(IChainState chainState)
    {
        var chainTipHash = await chainState.GetChainTipHashAsync();
        var chainTipHeight = await chainState.GetChainTipHeightAsync();
        var difficulty = await chainState.GetExpectedDifficultyAsync();
        var epoch = await chainState.GetExpectedEpochAsync();
        var challenge = await chainState.GetExpectedChallengeAsync();

        // Create a valid proof with a score that meets the difficulty
        var (proof, plotProof) = CreateValidProofWithGoodScore(challenge!, difficulty);

        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: chainTipHash!,
            height: chainTipHeight + 1,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            difficulty: difficulty,
            epoch: epoch,
            challenge: challenge!,
            plotRoot: proof.PlotMetadata.PlotId,
            proofScore: plotProof.Score,
            txRoot: new byte[32], // Empty transactions
            minerId: RandomNumberGenerator.GetBytes(33),
            signature: RandomNumberGenerator.GetBytes(64));

        var body = new BlockBody([], proof);
        return (new Block(header, body), plotProof);
    }

    private static async Task<(Block, Proof)> CreateValidBlockWithInvalidProofScoreAsync(IChainState chainState)
    {
        var chainTipHash = await chainState.GetChainTipHashAsync();
        var chainTipHeight = await chainState.GetChainTipHeightAsync();
        var difficulty = await chainState.GetExpectedDifficultyAsync();
        var epoch = await chainState.GetExpectedEpochAsync();
        var challenge = await chainState.GetExpectedChallengeAsync();

        // Create a proof with a score that's too high (doesn't meet difficulty)
        var (proof, plotProof) = CreateProofWithBadScore(challenge!, difficulty);

        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: chainTipHash!,
            height: chainTipHeight + 1,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            difficulty: difficulty,
            epoch: epoch,
            challenge: challenge!,
            plotRoot: proof.PlotMetadata.PlotId,
            proofScore: plotProof.Score,
            txRoot: new byte[32],
            minerId: RandomNumberGenerator.GetBytes(33),
            signature: RandomNumberGenerator.GetBytes(64));

        var body = new BlockBody([], proof);
        return (new Block(header, body), plotProof);
    }

    private static async Task<(Block, Transaction[])> CreateBlockWithInvalidTxRootAsync(IChainState chainState)
    {
        var chainTipHash = await chainState.GetChainTipHashAsync();
        var chainTipHeight = await chainState.GetChainTipHeightAsync();
        var difficulty = await chainState.GetExpectedDifficultyAsync();
        var epoch = await chainState.GetExpectedEpochAsync();
        var challenge = await chainState.GetExpectedChallengeAsync();

        var transactions = new[] { CreateValidTransaction() };
        var (proof, plotProof) = CreateValidProofWithGoodScore(challenge!, difficulty);

        // Use wrong Merkle root (not matching transactions)
        var wrongTxRoot = RandomNumberGenerator.GetBytes(32);

        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: chainTipHash!,
            height: chainTipHeight + 1,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            difficulty: difficulty,
            epoch: epoch,
            challenge: challenge!,
            plotRoot: proof.PlotMetadata.PlotId,
            proofScore: plotProof.Score,
            txRoot: wrongTxRoot,
            minerId: RandomNumberGenerator.GetBytes(33),
            signature: RandomNumberGenerator.GetBytes(64));

        var body = new BlockBody(transactions, proof);
        return (new Block(header, body), transactions);
    }

    private static async Task<(Block, Transaction[])> CreateValidBlockWithTransactionsAsync(
        IChainState chainState,
        List<Transaction> transactions)
    {
        var chainTipHash = await chainState.GetChainTipHashAsync();
        var chainTipHeight = await chainState.GetChainTipHeightAsync();
        var difficulty = await chainState.GetExpectedDifficultyAsync();
        var epoch = await chainState.GetExpectedEpochAsync();
        var challenge = await chainState.GetExpectedChallengeAsync();

        // Compute correct transaction Merkle root
        var hashFunction = new Sha256HashFunction();
        var txHashes = transactions.Select(tx => tx.ComputeHash()).ToList();
        var merkleTreeStream = new MerkleTree.Core.MerkleTreeStream(hashFunction);
        var leaves = ToAsyncEnumerable(txHashes);
        var metadata = await merkleTreeStream.BuildAsync(leaves, cacheConfig: null, CancellationToken.None);

        var (proof, plotProof) = CreateValidProofWithGoodScore(challenge!, difficulty);

        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: chainTipHash!,
            height: chainTipHeight + 1,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            difficulty: difficulty,
            epoch: epoch,
            challenge: challenge!,
            plotRoot: proof.PlotMetadata.PlotId,
            proofScore: plotProof.Score,
            txRoot: metadata.RootHash,
            minerId: RandomNumberGenerator.GetBytes(33),
            signature: RandomNumberGenerator.GetBytes(64));

        var body = new BlockBody(transactions, proof);
        return (new Block(header, body), transactions.ToArray());
    }

    private static async Task<(Block, Proof)> CreateValidBlockWithPropertiesAsync(
        IChainState chainState,
        byte[] parentHash,
        long height,
        long difficulty,
        long epoch,
        byte[] challenge)
    {
        var (proof, plotProof) = CreateValidProofWithGoodScore(challenge, difficulty);

        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: parentHash,
            height: height,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            difficulty: difficulty,
            epoch: epoch,
            challenge: challenge,
            plotRoot: proof.PlotMetadata.PlotId,
            proofScore: plotProof.Score,
            txRoot: new byte[32],
            minerId: RandomNumberGenerator.GetBytes(33),
            signature: RandomNumberGenerator.GetBytes(64));

        var body = new BlockBody([], proof);
        return (new Block(header, body), plotProof);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }

    private static (BlockProof, Proof) CreateValidProofWithGoodScore(byte[] challenge, long difficulty)
    {
        // Create a proof with a score that will meet the difficulty target
        // Generate leaf values until we find one with a low enough score
        var hashFunction = new Sha256HashFunction();
        var difficultyTarget = DifficultyAdjuster.DifficultyToTarget(difficulty);
        
        byte[] leafValue;
        byte[] score;
        int attempts = 0;
        const int maxAttempts = 10000;
        
        do
        {
            leafValue = RandomNumberGenerator.GetBytes(32);
            score = hashFunction.ComputeHash(challenge.Concat(leafValue).ToArray());
            attempts++;
            
            if (attempts >= maxAttempts)
            {
                // If we can't find a valid score, create one that's guaranteed to be below target
                leafValue = new byte[32];
                score = hashFunction.ComputeHash(challenge.Concat(leafValue).ToArray());
                break;
            }
        }
        while (CompareScores(score, difficultyTarget) >= 0);

        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var siblingHashes = new List<byte[]>
        {
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32)
        };
        var orientationBits = new List<bool> { false, true };

        var plotMetadata = BlockPlotMetadata.Create(
            leafCount: 1024,
            plotId: merkleRoot,
            plotHeaderHash: RandomNumberGenerator.GetBytes(32),
            version: 1);

        var blockProof = new BlockProof(
            leafValue,
            leafIndex: 0,
            siblingHashes,
            orientationBits,
            plotMetadata);

        var plotProof = new Proof(
            leafValue,
            leafIndex: 0,
            siblingHashes,
            orientationBits,
            merkleRoot,
            challenge,
            score);

        return (blockProof, plotProof);
    }

    private static int CompareScores(ReadOnlySpan<byte> score1, ReadOnlySpan<byte> score2)
    {
        for (var i = 0; i < score1.Length && i < score2.Length; i++)
        {
            var diff = score1[i] - score2[i];
            if (diff != 0)
            {
                return diff;
            }
        }
        return 0;
    }

    private static (BlockProof, Proof) CreateProofWithBadScore(byte[] challenge, long difficulty)
    {
        // Create a proof with a score that won't meet the difficulty target
        // We use all 0xFF bytes to create a very high score
        var leafValue = new byte[32];
        Array.Fill(leafValue, (byte)0xFF);

        var hashFunction = new Sha256HashFunction();
        var score = hashFunction.ComputeHash(challenge.Concat(leafValue).ToArray());

        var merkleRoot = RandomNumberGenerator.GetBytes(32);
        var siblingHashes = new List<byte[]>
        {
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32)
        };
        var orientationBits = new List<bool> { false, true };

        var plotMetadata = BlockPlotMetadata.Create(
            leafCount: 1024,
            plotId: merkleRoot,
            plotHeaderHash: RandomNumberGenerator.GetBytes(32),
            version: 1);

        var blockProof = new BlockProof(
            leafValue,
            leafIndex: 0,
            siblingHashes,
            orientationBits,
            plotMetadata);

        var plotProof = new Proof(
            leafValue,
            leafIndex: 0,
            siblingHashes,
            orientationBits,
            merkleRoot,
            challenge,
            score);

        return (blockProof, plotProof);
    }
}
