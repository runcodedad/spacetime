using System.Security.Cryptography;
using MerkleTree.Core;
using MerkleTree.Hashing;

namespace Spacetime.Core;

/// <summary>
/// Builds valid blocks when a miner wins the challenge.
/// </summary>
/// <remarks>
/// The block builder is responsible for:
/// - Collecting transactions from the mempool
/// - Computing the transaction Merkle root
/// - Populating the block header with all required fields
/// - Signing the block with the miner's private key
/// - Validating the block before returning it
/// 
/// <example>
/// Building a block:
/// <code>
/// var builder = new BlockBuilder(mempool, signer, validator);
/// 
/// var block = await builder.BuildBlockAsync(
///     parentHash: previousBlockHash,
///     height: previousHeight + 1,
///     difficulty: currentDifficulty,
///     epoch: currentEpoch,
///     challenge: currentChallenge,
///     proof: winningProof,
///     plotRoot: plotMerkleRoot,
///     proofScore: computedScore,
///     maxTransactions: 1000);
/// 
/// // Block is now signed and validated, ready for broadcast
/// </code>
/// </example>
/// </remarks>
public sealed class BlockBuilder
{
    private readonly IMempool _mempool;
    private readonly IBlockSigner _signer;
    private readonly IBlockValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockBuilder"/> class.
    /// </summary>
    /// <param name="mempool">The mempool for collecting transactions.</param>
    /// <param name="signer">The signer for signing blocks.</param>
    /// <param name="validator">The validator for validating blocks.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public BlockBuilder(IMempool mempool, IBlockSigner signer, IBlockValidator validator)
    {
        ArgumentNullException.ThrowIfNull(mempool);
        ArgumentNullException.ThrowIfNull(signer);
        ArgumentNullException.ThrowIfNull(validator);

        _mempool = mempool;
        _signer = signer;
        _validator = validator;
    }

    /// <summary>
    /// Builds a complete, signed, and validated block.
    /// </summary>
    /// <param name="parentHash">The hash of the parent block.</param>
    /// <param name="height">The height of the new block.</param>
    /// <param name="difficulty">The current difficulty target.</param>
    /// <param name="epoch">The current epoch.</param>
    /// <param name="challenge">The challenge for this epoch.</param>
    /// <param name="proof">The winning PoST proof.</param>
    /// <param name="plotRoot">The Merkle root of the winning miner's plot.</param>
    /// <param name="proofScore">The computed proof score.</param>
    /// <param name="maxTransactions">Maximum number of transactions to include.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A fully constructed, signed, and validated block.</returns>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid values.</exception>
    /// <exception cref="InvalidOperationException">Thrown when block validation fails.</exception>
    public async Task<Block> BuildBlockAsync(
        ReadOnlyMemory<byte> parentHash,
        long height,
        long difficulty,
        long epoch,
        ReadOnlyMemory<byte> challenge,
        BlockProof proof,
        ReadOnlyMemory<byte> plotRoot,
        ReadOnlyMemory<byte> proofScore,
        int maxTransactions = 1000,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proof);

        if (parentHash.Length != 32)
        {
            throw new ArgumentException("Parent hash must be 32 bytes", nameof(parentHash));
        }

        if (height < 0)
        {
            throw new ArgumentException("Height must be non-negative", nameof(height));
        }

        if (difficulty < 0)
        {
            throw new ArgumentException("Difficulty must be non-negative", nameof(difficulty));
        }

        if (epoch < 0)
        {
            throw new ArgumentException("Epoch must be non-negative", nameof(epoch));
        }

        if (challenge.Length != 32)
        {
            throw new ArgumentException("Challenge must be 32 bytes", nameof(challenge));
        }

        if (plotRoot.Length != 32)
        {
            throw new ArgumentException("Plot root must be 32 bytes", nameof(plotRoot));
        }

        if (proofScore.Length != 32)
        {
            throw new ArgumentException("Proof score must be 32 bytes", nameof(proofScore));
        }

        if (maxTransactions <= 0)
        {
            throw new ArgumentException("Max transactions must be positive", nameof(maxTransactions));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Step 1: Collect transactions from mempool
        var transactions = await _mempool.GetPendingTransactionsAsync(maxTransactions, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        // Step 2: Build transaction Merkle tree and compute root
        var txRoot = await ComputeTransactionMerkleRootAsync(transactions, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        // Step 3: Get current timestamp
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Step 4: Get miner's public key
        var minerId = _signer.GetPublicKey();

        // Step 5: Create block header (unsigned)
        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: parentHash.Span,
            height: height,
            timestamp: timestamp,
            difficulty: difficulty,
            epoch: epoch,
            challenge: challenge.Span,
            plotRoot: plotRoot.Span,
            proofScore: proofScore.Span,
            txRoot: txRoot,
            minerId: minerId,
            signature: Array.Empty<byte>());

        cancellationToken.ThrowIfCancellationRequested();

        // Step 6: Sign the block header
        var headerHash = header.ComputeHash();
        var signature = await _signer.SignBlockHeaderAsync(headerHash, cancellationToken);
        header.SetSignature(signature);

        cancellationToken.ThrowIfCancellationRequested();

        // Step 7: Create block body
        var body = new BlockBody(transactions, proof);

        // Step 8: Create complete block
        var block = new Block(header, body);

        cancellationToken.ThrowIfCancellationRequested();

        // Step 9: Validate block before returning
        var isValid = await _validator.ValidateBlockAsync(block, cancellationToken);
        if (!isValid)
        {
            throw new InvalidOperationException("Block failed validation after construction");
        }

        return block;
    }

    /// <summary>
    /// Computes the Merkle root of a list of transactions asynchronously.
    /// </summary>
    /// <param name="transactions">The list of transactions.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The 32-byte Merkle root hash.</returns>
    /// <remarks>
    /// If there are no transactions, returns a zero hash (32 zero bytes).
    /// Uses SHA256 for hashing transaction data.
    /// </remarks>
    private static async Task<byte[]> ComputeTransactionMerkleRootAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        if (transactions.Count == 0)
        {
            // Empty transaction list produces zero hash
            return new byte[32];
        }

        // Build Merkle tree using the MerkleTree library
        var hashFunction = new Sha256HashFunction();
        var merkleTreeStream = new MerkleTreeStream(hashFunction);

        // Convert transactions to async enumerable of hashes
        var leaves = GetTransactionHashesAsync(transactions);
        var metadata = await merkleTreeStream.BuildAsync(leaves, cacheConfig: null, cancellationToken)
            .ConfigureAwait(false);

        return metadata.RootHash;
    }

    /// <summary>
    /// Converts a list of transactions to an async enumerable of transaction hashes.
    /// </summary>
    private static async IAsyncEnumerable<byte[]> GetTransactionHashesAsync(IReadOnlyList<Transaction> transactions)
    {
        foreach (var tx in transactions)
        {
            yield return tx.ComputeHash();
        }
    }
}
