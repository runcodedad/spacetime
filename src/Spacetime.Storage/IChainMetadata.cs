namespace Spacetime.Storage;

/// <summary>
/// Interface for storing and retrieving chain metadata.
/// </summary>
public interface IChainMetadata
{
    /// <summary>
    /// Gets the best (tip) block hash.
    /// </summary>
    /// <returns>The best block hash, or null if not set.</returns>
    ReadOnlyMemory<byte>? GetBestBlockHash();

    /// <summary>
    /// Sets the best (tip) block hash.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    void SetBestBlockHash(ReadOnlyMemory<byte> hash);

    /// <summary>
    /// Gets the current chain height.
    /// </summary>
    /// <returns>The chain height, or null if not set.</returns>
    long? GetChainHeight();

    /// <summary>
    /// Sets the current chain height.
    /// </summary>
    /// <param name="height">The chain height.</param>
    void SetChainHeight(long height);
}
