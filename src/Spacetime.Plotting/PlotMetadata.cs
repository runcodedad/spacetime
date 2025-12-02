namespace Spacetime.Plotting;

/// <summary>
/// Represents metadata about a plot file used for tracking and management.
/// </summary>
/// <param name="PlotId">Unique identifier for the plot.</param>
/// <param name="FilePath">Absolute path to the plot file.</param>
/// <param name="CacheFilePath">Absolute path to the cache file, if any.</param>
/// <param name="SpaceAllocatedBytes">Total disk space allocated by the plot file in bytes.</param>
/// <param name="MerkleRoot">The Merkle root hash of the plot (32 bytes).</param>
/// <param name="CreatedAtUtc">UTC timestamp when the plot was created.</param>
/// <param name="Status">Current status of the plot file.</param>
public sealed record PlotMetadata(
    Guid PlotId,
    string FilePath,
    string? CacheFilePath,
    long SpaceAllocatedBytes,
    byte[] MerkleRoot,
    DateTime CreatedAtUtc,
    PlotStatus Status)
{
    /// <summary>
    /// Creates a new <see cref="PlotMetadata"/> with an updated status.
    /// </summary>
    /// <param name="newStatus">The new status to set.</param>
    /// <returns>A new <see cref="PlotMetadata"/> instance with the updated status.</returns>
    public PlotMetadata WithStatus(PlotStatus newStatus) =>
        this with { Status = newStatus };

    /// <summary>
    /// Creates metadata from a <see cref="PlotLoader"/> instance.
    /// </summary>
    /// <param name="loader">The loaded plot.</param>
    /// <param name="cacheFilePath">Optional cache file path.</param>
    /// <param name="plotId">Optional plot ID. If not provided, a new GUID is generated.</param>
    /// <param name="createdAtUtc">Optional creation timestamp. If not provided, current UTC time is used.</param>
    /// <returns>A new <see cref="PlotMetadata"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when loader is null.</exception>
    public static PlotMetadata FromPlotLoader(
        PlotLoader loader,
        string? cacheFilePath = null,
        Guid? plotId = null,
        DateTime? createdAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(loader);

        var fileInfo = new FileInfo(loader.FilePath);
        return new PlotMetadata(
            PlotId: plotId ?? Guid.NewGuid(),
            FilePath: loader.FilePath,
            CacheFilePath: cacheFilePath,
            SpaceAllocatedBytes: fileInfo.Exists ? fileInfo.Length : 0,
            MerkleRoot: loader.MerkleRoot.ToArray(),
            CreatedAtUtc: createdAtUtc ?? DateTime.UtcNow,
            Status: PlotStatus.Valid);
    }
}
