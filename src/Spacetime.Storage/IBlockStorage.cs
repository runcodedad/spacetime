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
    void StoreHeader(BlockHeader header);

    /// <summary>
    /// Stores a block body.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <param name="body">The block body to store.</param>
    void StoreBody(ReadOnlyMemory<byte> hash, BlockBody body);

    /// <summary>
    /// Stores a complete block (header and body).
    /// </summary>
    /// <param name="block">The block to store.</param>
    void StoreBlock(Block block);

    /// <summary>
    /// Retrieves a block header by hash.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <returns>The block header, or null if not found.</returns>
    BlockHeader? GetHeaderByHash(ReadOnlyMemory<byte> hash);

    /// <summary>
    /// Retrieves a block header by height.
    /// </summary>
    /// <param name="height">The block height.</param>
    /// <returns>The block header, or null if not found.</returns>
    BlockHeader? GetHeaderByHeight(long height);

    /// <summary>
    /// Retrieves a block body by hash.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <returns>The block body, or null if not found.</returns>
    BlockBody? GetBodyByHash(ReadOnlyMemory<byte> hash);

    /// <summary>
    /// Retrieves a complete block by hash.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <returns>The block, or null if not found.</returns>
    Block? GetBlockByHash(ReadOnlyMemory<byte> hash);

    /// <summary>
    /// Retrieves a complete block by height.
    /// </summary>
    /// <param name="height">The block height.</param>
    /// <returns>The block, or null if not found.</returns>
    Block? GetBlockByHeight(long height);

    /// <summary>
    /// Checks if a block exists by hash.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <returns>True if the block exists, false otherwise.</returns>
    bool Exists(ReadOnlyMemory<byte> hash);

    /// <summary>
    /// Marks a block as orphaned.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <remarks>
    /// Orphaned blocks are blocks that were part of the main chain but were
    /// replaced during a chain reorganization.
    /// </remarks>
    void MarkAsOrphaned(ReadOnlyMemory<byte> hash);

    /// <summary>
    /// Checks if a block is marked as orphaned.
    /// </summary>
    /// <param name="hash">The block hash.</param>
    /// <returns>True if the block is orphaned, false otherwise.</returns>
    bool IsOrphaned(ReadOnlyMemory<byte> hash);
}
