namespace Spacetime.Consensus;

/// <summary>
/// Specifies the type of proof validation error.
/// </summary>
public enum ProofValidationErrorType
{
    /// <summary>
    /// The Merkle proof path is invalid.
    /// </summary>
    InvalidMerklePath,

    /// <summary>
    /// The leaf value does not match the expected value.
    /// </summary>
    InvalidLeafValue,

    /// <summary>
    /// The plot root does not match the known plot identity.
    /// </summary>
    PlotRootMismatch,

    /// <summary>
    /// The score does not match the recalculated value.
    /// </summary>
    ScoreMismatch,

    /// <summary>
    /// The score does not meet the difficulty target.
    /// </summary>
    ScoreAboveTarget,

    /// <summary>
    /// The challenge does not match the expected challenge.
    /// </summary>
    ChallengeMismatch
}
