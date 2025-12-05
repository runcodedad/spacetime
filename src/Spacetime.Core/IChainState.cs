namespace Spacetime.Core;

/// <summary>
/// Provides access to the current blockchain state for validation.
/// </summary>
/// <remarks>
/// The chain state provides information needed to validate blocks,
/// such as the current chain tip, expected difficulty, and epoch information.
/// </remarks>
public interface IChainState
{
    /// <summary>
    /// Gets the hash of the current chain tip (the last valid block).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The 32-byte hash of the chain tip, or null if chain is empty.</returns>
    Task<byte[]?> GetChainTipHashAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the height of the current chain tip.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The height of the chain tip, or -1 if chain is empty.</returns>
    Task<long> GetChainTipHeightAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the expected difficulty for the next block.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The expected difficulty value.</returns>
    Task<long> GetExpectedDifficultyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the expected epoch for the next block.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The expected epoch number.</returns>
    Task<long> GetExpectedEpochAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the expected challenge for the current epoch.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The 32-byte expected challenge.</returns>
    Task<byte[]> GetExpectedChallengeAsync(CancellationToken cancellationToken = default);
}
