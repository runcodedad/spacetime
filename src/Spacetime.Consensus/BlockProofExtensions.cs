using Spacetime.Core;
using Spacetime.Plotting;

namespace Spacetime.Consensus;

/// <summary>
/// Extension methods for BlockProof.
/// </summary>
public static class BlockProofExtensions
{
    /// <summary>
    /// Converts a BlockProof to a Plotting.Proof for validation.
    /// </summary>
    /// <param name="blockProof">The block proof to convert.</param>
    /// <param name="challenge">The challenge used for this proof.</param>
    /// <param name="plotRoot">The Merkle root of the plot.</param>
    /// <param name="proofScore">The computed proof score.</param>
    /// <returns>A Plotting.Proof instance.</returns>
    public static Proof ToPlottingProof(
        this BlockProof blockProof,
        ReadOnlySpan<byte> challenge,
        ReadOnlySpan<byte> plotRoot,
        ReadOnlySpan<byte> proofScore)
    {
        ArgumentNullException.ThrowIfNull(blockProof);

        return new Proof(
            blockProof.LeafValue.ToArray(),
            blockProof.LeafIndex,
            blockProof.MerkleProofPath,
            blockProof.OrientationBits,
            plotRoot.ToArray(),
            challenge.ToArray(),
            proofScore.ToArray());
    }
}
