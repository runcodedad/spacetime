namespace Spacetime.Core;

/// <summary>
/// Represents the metadata of the plot file used to generate a block proof.
/// </summary>
/// <remarks>
/// This metadata is included in the block body to allow verifiers to validate
/// that the proof came from a legitimate plot file.
/// </remarks>
/// <param name="LeafCount">The total number of leaves in the plot.</param>
/// <param name="PlotId">The unique 32-byte identifier of the plot.</param>
/// <param name="PlotHeaderHash">The 32-byte SHA256 hash of the plot header.</param>
/// <param name="Version">The version of the plot format.</param>
public sealed record BlockPlotMetadata(
    long LeafCount,
    byte[] PlotId,
    byte[] PlotHeaderHash,
    byte Version)
{
    /// <summary>
    /// Size of plot ID and plot header hash in bytes (SHA256).
    /// </summary>
    private const int HashSize = 32;

    /// <summary>
    /// Creates a new instance of <see cref="BlockPlotMetadata"/> with validation.
    /// </summary>
    /// <param name="leafCount">The total number of leaves in the plot.</param>
    /// <param name="plotId">The unique 32-byte identifier of the plot.</param>
    /// <param name="plotHeaderHash">The 32-byte SHA256 hash of the plot header.</param>
    /// <param name="version">The version of the plot format.</param>
    /// <returns>A new validated <see cref="BlockPlotMetadata"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when plotId or plotHeaderHash is null.</exception>
    /// <exception cref="ArgumentException">Thrown when parameters have invalid values.</exception>
    public static BlockPlotMetadata Create(
        long leafCount,
        byte[] plotId,
        byte[] plotHeaderHash,
        byte version)
    {
        ArgumentNullException.ThrowIfNull(plotId);
        ArgumentNullException.ThrowIfNull(plotHeaderHash);

        if (leafCount <= 0)
        {
            throw new ArgumentException("Leaf count must be positive", nameof(leafCount));
        }

        if (plotId.Length != HashSize)
        {
            throw new ArgumentException($"Plot ID must be {HashSize} bytes", nameof(plotId));
        }

        if (plotHeaderHash.Length != HashSize)
        {
            throw new ArgumentException($"Plot header hash must be {HashSize} bytes", nameof(plotHeaderHash));
        }

        return new BlockPlotMetadata(leafCount, plotId, plotHeaderHash, version);
    }

    /// <summary>
    /// Serializes the metadata using a <see cref="BinaryWriter"/>.
    /// </summary>
    /// <param name="writer">The binary writer to serialize to.</param>
    /// <exception cref="ArgumentNullException">Thrown when writer is null.</exception>
    public void Serialize(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.Write(LeafCount);
        writer.Write(PlotId);
        writer.Write(PlotHeaderHash);
        writer.Write(Version);
    }

    /// <summary>
    /// Deserializes metadata from a <see cref="BinaryReader"/>.
    /// </summary>
    /// <param name="reader">The binary reader to deserialize from.</param>
    /// <returns>A new <see cref="BlockPlotMetadata"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when reader is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static BlockPlotMetadata Deserialize(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var leafCount = reader.ReadInt64();
        var plotId = reader.ReadBytes(HashSize);
        var plotHeaderHash = reader.ReadBytes(HashSize);
        var version = reader.ReadByte();

        if (plotId.Length != HashSize)
        {
            throw new InvalidOperationException("Failed to read plot ID: unexpected end of stream");
        }

        if (plotHeaderHash.Length != HashSize)
        {
            throw new InvalidOperationException("Failed to read plot header hash: unexpected end of stream");
        }

        return Create(leafCount, plotId, plotHeaderHash, version);
    }

    /// <summary>
    /// Gets the serialized size of the metadata in bytes.
    /// </summary>
    public static int SerializedSize => sizeof(long) + HashSize + HashSize + sizeof(byte);
}
