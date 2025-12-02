namespace Spacetime.Plotting;

public sealed partial class PlotManager
{
    /// <summary>
    /// Serializable version of PlotMetadata for JSON persistence.
    /// </summary>
    private sealed class SerializablePlotMetadata
    {
        public Guid PlotId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string CacheFilePath { get; set; } = string.Empty;
        public long SpaceAllocatedBytes { get; set; }
        public string MerkleRoot { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
