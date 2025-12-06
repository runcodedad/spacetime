namespace Spacetime.Storage;

/// <summary>
/// Interface for storing and retrieving chain metadata.
/// </summary>
public interface IChainMetadata
{
    /// <summary>
    /// Gets the best (tip) block hash.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The best block hash, or null if not set.</returns>
    Task<ReadOnlyMemory<byte>?> GetBestBlockHashAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the best (tip) block hash.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetBestBlockHashAsync(ReadOnlyMemory<byte> hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current chain height.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chain height, or null if not set.</returns>
    Task<long?> GetChainHeightAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the current chain height.
    /// </summary>
    /// <param name="height">The chain height.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetChainHeightAsync(long height, CancellationToken cancellationToken = default);
}
