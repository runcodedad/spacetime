namespace Spacetime.Consensus;

/// <summary>
/// Specifies the type of block validation error.
/// </summary>
public enum BlockValidationErrorType
{
    /// <summary>
    /// Block header is not signed.
    /// </summary>
    HeaderNotSigned,

    /// <summary>
    /// Block header signature is invalid.
    /// </summary>
    InvalidHeaderSignature,

    /// <summary>
    /// Block timestamp is outside allowed skew range.
    /// </summary>
    InvalidTimestamp,

    /// <summary>
    /// Previous block hash does not match chain tip.
    /// </summary>
    InvalidParentHash,

    /// <summary>
    /// Difficulty value is invalid or does not match expected difficulty.
    /// </summary>
    InvalidDifficulty,

    /// <summary>
    /// Epoch number is incorrect.
    /// </summary>
    InvalidEpoch,

    /// <summary>
    /// Challenge does not match expected challenge for the epoch.
    /// </summary>
    InvalidChallenge,

    /// <summary>
    /// Proof validation failed.
    /// </summary>
    InvalidProof,

    /// <summary>
    /// Proof score does not meet difficulty target.
    /// </summary>
    ProofScoreTooHigh,

    /// <summary>
    /// Transaction Merkle root does not match header.
    /// </summary>
    InvalidTransactionRoot,

    /// <summary>
    /// One or more transactions have invalid signatures.
    /// </summary>
    InvalidTransactionSignature,

    /// <summary>
    /// One or more transactions fail basic validation rules.
    /// </summary>
    InvalidTransaction,

    /// <summary>
    /// Block body contains no transactions but should have at least a coinbase.
    /// </summary>
    NoTransactions,

    /// <summary>
    /// Block version is not supported.
    /// </summary>
    UnsupportedVersion,

    /// <summary>
    /// Block height is invalid.
    /// </summary>
    InvalidHeight,

    /// <summary>
    /// Generic validation failure.
    /// </summary>
    Other
}
