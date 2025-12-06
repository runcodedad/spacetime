using System.Security.Cryptography;
using MerkleTree.Core;
using Spacetime.Core;

namespace Spacetime.Consensus;

/// <summary>
/// Validates blocks according to consensus rules.
/// </summary>
/// <remarks>
/// <para>
/// The block validator performs comprehensive validation including:
/// </para>
/// <list type="bullet">
/// <item>Header validation (version, height, timestamp, parent hash, difficulty, epoch, challenge)</item>
/// <item>Block signature verification</item>
/// <item>Proof-of-Space-Time validation</item>
/// <item>Transaction validation (signatures, basic rules, Merkle root)</item>
/// <item>Difficulty target verification</item>
/// </list>
/// <para>
/// Validation is performed in order of computational cost, with cheaper checks first
/// to fail fast on invalid blocks.
/// </para>
/// <example>
/// Using the block validator:
/// <code>
/// var validator = new BlockValidator(signatureVerifier, proofValidator, chainState);
/// var result = await validator.ValidateBlockAsync(block);
/// 
/// if (!result.IsValid)
/// {
///     Console.WriteLine($"Block validation failed: {result.ErrorMessage}");
///     foreach (var error in result.Errors)
///     {
///         Console.WriteLine($"  - {error}");
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public sealed class BlockValidator : IBlockValidator
{
    private readonly ISignatureVerifier _signatureVerifier;
    private readonly ProofValidator _proofValidator;
    private readonly IChainState _chainState;
    private readonly MerkleTree.Hashing.IHashFunction _hashFunction;

    /// <summary>
    /// Maximum allowed timestamp skew in seconds (blocks can't be too far in the future).
    /// </summary>
    private const long MaxTimestampSkewSeconds = 120; // 2 minutes

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockValidator"/> class.
    /// </summary>
    /// <param name="signatureVerifier">The signature verifier for block and transaction signatures.</param>
    /// <param name="proofValidator">The proof validator for PoST proofs.</param>
    /// <param name="chainState">The chain state for accessing current blockchain information.</param>
    /// <param name="hashFunction">The hash function for computing Merkle roots.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public BlockValidator(
        ISignatureVerifier signatureVerifier,
        ProofValidator proofValidator,
        IChainState chainState,
        MerkleTree.Hashing.IHashFunction hashFunction)
    {
        ArgumentNullException.ThrowIfNull(signatureVerifier);
        ArgumentNullException.ThrowIfNull(proofValidator);
        ArgumentNullException.ThrowIfNull(chainState);
        ArgumentNullException.ThrowIfNull(hashFunction);

        _signatureVerifier = signatureVerifier;
        _proofValidator = proofValidator;
        _chainState = chainState;
        _hashFunction = hashFunction;
    }

    /// <summary>
    /// Validates a block against consensus rules.
    /// </summary>
    /// <param name="block">The block to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A detailed validation result including any errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when block is null.</exception>
    public async Task<BlockValidationResult> ValidateBlockAsync(
        Block block,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(block);

        try
        {
            // 1. Validate block header basic structure
            var headerValidation = ValidateHeaderStructure(block.Header);
            if (!headerValidation.IsValid)
            {
                return headerValidation;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 2. Validate timestamp
            var timestampValidation = ValidateTimestamp(block.Header);
            if (!timestampValidation.IsValid)
            {
                return timestampValidation;
            }

            // 3. Validate block signature
            var signatureValidation = ValidateBlockSignature(block);
            if (!signatureValidation.IsValid)
            {
                return signatureValidation;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 4. Validate against chain state (parent hash, height, difficulty, epoch, challenge)
            var chainValidation = await ValidateAgainstChainStateAsync(block.Header, cancellationToken);
            if (!chainValidation.IsValid)
            {
                return chainValidation;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 5. Validate transactions
            var transactionValidation = ValidateTransactions(block);
            if (!transactionValidation.IsValid)
            {
                return transactionValidation;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 6. Validate transaction Merkle root
            var merkleRootValidation = await ValidateTransactionMerkleRoot(block);
            if (!merkleRootValidation.IsValid)
            {
                return merkleRootValidation;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 7. Validate proof
            var proofValidation = await ValidateProofAsync(block, cancellationToken);
            if (!proofValidation.IsValid)
            {
                return proofValidation;
            }

            return BlockValidationResult.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BlockValidationResult.Failure(new BlockValidationError(
                BlockValidationErrorType.Other,
                $"Unexpected validation error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Validates the basic structure of the block header.
    /// </summary>
    private BlockValidationResult ValidateHeaderStructure(BlockHeader header)
    {
        // Check version
        if (header.Version != BlockHeader.CurrentVersion)
        {
            return BlockValidationResult.Failure(new BlockValidationError(
                BlockValidationErrorType.UnsupportedVersion,
                $"Unsupported block version: {header.Version}. Expected: {BlockHeader.CurrentVersion}"));
        }

        // Check height is non-negative
        if (header.Height < 0)
        {
            return BlockValidationResult.Failure(new BlockValidationError(
                BlockValidationErrorType.InvalidHeight,
                $"Block height must be non-negative, got: {header.Height}"));
        }

        // Check header is signed
        if (!header.IsSigned())
        {
            return BlockValidationResult.Failure(new BlockValidationError(
                BlockValidationErrorType.HeaderNotSigned,
                "Block header must be signed"));
        }

        // Check difficulty is positive
        if (header.Difficulty <= 0)
        {
            return BlockValidationResult.Failure(new BlockValidationError(
                BlockValidationErrorType.InvalidDifficulty,
                $"Difficulty must be positive, got: {header.Difficulty}"));
        }

        return BlockValidationResult.Success();
    }

    /// <summary>
    /// Validates the block timestamp.
    /// </summary>
    private BlockValidationResult ValidateTimestamp(BlockHeader header)
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var maxAllowedTime = currentTime + MaxTimestampSkewSeconds;

        if (header.Timestamp > maxAllowedTime)
        {
            return BlockValidationResult.Failure(new BlockValidationError(
                BlockValidationErrorType.InvalidTimestamp,
                $"Block timestamp {header.Timestamp} is too far in the future. " +
                $"Current time: {currentTime}, max allowed: {maxAllowedTime}"));
        }

        // Timestamp should not be negative
        if (header.Timestamp < 0)
        {
            return BlockValidationResult.Failure(new BlockValidationError(
                BlockValidationErrorType.InvalidTimestamp,
                $"Block timestamp must be non-negative, got: {header.Timestamp}"));
        }

        return BlockValidationResult.Success();
    }

    /// <summary>
    /// Validates the block signature.
    /// </summary>
    private BlockValidationResult ValidateBlockSignature(Block block)
    {
        try
        {
            var headerHash = block.Header.ComputeHash();
            var isValid = _signatureVerifier.VerifySignature(
                headerHash,
                block.Header.Signature.ToArray(),
                block.Header.MinerId.ToArray());

            if (!isValid)
            {
                return BlockValidationResult.Failure(new BlockValidationError(
                    BlockValidationErrorType.InvalidHeaderSignature,
                    $"Block header signature verification failed. Miner ID: {Convert.ToHexString(block.Header.MinerId)}"));
            }

            return BlockValidationResult.Success();
        }
        catch (Exception ex)
        {
            return BlockValidationResult.Failure(new BlockValidationError(
                BlockValidationErrorType.InvalidHeaderSignature,
                $"Block signature verification threw exception: {ex.Message}"));
        }
    }

    /// <summary>
    /// Validates the block against the current chain state.
    /// </summary>
    private async Task<BlockValidationResult> ValidateAgainstChainStateAsync(
        BlockHeader header,
        CancellationToken cancellationToken)
    {
        // Get chain state information
        var chainTipHash = await _chainState.GetChainTipHashAsync(cancellationToken);
        var chainTipHeight = await _chainState.GetChainTipHeightAsync(cancellationToken);
        var expectedDifficulty = await _chainState.GetExpectedDifficultyAsync(cancellationToken);
        var expectedEpoch = await _chainState.GetExpectedEpochAsync(cancellationToken);
        var expectedChallenge = await _chainState.GetExpectedChallengeAsync(cancellationToken);

        // Validate parent hash
        if (chainTipHash != null)
        {
            if (!header.ParentHash.SequenceEqual(chainTipHash))
            {
                return BlockValidationResult.Failure(new BlockValidationError(
                    BlockValidationErrorType.InvalidParentHash,
                    $"Parent hash mismatch. Expected: {Convert.ToHexString(chainTipHash)}, " +
                    $"got: {Convert.ToHexString(header.ParentHash)}"));
            }
        }

        // Validate height
        var expectedHeight = chainTipHeight + 1;
        if (header.Height != expectedHeight)
        {
            return BlockValidationResult.Failure(new BlockValidationError(
                BlockValidationErrorType.InvalidHeight,
                $"Block height mismatch. Expected: {expectedHeight}, got: {header.Height}"));
        }

        // Validate difficulty
        if (header.Difficulty != expectedDifficulty)
        {
            return BlockValidationResult.Failure(new BlockValidationError(
                BlockValidationErrorType.InvalidDifficulty,
                $"Difficulty mismatch. Expected: {expectedDifficulty}, got: {header.Difficulty}"));
        }

        // Validate epoch
        if (header.Epoch != expectedEpoch)
        {
            return BlockValidationResult.Failure(new BlockValidationError(
                BlockValidationErrorType.InvalidEpoch,
                $"Epoch mismatch. Expected: {expectedEpoch}, got: {header.Epoch}"));
        }

        // Validate challenge
        if (!header.Challenge.SequenceEqual(expectedChallenge))
        {
            return BlockValidationResult.Failure(new BlockValidationError(
                BlockValidationErrorType.InvalidChallenge,
                $"Challenge mismatch. Expected: {Convert.ToHexString(expectedChallenge)}, " +
                $"got: {Convert.ToHexString(header.Challenge)}"));
        }

        return BlockValidationResult.Success();
    }

    /// <summary>
    /// Validates all transactions in the block.
    /// </summary>
    private BlockValidationResult ValidateTransactions(Block block)
    {
        var transactions = block.Body.Transactions;

        // Empty blocks are allowed - they are expected for newer or less used blockchains
        // No coinbase transaction is required as we don't want to mint new coins for every empty block

        foreach (var tx in transactions)
        {
            // Validate basic transaction rules
            if (!tx.ValidateBasicRules())
            {
                return BlockValidationResult.Failure(new BlockValidationError(
                    BlockValidationErrorType.InvalidTransaction,
                    $"Transaction failed basic validation: {Convert.ToHexString(tx.ComputeHash())}"));
            }

            // Verify transaction signature
            try
            {
                var txHash = tx.ComputeHash();
                var isValid = _signatureVerifier.VerifySignature(
                    txHash,
                    tx.Signature.ToArray(),
                    tx.Sender.ToArray());

                if (!isValid)
                {
                    return BlockValidationResult.Failure(new BlockValidationError(
                        BlockValidationErrorType.InvalidTransactionSignature,
                        $"Transaction signature verification failed: {Convert.ToHexString(txHash)}"));
                }
            }
            catch (Exception ex)
            {
                return BlockValidationResult.Failure(new BlockValidationError(
                    BlockValidationErrorType.InvalidTransactionSignature,
                    $"Transaction signature verification threw exception: {ex.Message}"));
            }
        }

        return BlockValidationResult.Success();
    }

    /// <summary>
    /// Validates the transaction Merkle root.
    /// </summary>
    private async Task<BlockValidationResult> ValidateTransactionMerkleRoot(Block block)
    {
        try
        {
            var transactions = block.Body.Transactions;
            
            // Compute Merkle root of transactions
            byte[] computedRoot;
            
            if (transactions.Count == 0)
            {
                // Empty transaction list produces zero hash
                computedRoot = new byte[32];
            }
            else
            {
                // Build Merkle tree using the MerkleTree library
                // Note: We use the in-memory approach as suggested, building from transaction hashes
                var txHashes = transactions
                    .Select(tx => tx.ComputeHash())
                    .ToList();

                var merkleTreeStream = new MerkleTreeStream(_hashFunction);
                var leaves = ToAsyncEnumerable(txHashes);
                var metadata = await merkleTreeStream.BuildAsync(leaves, cacheConfig: null, CancellationToken.None);
                computedRoot = metadata.RootHash;
            }

            // Compare with header's transaction root
            if (!block.Header.TxRoot.SequenceEqual(computedRoot))
            {
                return BlockValidationResult.Failure(new BlockValidationError(
                    BlockValidationErrorType.InvalidTransactionRoot,
                    $"Transaction Merkle root mismatch. Expected: {Convert.ToHexString(computedRoot)}, " +
                    $"got: {Convert.ToHexString(block.Header.TxRoot)}"));
            }

            return BlockValidationResult.Success();
        }
        catch (Exception ex)
        {
            return BlockValidationResult.Failure(new BlockValidationError(
                BlockValidationErrorType.InvalidTransactionRoot,
                $"Failed to compute transaction Merkle root: {ex.Message}"));
        }
    }

    /// <summary>
    /// Converts a list to an async enumerable.
    /// </summary>
    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates the Proof-of-Space-Time proof.
    /// </summary>
    private async Task<BlockValidationResult> ValidateProofAsync(
        Block block,
        CancellationToken cancellationToken)
    {
        try
        {
            // Convert BlockProof to Plotting.Proof format using extension method
            var proof = block.Body.Proof.ToPlottingProof(
                block.Header.Challenge,
                block.Header.PlotRoot,
                block.Header.ProofScore);

            // Convert difficulty to target
            var difficultyTarget = DifficultyAdjuster.DifficultyToTarget(block.Header.Difficulty);

            // Validate proof
            var proofResult = _proofValidator.ValidateProof(
                proof,
                expectedChallenge: block.Header.Challenge.ToArray(),
                expectedPlotRoot: block.Header.PlotRoot.ToArray(),
                difficultyTarget: difficultyTarget);

            if (!proofResult.IsValid)
            {
                // Determine specific error type
                var errorType = proofResult.Error?.Type switch
                {
                    ProofValidationErrorType.ScoreAboveTarget => BlockValidationErrorType.ProofScoreTooHigh,
                    _ => BlockValidationErrorType.InvalidProof
                };

                return BlockValidationResult.Failure(new BlockValidationError(
                    errorType,
                    proofResult.ErrorMessage ?? "Proof validation failed"));
            }

            return BlockValidationResult.Success();
        }
        catch (Exception ex)
        {
            return BlockValidationResult.Failure(new BlockValidationError(
                BlockValidationErrorType.InvalidProof,
                $"Proof validation threw exception: {ex.Message}"));
        }
    }
}
