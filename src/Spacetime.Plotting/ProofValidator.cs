using MerkleTree.Hashing;
using MerkleTree.Proofs;

namespace Spacetime.Plotting;

/// <summary>
/// Validates cryptographic proofs from plot files.
/// </summary>
/// <remarks>
/// The proof validator performs the following checks:
/// 1. Recalculates score = H(challenge || leaf) and verifies it matches
/// 2. Compares score against difficulty target (score &lt; target)
/// 3. Verifies Merkle path using the MerkleTree library
/// 4. Verifies plot root matches known plot identity
/// 5. Verifies challenge matches expected challenge
/// 
/// All validation failures include detailed error messages.
/// </remarks>
public sealed class ProofValidator
{
    private readonly IHashFunction _hashFunction;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProofValidator"/> class.
    /// </summary>
    /// <param name="hashFunction">Hash function for score computation and Merkle verification</param>
    public ProofValidator(IHashFunction hashFunction)
    {
        ArgumentNullException.ThrowIfNull(hashFunction);
        _hashFunction = hashFunction;
    }

    /// <summary>
    /// Computes the score for a proof given a challenge and leaf value.
    /// </summary>
    /// <param name="challenge">The 32-byte challenge.</param>
    /// <param name="leafValue">The 32-byte leaf value.</param>
    /// <returns>A 32-byte score computed as H(challenge || leaf).</returns>
    /// <exception cref="ArgumentNullException">Thrown when arguments are null.</exception>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid sizes.</exception>
    /// <remarks>
    /// Score is computed as: score = SHA256(challenge || leaf)
    /// Lower scores are better in the proof-of-space-time consensus.
    /// </remarks>
    public byte[] ComputeScore(byte[] challenge, byte[] leafValue)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        ArgumentNullException.ThrowIfNull(leafValue);

        if (challenge.Length != 32)
        {
            throw new ArgumentException("Challenge must be 32 bytes", nameof(challenge));
        }

        if (leafValue.Length != 32)
        {
            throw new ArgumentException("Leaf value must be 32 bytes", nameof(leafValue));
        }

        var input = new byte[challenge.Length + leafValue.Length];
        challenge.CopyTo(input.AsSpan());
        leafValue.CopyTo(input.AsSpan(challenge.Length));

        return _hashFunction.ComputeHash(input);
    }

    /// <summary>
    /// Compares a score against a difficulty target.
    /// </summary>
    /// <param name="score">The 32-byte proof score to check.</param>
    /// <param name="difficultyTarget">The 32-byte difficulty target.</param>
    /// <returns>True if the score meets the target (score &lt; target); otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when arguments are null.</exception>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid sizes.</exception>
    /// <remarks>
    /// A proof is valid when its score is strictly less than the difficulty target.
    /// Scores are compared as big-endian unsigned integers.
    /// </remarks>
    public bool IsScoreBelowTarget(byte[] score, byte[] difficultyTarget)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(difficultyTarget);

        if (score.Length != 32)
        {
            throw new ArgumentException("Score must be 32 bytes", nameof(score));
        }

        if (difficultyTarget.Length != 32)
        {
            throw new ArgumentException("Difficulty target must be 32 bytes", nameof(difficultyTarget));
        }

        return CompareScores(score, difficultyTarget) < 0;
    }

    /// <summary>
    /// Validates a complete proof with all verification checks.
    /// </summary>
    /// <param name="proof">The proof to validate.</param>
    /// <param name="expectedChallenge">The expected challenge for this proof.</param>
    /// <param name="expectedPlotRoot">The expected Merkle root of the plot.</param>
    /// <param name="difficultyTarget">The difficulty target the score must meet (optional).</param>
    /// <param name="treeHeight">The height of the Merkle tree.</param>
    /// <returns>A validation result indicating success or detailed failure information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required arguments are null.</exception>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid values.</exception>
    /// <remarks>
    /// Validation performs the following checks in order:
    /// 1. Challenge correctness
    /// 2. Plot root matches known identity
    /// 3. Score recalculation and verification
    /// 4. Difficulty target comparison (if provided)
    /// 5. Merkle path verification
    /// 
    /// The first failure encountered is returned as the validation result.
    /// </remarks>
    public ProofValidationResult ValidateProof(
        Proof proof,
        byte[] expectedChallenge,
        byte[] expectedPlotRoot,
        byte[]? difficultyTarget = null,
        int? treeHeight = null)
    {
        ArgumentNullException.ThrowIfNull(proof);
        ArgumentNullException.ThrowIfNull(expectedChallenge);
        ArgumentNullException.ThrowIfNull(expectedPlotRoot);

        if (expectedChallenge.Length != 32)
        {
            throw new ArgumentException("Expected challenge must be 32 bytes", nameof(expectedChallenge));
        }

        if (expectedPlotRoot.Length != 32)
        {
            throw new ArgumentException("Expected plot root must be 32 bytes", nameof(expectedPlotRoot));
        }

        if (difficultyTarget != null && difficultyTarget.Length != 32)
        {
            throw new ArgumentException("Difficulty target must be 32 bytes", nameof(difficultyTarget));
        }

        // 1. Verify challenge correctness
        if (!proof.Challenge.AsSpan().SequenceEqual(expectedChallenge))
        {
            return ProofValidationResult.Failure(new ProofValidationError(
                ProofValidationErrorType.ChallengeMismatch,
                $"Challenge mismatch: expected {Convert.ToHexString(expectedChallenge)}, " +
                $"but proof contains {Convert.ToHexString(proof.Challenge)}"));
        }

        // 2. Verify plot root matches known plot identity
        if (!proof.MerkleRoot.AsSpan().SequenceEqual(expectedPlotRoot))
        {
            return ProofValidationResult.Failure(new ProofValidationError(
                ProofValidationErrorType.PlotRootMismatch,
                $"Plot root mismatch: expected {Convert.ToHexString(expectedPlotRoot)}, " +
                $"but proof contains {Convert.ToHexString(proof.MerkleRoot)}"));
        }

        // 3. Recalculate and verify score
        var recalculatedScore = ComputeScore(proof.Challenge, proof.LeafValue);
        if (!recalculatedScore.AsSpan().SequenceEqual(proof.Score))
        {
            return ProofValidationResult.Failure(new ProofValidationError(
                ProofValidationErrorType.ScoreMismatch,
                $"Score mismatch: recalculated score {Convert.ToHexString(recalculatedScore)} " +
                $"does not match proof score {Convert.ToHexString(proof.Score)}"));
        }

        // 4. Verify score meets difficulty target (if provided)
        if (difficultyTarget != null && !IsScoreBelowTarget(proof.Score, difficultyTarget))
        {
            return ProofValidationResult.Failure(new ProofValidationError(
                ProofValidationErrorType.ScoreAboveTarget,
                $"Score {Convert.ToHexString(proof.Score)} does not meet difficulty target " +
                $"{Convert.ToHexString(difficultyTarget)} (score must be strictly less than target)"));
        }

        // 5. Verify Merkle proof path
        var merkleValidationResult = ValidateMerklePath(proof, treeHeight);
        if (!merkleValidationResult.IsValid)
        {
            return merkleValidationResult;
        }

        return ProofValidationResult.Success();
    }

    /// <summary>
    /// Validates the Merkle proof path using the MerkleTree library.
    /// </summary>
    /// <param name="proof">The proof containing the Merkle path to validate.</param>
    /// <param name="treeHeight">The height of the Merkle tree (optional, will be inferred if not provided).</param>
    /// <returns>A validation result indicating success or failure.</returns>
    /// <remarks>
    /// Uses the MerkleTree library's MerkleProof.Verify method to validate the path.
    /// </remarks>
    private ProofValidationResult ValidateMerklePath(Proof proof, int? treeHeight)
    {
        try
        {
            // Infer tree height from sibling hashes count if not provided
            var height = treeHeight ?? proof.SiblingHashes.Count;

            // Create MerkleProof object for validation using the MerkleTree library
            var merkleProof = new MerkleProof(
                proof.LeafValue,
                proof.LeafIndex,
                height,
                proof.SiblingHashes.ToArray(),
                proof.OrientationBits.ToArray());

            // Verify using the MerkleTree library
            var isValid = merkleProof.Verify(proof.MerkleRoot, _hashFunction);

            if (!isValid)
            {
                return ProofValidationResult.Failure(new ProofValidationError(
                    ProofValidationErrorType.InvalidMerklePath,
                    $"Merkle proof verification failed: the leaf at index {proof.LeafIndex} " +
                    $"with value {Convert.ToHexString(proof.LeafValue)} cannot be verified " +
                    $"against root {Convert.ToHexString(proof.MerkleRoot)}"));
            }

            return ProofValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ProofValidationResult.Failure(new ProofValidationError(
                ProofValidationErrorType.InvalidMerklePath,
                $"Merkle proof verification failed with exception: {ex.Message}"));
        }
    }

    /// <summary>
    /// Compares two scores. Returns negative if score1 &lt; score2, zero if equal, positive if score1 &gt; score2.
    /// </summary>
    /// <remarks>
    /// Scores are compared as big-endian unsigned integers (byte-by-byte from left to right).
    /// </remarks>
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
}
