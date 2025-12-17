namespace Spacetime.Network;

/// <summary>
/// Represents a request to download a block.
/// </summary>
internal sealed class BlockDownloadRequest
{
    /// <summary>
    /// Gets the block hash to download.
    /// </summary>
    public ReadOnlyMemory<byte> BlockHash { get; }

    /// <summary>
    /// Gets the block height.
    /// </summary>
    public long Height { get; }

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
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the request is completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the request failed.
    /// </summary>
    public bool IsFailed { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockDownloadRequest"/> class.
    /// </summary>
    /// <param name="blockHash">The block hash.</param>
    /// <param name="height">The block height.</param>
    public BlockDownloadRequest(ReadOnlyMemory<byte> blockHash, long height)
    {
        BlockHash = blockHash;
        Height = height;
        RetryCount = 0;
        IsCompleted = false;
        IsFailed = false;
    }
}
