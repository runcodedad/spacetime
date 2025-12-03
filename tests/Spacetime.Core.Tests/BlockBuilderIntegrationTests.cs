using System.Security.Cryptography;
using MerkleTree.Core;
using MerkleTree.Hashing;

namespace Spacetime.Core.Tests;

/// <summary>
/// Integration tests for BlockBuilder demonstrating real-world usage scenarios.
/// </summary>
public class BlockBuilderIntegrationTests
{
    /// <summary>
    /// Simple in-memory mempool for testing.
    /// </summary>
    private class InMemoryMempool : IMempool
    {
        private readonly List<Transaction> _transactions = new();

        public void AddTransaction(Transaction tx)
        {
            _transactions.Add(tx);
        }

        public Task<IReadOnlyList<Transaction>> GetPendingTransactionsAsync(
            int maxCount,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var transactions = _transactions
                .OrderByDescending(tx => tx.Fee) // Prioritize by fee
                .Take(maxCount)
                .ToList();

            return Task.FromResult<IReadOnlyList<Transaction>>(transactions);
        }
    }

    /// <summary>
    /// Simple block signer that creates mock signatures for testing.
    /// In production, this would use real ECDSA signing with secp256k1.
    /// </summary>
    private class MockBlockSigner : IBlockSigner
    {
        private readonly byte[] _publicKey;

        public MockBlockSigner()
        {
            _publicKey = RandomNumberGenerator.GetBytes(33);
        }

        public byte[] GetPublicKey() => _publicKey;

        public Task<byte[]> SignBlockHeaderAsync(
            ReadOnlyMemory<byte> headerHash,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (headerHash.Length != 32)
            {
                throw new ArgumentException("Header hash must be 32 bytes", nameof(headerHash));
            }

            // In production, this would use ECDSA signing
            // For testing, we just return a deterministic "signature"
            var signature = new byte[64];
            Array.Copy(headerHash.ToArray(), 0, signature, 0, 32);
            Array.Copy(_publicKey, 0, signature, 32, 32);
            return Task.FromResult(signature);
        }
    }

    /// <summary>
    /// Simple block validator that performs basic validation checks.
    /// In production, this would verify signatures, proof scores, and consensus rules.
    /// </summary>
    private class BasicBlockValidator : IBlockValidator
    {
        public async Task<bool> ValidateBlockAsync(Block block, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check block header is signed
            if (!block.Header.IsSigned())
            {
                return false;
            }

            // Check all transactions are signed
            foreach (var tx in block.Body.Transactions)
            {
                if (!tx.IsSigned() || !tx.ValidateBasicRules())
                {
                    return false;
                }
            }

            // Basic structure validation
            if (block.Header.Height < 0 || block.Header.Difficulty < 0 || block.Header.Epoch < 0)
            {
                return false;
            }

            // Transaction Merkle root verification
            var computedTxRoot = await ComputeTransactionMerkleRootAsync(block.Body.Transactions)
                .ConfigureAwait(false);
            if (!block.Header.TxRoot.SequenceEqual(computedTxRoot))
            {
                return false;
            }

            return true;
        }

        private static async Task<byte[]> ComputeTransactionMerkleRootAsync(IReadOnlyList<Transaction> transactions)
        {
            if (transactions.Count == 0)
            {
                return new byte[32];
            }

            var hashFunction = new Sha256HashFunction();
            var merkleTreeStream = new MerkleTreeStream(hashFunction);

            var leaves = GetTransactionHashesAsync(transactions);
            var metadata = await merkleTreeStream.BuildAsync(leaves, cacheConfig: null, CancellationToken.None)
                .ConfigureAwait(false);

            return metadata.RootHash;
        }

        private static async IAsyncEnumerable<byte[]> GetTransactionHashesAsync(IReadOnlyList<Transaction> transactions)
        {
            foreach (var tx in transactions)
            {
                yield return tx.ComputeHash();
            }
        }
    }

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

    private static Transaction CreateValidTransaction(long fee = 10)
    {
        var sender = RandomNumberGenerator.GetBytes(33);
        var recipient = RandomNumberGenerator.GetBytes(33);
        return new Transaction(sender, recipient, 1000, 1, fee, RandomNumberGenerator.GetBytes(64));
    }

    [Fact]
    public async Task BuildBlockAsync_WithFullWorkflow_CreatesValidBlock()
    {
        // Arrange
        var mempool = new InMemoryMempool();
        mempool.AddTransaction(CreateValidTransaction(fee: 100));
        mempool.AddTransaction(CreateValidTransaction(fee: 50));
        mempool.AddTransaction(CreateValidTransaction(fee: 75));

        var signer = new MockBlockSigner();
        var validator = new BasicBlockValidator();
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
            maxTransactions: 10);

        // Assert
        Assert.NotNull(block);
        Assert.True(block.Header.IsSigned());
        Assert.Equal(3, block.Body.Transactions.Count);
        Assert.Equal(100, block.Header.Height);
        Assert.Equal(1000, block.Header.Difficulty);
        Assert.Equal(10, block.Header.Epoch);
    }

    [Fact]
    public async Task BuildBlockAsync_PrioritizesHigherFeeTransactions()
    {
        // Arrange
        var mempool = new InMemoryMempool();
        var lowFeeTx = CreateValidTransaction(fee: 10);
        var medFeeTx = CreateValidTransaction(fee: 50);
        var highFeeTx = CreateValidTransaction(fee: 100);

        mempool.AddTransaction(lowFeeTx);
        mempool.AddTransaction(medFeeTx);
        mempool.AddTransaction(highFeeTx);

        var signer = new MockBlockSigner();
        var validator = new BasicBlockValidator();
        var builder = new BlockBuilder(mempool, signer, validator);

        // Act - only allow 2 transactions
        var block = await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 100,
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: CreateValidProof(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32),
            maxTransactions: 2);

        // Assert - should include the two highest fee transactions
        Assert.Equal(2, block.Body.Transactions.Count);
        Assert.All(block.Body.Transactions, tx => Assert.True(tx.Fee >= 50));
    }

    [Fact]
    public async Task BuildBlockAsync_ValidatesTransactionMerkleRoot()
    {
        // Arrange
        var mempool = new InMemoryMempool();
        mempool.AddTransaction(CreateValidTransaction());
        mempool.AddTransaction(CreateValidTransaction());

        var signer = new MockBlockSigner();
        var validator = new BasicBlockValidator();
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

        // Assert - validator should verify tx root matches
        var isValid = await validator.ValidateBlockAsync(block);
        Assert.True(isValid);
    }

    [Fact]
    public async Task BuildBlockAsync_CreatesSerializableBlock()
    {
        // Arrange
        var mempool = new InMemoryMempool();
        mempool.AddTransaction(CreateValidTransaction());

        var signer = new MockBlockSigner();
        var validator = new BasicBlockValidator();
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

        // Assert - block should be serializable
        var serialized = block.Serialize();
        Assert.NotEmpty(serialized);

        var deserialized = Block.Deserialize(serialized);
        Assert.Equal(block.Header.Height, deserialized.Header.Height);
        Assert.Equal(block.Body.Transactions.Count, deserialized.Body.Transactions.Count);
    }

    [Fact]
    public async Task BuildBlockAsync_WithEmptyMempool_CreatesEmptyBlock()
    {
        // Arrange
        var mempool = new InMemoryMempool(); // Empty mempool
        var signer = new MockBlockSigner();
        var validator = new BasicBlockValidator();
        var builder = new BlockBuilder(mempool, signer, validator);

        // Act
        var block = await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 1,
            difficulty: 1000,
            epoch: 1,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: CreateValidProof(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32));

        // Assert
        Assert.NotNull(block);
        Assert.Empty(block.Body.Transactions);
        Assert.True(block.Header.IsSigned());
        Assert.Equal(new byte[32], block.Header.TxRoot.ToArray()); // Zero hash for empty transactions
    }

    [Fact]
    public async Task BuildBlockAsync_PopulatesAllHeaderFields()
    {
        // Arrange
        var mempool = new InMemoryMempool();
        var signer = new MockBlockSigner();
        var validator = new BasicBlockValidator();
        var builder = new BlockBuilder(mempool, signer, validator);

        var parentHash = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var plotRoot = RandomNumberGenerator.GetBytes(32);
        var proofScore = RandomNumberGenerator.GetBytes(32);

        // Act
        var block = await builder.BuildBlockAsync(
            parentHash: parentHash,
            height: 123,
            difficulty: 5000,
            epoch: 25,
            challenge: challenge,
            proof: CreateValidProof(),
            plotRoot: plotRoot,
            proofScore: proofScore);

        // Assert - verify all fields are populated correctly
        Assert.Equal(BlockHeader.CurrentVersion, block.Header.Version);
        Assert.Equal(parentHash, block.Header.ParentHash.ToArray());
        Assert.Equal(123, block.Header.Height);
        Assert.Equal(5000, block.Header.Difficulty);
        Assert.Equal(25, block.Header.Epoch);
        Assert.Equal(challenge, block.Header.Challenge.ToArray());
        Assert.Equal(plotRoot, block.Header.PlotRoot.ToArray());
        Assert.Equal(proofScore, block.Header.ProofScore.ToArray());
        Assert.Equal(signer.GetPublicKey(), block.Header.MinerId.ToArray());
        Assert.True(block.Header.Timestamp > 0);
        Assert.True(block.Header.IsSigned());
    }

    [Fact]
    public async Task BuildBlockAsync_ComputesCorrectBlockHash()
    {
        // Arrange
        var mempool = new InMemoryMempool();
        var signer = new MockBlockSigner();
        var validator = new BasicBlockValidator();
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
        var blockHash = block.ComputeHash();
        Assert.Equal(32, blockHash.Length);
        Assert.NotEqual(new byte[32], blockHash); // Should not be zero hash

        // Hash should be consistent
        var blockHash2 = block.ComputeHash();
        Assert.Equal(blockHash, blockHash2);
    }

    [Fact]
    public async Task BuildBlockAsync_MultipleBlocks_ProducesDifferentHashes()
    {
        // Arrange
        var mempool = new InMemoryMempool();
        var signer = new MockBlockSigner();
        var validator = new BasicBlockValidator();
        var builder = new BlockBuilder(mempool, signer, validator);

        // Act - build two blocks
        var block1 = await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 100,
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: CreateValidProof(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32));

        await Task.Delay(10); // Ensure different timestamp

        var block2 = await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 101,
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: CreateValidProof(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32));

        // Assert
        var hash1 = block1.ComputeHash();
        var hash2 = block2.ComputeHash();
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public async Task BuildBlockAsync_IncludesProofInBlockBody()
    {
        // Arrange
        var mempool = new InMemoryMempool();
        var signer = new MockBlockSigner();
        var validator = new BasicBlockValidator();
        var builder = new BlockBuilder(mempool, signer, validator);

        var proof = CreateValidProof();

        // Act
        var block = await builder.BuildBlockAsync(
            parentHash: RandomNumberGenerator.GetBytes(32),
            height: 100,
            difficulty: 1000,
            epoch: 10,
            challenge: RandomNumberGenerator.GetBytes(32),
            proof: proof,
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32));

        // Assert
        Assert.NotNull(block.Body.Proof);
        Assert.Equal(proof.LeafIndex, block.Body.Proof.LeafIndex);
        Assert.Equal(proof.LeafValue.ToArray(), block.Body.Proof.LeafValue.ToArray());
    }
}
