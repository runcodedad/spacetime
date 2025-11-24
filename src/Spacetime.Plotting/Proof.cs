namespace Spacetime.Plotting;

/// <summary>
/// Represents a cryptographic proof generated from a plot that demonstrates
/// a miner has valid data matching a challenge.
/// </summary>
/// <remarks>
/// A proof consists of:
/// - The leaf value that produced the best score
/// - The leaf's position (index) in the plot
/// - Merkle proof data (sibling hashes and orientation bits)
/// - The Merkle root hash for verification
/// - The challenge used to compute the score
/// - The computed score (lower is better)
/// 
/// The proof can be verified by:
/// 1. Recomputing score = H(challenge || leaf)
/// 2. Verifying the Merkle proof shows leaf is in tree with given root
/// </remarks>
public sealed class Proof
{
    /// <summary>
    /// Gets the leaf value that produced this proof.
    /// </summary>
    public byte[] LeafValue { get; }

    /// <summary>
    /// Gets the zero-based index of the leaf in the plot.
    /// </summary>
    public long LeafIndex { get; }

    /// <summary>
    /// Gets the sibling hashes for the Merkle proof path.
    /// </summary>
    /// <remarks>
    /// Each entry is a 32-byte hash. The array length equals the tree height.
    /// </remarks>
    public IReadOnlyList<byte[]> SiblingHashes { get; }

    /// <summary>
    /// Gets the orientation bits for the Merkle proof path.
    /// </summary>
    /// <remarks>
    /// Each bit indicates whether the sibling is on the left (0) or right (1).
    /// The length equals the tree height.
    /// </remarks>
    public IReadOnlyList<bool> OrientationBits { get; }

    /// <summary>
    /// Gets the Merkle root hash of the plot.
    /// </summary>
    public byte[] MerkleRoot { get; }

    /// <summary>
    /// Gets the challenge that was used to generate this proof.
    /// </summary>
    public byte[] Challenge { get; }

    /// <summary>
    /// Gets the computed score for this proof.
    /// </summary>
    /// <remarks>
    /// Score is computed as H(challenge || leaf). Lower scores are better.
    /// </remarks>
    public byte[] Score { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Proof"/> class.
    /// </summary>
    /// <param name="leafValue">The leaf value</param>
    /// <param name="leafIndex">The leaf index</param>
    /// <param name="siblingHashes">The sibling hashes for Merkle proof</param>
    /// <param name="orientationBits">The orientation bits for Merkle proof</param>
    /// <param name="merkleRoot">The Merkle root hash</param>
    /// <param name="challenge">The challenge used</param>
    /// <param name="score">The computed score</param>
    public Proof(
        byte[] leafValue,
        long leafIndex,
        IReadOnlyList<byte[]> siblingHashes,
        IReadOnlyList<bool> orientationBits,
        byte[] merkleRoot,
        byte[] challenge,
        byte[] score)
    {
        ArgumentNullException.ThrowIfNull(leafValue);
        ArgumentNullException.ThrowIfNull(siblingHashes);
        ArgumentNullException.ThrowIfNull(orientationBits);
        ArgumentNullException.ThrowIfNull(merkleRoot);
        ArgumentNullException.ThrowIfNull(challenge);
        ArgumentNullException.ThrowIfNull(score);

        if (leafIndex < 0)
        {
            throw new ArgumentException("Leaf index must be non-negative", nameof(leafIndex));
        }

        if (leafValue.Length != 32)
        {
            throw new ArgumentException("Leaf value must be 32 bytes", nameof(leafValue));
        }

        if (merkleRoot.Length != 32)
        {
            throw new ArgumentException("Merkle root must be 32 bytes", nameof(merkleRoot));
        }

        if (challenge.Length != 32)
        {
            throw new ArgumentException("Challenge must be 32 bytes", nameof(challenge));
        }

        if (score.Length != 32)
        {
            throw new ArgumentException("Score must be 32 bytes", nameof(score));
        }

        if (siblingHashes.Count != orientationBits.Count)
        {
            throw new ArgumentException(
                "Sibling hashes and orientation bits must have the same count",
                nameof(orientationBits));
        }

        foreach (var hash in siblingHashes)
        {
            if (hash == null || hash.Length != 32)
            {
                throw new ArgumentException(
                    "All sibling hashes must be 32 bytes",
                    nameof(siblingHashes));
            }
        }

        LeafValue = leafValue;
        LeafIndex = leafIndex;
        SiblingHashes = siblingHashes;
        OrientationBits = orientationBits;
        MerkleRoot = merkleRoot;
        Challenge = challenge;
        Score = score;
    }
}
