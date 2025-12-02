namespace Spacetime.Core;

/// <summary>
/// Represents a Proof-of-Space-Time proof included in a block.
/// </summary>
/// <remarks>
/// A block proof contains:
/// - The leaf value that produced the best score
/// - The leaf's position (index) in the plot
/// - Merkle proof data (sibling hashes and orientation bits)
/// - Metadata about the plot file
/// 
/// The proof can be verified by:
/// 1. Recomputing score = H(challenge || leaf)
/// 2. Verifying the Merkle proof shows leaf is in tree with given root
/// </remarks>
public sealed class BlockProof
{
    /// <summary>
    /// Size of hash values in bytes (SHA256).
    /// </summary>
    private const int HashSize = 32;

    /// <summary>
    /// Gets the leaf value that produced this proof.
    /// </summary>
    public byte[] LeafValue { get; }

    /// <summary>
    /// Gets the zero-based index of the leaf in the plot.
    /// </summary>
    public long LeafIndex { get; }

    /// <summary>
    /// Gets the sibling hashes for the Merkle proof path.
    /// </summary>
    /// <remarks>
    /// Each entry is a 32-byte hash. The list length equals the tree height.
    /// </remarks>
    public IReadOnlyList<byte[]> MerkleProofPath { get; }

    /// <summary>
    /// Gets the orientation bits for the Merkle proof path.
    /// </summary>
    /// <remarks>
    /// Each bit indicates whether the sibling is on the left (false) or right (true).
    /// The length equals the tree height.
    /// </remarks>
    public IReadOnlyList<bool> OrientationBits { get; }

    /// <summary>
    /// Gets the plot metadata associated with this proof.
    /// </summary>
    public BlockPlotMetadata PlotMetadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockProof"/> class.
    /// </summary>
    /// <param name="leafValue">The 32-byte leaf value.</param>
    /// <param name="leafIndex">The zero-based index of the leaf in the plot.</param>
    /// <param name="merkleProofPath">The sibling hashes for the Merkle proof.</param>
    /// <param name="orientationBits">The orientation bits for the Merkle proof.</param>
    /// <param name="plotMetadata">The metadata about the plot file.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when arguments have invalid values.</exception>
    public BlockProof(
        ReadOnlySpan<byte> leafValue,
        long leafIndex,
        IReadOnlyList<byte[]> merkleProofPath,
        IReadOnlyList<bool> orientationBits,
        BlockPlotMetadata plotMetadata)
    {
        ArgumentNullException.ThrowIfNull(merkleProofPath);
        ArgumentNullException.ThrowIfNull(orientationBits);
        ArgumentNullException.ThrowIfNull(plotMetadata);

        if (leafValue.Length != HashSize)
        {
            throw new ArgumentException($"Leaf value must be {HashSize} bytes", nameof(leafValue));
        }

        if (leafIndex < 0)
        {
            throw new ArgumentException("Leaf index must be non-negative", nameof(leafIndex));
        }

        if (merkleProofPath.Count != orientationBits.Count)
        {
            throw new ArgumentException(
                "Merkle proof path and orientation bits must have the same count",
                nameof(orientationBits));
        }

        foreach (var hash in merkleProofPath)
        {
            if (hash == null || hash.Length != HashSize)
            {
                throw new ArgumentException(
                    $"All Merkle proof hashes must be {HashSize} bytes",
                    nameof(merkleProofPath));
            }
        }

        LeafValue = leafValue.ToArray();
        LeafIndex = leafIndex;
        MerkleProofPath = new List<byte[]>(merkleProofPath.Select(h => (byte[])h.Clone())).AsReadOnly();
        OrientationBits = new List<bool>(orientationBits).AsReadOnly();
        PlotMetadata = plotMetadata;
    }

    /// <summary>
    /// Serializes the proof using a <see cref="BinaryWriter"/>.
    /// </summary>
    /// <param name="writer">The binary writer to serialize to.</param>
    /// <exception cref="ArgumentNullException">Thrown when writer is null.</exception>
    public void Serialize(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        // Write leaf value
        writer.Write(LeafValue);

        // Write leaf index
        writer.Write(LeafIndex);

        // Write Merkle proof path count and hashes
        writer.Write(MerkleProofPath.Count);
        foreach (var hash in MerkleProofPath)
        {
            writer.Write(hash);
        }

        // Write orientation bits as packed bytes
        var bitCount = OrientationBits.Count;
        writer.Write(bitCount);
        for (var i = 0; i < bitCount; i++)
        {
            writer.Write(OrientationBits[i]);
        }

        // Write plot metadata
        PlotMetadata.Serialize(writer);
    }

    /// <summary>
    /// Deserializes a proof from a <see cref="BinaryReader"/>.
    /// </summary>
    /// <param name="reader">The binary reader to deserialize from.</param>
    /// <returns>A new <see cref="BlockProof"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when reader is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public static BlockProof Deserialize(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        // Read leaf value
        var leafValue = reader.ReadBytes(HashSize);
        if (leafValue.Length != HashSize)
        {
            throw new InvalidOperationException("Failed to read leaf value: unexpected end of stream");
        }

        // Read leaf index
        var leafIndex = reader.ReadInt64();

        // Read Merkle proof path
        var proofPathCount = reader.ReadInt32();
        if (proofPathCount < 0)
        {
            throw new InvalidOperationException("Invalid Merkle proof path count");
        }

        var merkleProofPath = new List<byte[]>(proofPathCount);
        for (var i = 0; i < proofPathCount; i++)
        {
            var hash = reader.ReadBytes(HashSize);
            if (hash.Length != HashSize)
            {
                throw new InvalidOperationException("Failed to read Merkle proof hash: unexpected end of stream");
            }
            merkleProofPath.Add(hash);
        }

        // Read orientation bits
        var bitCount = reader.ReadInt32();
        if (bitCount != proofPathCount)
        {
            throw new InvalidOperationException("Orientation bit count does not match Merkle proof path count");
        }

        var orientationBits = new List<bool>(bitCount);
        for (var i = 0; i < bitCount; i++)
        {
            orientationBits.Add(reader.ReadBoolean());
        }

        // Read plot metadata
        var plotMetadata = BlockPlotMetadata.Deserialize(reader);

        return new BlockProof(leafValue, leafIndex, merkleProofPath, orientationBits, plotMetadata);
    }
}
