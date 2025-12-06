using Spacetime.Core;

namespace Spacetime.Storage;

/// <summary>
/// Interface for storing and retrieving blocks.
/// </summary>
public interface IBlockStorage
{
    /// <summary>
    /// Stores a block header.
    /// </summary>
    /// <param name="header">The block header to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StoreHeaderAsync(BlockHeader header, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a block body.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <param name="body">The block body to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StoreBodyAsync(ReadOnlyMemory<byte> hash, BlockBody body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a complete block (header and body).
    /// </summary>
    /// <param name="block">The block to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StoreBlockAsync(Block block, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a block header by hash.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The block header, or null if not found.</returns>
    Task<BlockHeader?> GetHeaderByHashAsync(ReadOnlyMemory<byte> hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a block header by height.
    /// </summary>
    /// <param name="height">The block height.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The block header, or null if not found.</returns>
    Task<BlockHeader?> GetHeaderByHeightAsync(long height, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a block body by hash.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The block body, or null if not found.</returns>
    Task<BlockBody?> GetBodyByHashAsync(ReadOnlyMemory<byte> hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a complete block by hash.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The block, or null if not found.</returns>
    Task<Block?> GetBlockByHashAsync(ReadOnlyMemory<byte> hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a complete block by height.
    /// </summary>
    /// <param name="height">The block height.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The block, or null if not found.</returns>
    Task<Block?> GetBlockByHeightAsync(long height, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a block exists by hash.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the block exists, false otherwise.</returns>
    Task<bool> ExistsAsync(ReadOnlyMemory<byte> hash, CancellationToken cancellationToken = default);
}
