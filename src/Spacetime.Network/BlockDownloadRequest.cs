namespace Spacetime.Network;

/// <summary>
/// Represents a request to download a block.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BlockDownloadRequest"/> class.
/// </remarks>
/// <param name="blockHash">The block hash.</param>
/// <param name="height">The block height.</param>
internal sealed class BlockDownloadRequest(ReadOnlyMemory<byte> blockHash, long height)
{
    /// <summary>
    /// Gets the block hash to download.
    /// </summary>
    public ReadOnlyMemory<byte> BlockHash { get; } = blockHash;

    /// <summary>
    /// Gets the block height.
    /// </summary>
    public long Height { get; } = height;

    /// <summary>
    /// Gets the peer ID assigned to download this block.
    /// </summary>
    public string? AssignedPeerId { get; set; }

    /// <summary>
    /// Gets the timestamp when the request was assigned.
    /// </summary>
    public DateTimeOffset? AssignedAt { get; set; }

    /// <summary>
    /// Gets the number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether the request is completed.
    /// </summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the request failed.
    /// </summary>
    public bool IsFailed { get; set; } = false;
}
