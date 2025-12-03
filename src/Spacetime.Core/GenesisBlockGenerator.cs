using System.Security.Cryptography;

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
        var transactions = await CreatePremineTransactionsAsync(config, cancellationToken);

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
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of premine transactions.</returns>
    private static async Task<IReadOnlyList<Transaction>> CreatePremineTransactionsAsync(
        GenesisConfig config,
        CancellationToken cancellationToken)
    {
        var transactions = new List<Transaction>();

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
            // Note: In a real implementation, this would use a proper Transaction constructor
            // For now, we'll create an empty list since Transaction structure may need to be updated
            // to support coinbase/premine transactions
        }

        // Return empty list for now - premine transactions will be added in a future update
        // when the Transaction class is extended to support coinbase transactions
        await Task.CompletedTask;
        return transactions;
    }

    /// <summary>
    /// Computes the Merkle root of a list of transactions.
    /// </summary>
    /// <param name="transactions">The list of transactions.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The 32-byte Merkle root hash.</returns>
    private static async Task<byte[]> ComputeTransactionMerkleRootAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        if (transactions.Count == 0)
        {
            // Empty transaction list produces zero hash
            await Task.CompletedTask;
            return new byte[32];
        }

        // For now, return zero hash since we don't have transactions yet
        // In future, this will use MerkleTree library
        return new byte[32];
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
