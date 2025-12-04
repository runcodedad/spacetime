using System.Security.Cryptography;
using MerkleTree.Core;
using MerkleTree.Hashing;

namespace Spacetime.Core;

/// <summary>
/// Generates genesis blocks from genesis configurations.
/// </summary>
/// <remarks>
/// The genesis block generator creates the first block in a blockchain network.
/// It uses a genesis signer to sign the block and validates the configuration before generation.
/// 
/// <example>
/// Creating a genesis block:
/// <code>
/// var signer = new GenesisBlockSigner();
/// var generator = new GenesisBlockGenerator(signer);
/// 
/// var config = new GenesisConfig(
///     NetworkId: "testnet-v1",
///     InitialTimestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
///     InitialDifficulty: 1000,
///     InitialEpoch: 0,
///     EpochDurationSeconds: 30,
///     TargetBlockTime: 30,
///     PreminedAllocations: new Dictionary&lt;string, long&gt;());
/// 
/// var genesisBlock = await generator.GenerateGenesisBlockAsync(config);
/// </code>
/// </example>
/// </remarks>
public sealed class GenesisBlockGenerator : IGenesisBlockGenerator
{
    private readonly IBlockSigner _signer;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenesisBlockGenerator"/> class.
    /// </summary>
    /// <param name="signer">The signer for signing the genesis block.</param>
    /// <exception cref="ArgumentNullException">Thrown when signer is null.</exception>
    public GenesisBlockGenerator(IBlockSigner signer)
    {
        ArgumentNullException.ThrowIfNull(signer);
        _signer = signer;
    }

    /// <summary>
    /// Generates a genesis block from the specified configuration.
    /// </summary>
    /// <param name="config">The genesis configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A fully constructed and signed genesis block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when genesis generation fails.</exception>
    public async Task<Block> GenerateGenesisBlockAsync(
        GenesisConfig config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        // Validate configuration
        config.Validate();

        cancellationToken.ThrowIfCancellationRequested();

        // Create premine transactions
        var transactions = await CreatePremineTransactionsAsync(config, _signer, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Compute transaction Merkle root
        var txRoot = await ComputeTransactionMerkleRootAsync(transactions, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Create genesis challenge (derived from network ID)
        var genesisChallenge = ComputeGenesisChallenge(config.NetworkId);

        // Genesis block has parent hash of all zeros
        var parentHash = new byte[32];

        // Genesis plot root (all zeros for genesis)
        var plotRoot = new byte[32];

        // Genesis proof score (all zeros for genesis)
        var proofScore = new byte[32];

        // Get genesis miner's public key
        var minerId = _signer.GetPublicKey();

        // Create block header (unsigned)
        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: parentHash,
            height: 0,
            timestamp: config.InitialTimestamp,
            difficulty: config.InitialDifficulty,
            epoch: config.InitialEpoch,
            challenge: genesisChallenge,
            plotRoot: plotRoot,
            proofScore: proofScore,
            txRoot: txRoot,
            minerId: minerId,
            signature: []);

        cancellationToken.ThrowIfCancellationRequested();

        // Sign the block header
        var headerHash = header.ComputeHash();
        var signature = await _signer.SignBlockHeaderAsync(headerHash, cancellationToken);
        header.SetSignature(signature);

        cancellationToken.ThrowIfCancellationRequested();

        // Create genesis proof (empty for genesis block)
        var plotMetadata = BlockPlotMetadata.Create(
            leafCount: 1,
            plotId: new byte[32],
            plotHeaderHash: new byte[32],
            version: 1);

        var proof = new BlockProof(
            leafValue: new byte[32],
            leafIndex: 0,
            merkleProofPath: Array.Empty<byte[]>(),
            orientationBits: Array.Empty<bool>(),
            plotMetadata: plotMetadata);

        // Create block body
        var body = new BlockBody(transactions, proof);

        // Create complete genesis block
        var block = new Block(header, body);

        return block;
    }

    /// <summary>
    /// Creates premine transactions from the genesis configuration.
    /// </summary>
    /// <param name="config">The genesis configuration.</param>
    /// <param name="signer">The signer for signing the premine transactions.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of signed premine transactions.</returns>
    /// <remarks>
    /// Premine transactions use a special sender address (all zeros) to indicate they create new coins from nothing.
    /// This is called a "coinbase transaction" in blockchain terminology (the term comes from Bitcoin, not the company).
    /// This distinguishes them from regular user-to-user transfers where coins move between existing accounts.
    /// </remarks>
    private static async Task<IReadOnlyList<Transaction>> CreatePremineTransactionsAsync(
        GenesisConfig config,
        IBlockSigner signer,
        CancellationToken cancellationToken)
    {
        var transactions = new List<Transaction>();

        // Special sender address (all zeros) indicates this transaction creates new coins (a "coinbase" transaction)
        // Note: "coinbase" is blockchain terminology for transactions that mint new coins, not related to Coinbase the company
        var mintSender = new byte[33];

        // Create a transaction for each premine allocation
        foreach (var allocation in config.PreminedAllocations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Parse the public key from hex
            var recipientPublicKey = Convert.FromHexString(allocation.Key);

            if (recipientPublicKey.Length != 33)
            {
                throw new InvalidOperationException(
                    $"Invalid public key length for premine allocation: {allocation.Key}. Expected 33 bytes.");
            }

            // Create premine transaction
            // Sender: all zeros (indicates new coin creation)
            // Recipient: allocation recipient
            // Amount: allocation amount
            // Nonce: 0 (first transaction)
            // Fee: 0 (no fee for premine)
            // Signature: empty initially
            var tx = new Transaction(
                sender: mintSender,
                recipient: recipientPublicKey,
                amount: allocation.Value,
                nonce: 0,
                fee: 0,
                signature: []);

            // Sign the transaction with genesis signer
            var txHash = tx.ComputeHash();
            var txSignature = await signer.SignBlockHeaderAsync(txHash, cancellationToken);
            tx.SetSignature(txSignature);

            transactions.Add(tx);
        }

        return transactions;
    }

    /// <summary>
    /// Computes the Merkle root of a list of transactions.
    /// </summary>
    /// <param name="transactions">The list of transactions.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The 32-byte Merkle root hash.</returns>
    /// <remarks>
    /// If there are no transactions, returns a zero hash (32 zero bytes).
    /// Uses SHA256 for hashing transaction data via the MerkleTree library.
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

    /// <summary>
    /// Computes the genesis challenge from the network ID.
    /// </summary>
    /// <param name="networkId">The network identifier.</param>
    /// <returns>A 32-byte challenge hash.</returns>
    private static byte[] ComputeGenesisChallenge(string networkId)
    {
        // Genesis challenge is the hash of the network ID
        var networkIdBytes = System.Text.Encoding.UTF8.GetBytes(networkId);
        return SHA256.HashData(networkIdBytes);
    }
}
