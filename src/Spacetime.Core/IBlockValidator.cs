namespace Spacetime.Core;

/// <summary>
/// Provides block validation functionality.
/// </summary>
/// <remarks>
/// Block validators ensure that blocks meet consensus rules before they are broadcast or added to the chain.
/// This includes verifying signatures, proof scores, transaction validity, and other consensus requirements.
/// </remarks>
public interface IBlockValidator
{
    /// <summary>
    /// Validates a block against consensus rules.
    /// </summary>
    /// <param name="block">The block to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A detailed validation result including any errors.</returns>
    /// <remarks>
    /// Validation should include:
    /// - Block header signature verification
    /// - Proof score verification against difficulty
    /// - Transaction Merkle root verification
    /// - All transactions must be signed and valid
    /// - Block timestamp must be reasonable
    /// - Parent block must exist (if not genesis)
    /// </remarks>
    Task<BlockValidationResult> ValidateBlockAsync(Block block, CancellationToken cancellationToken = default);
}
