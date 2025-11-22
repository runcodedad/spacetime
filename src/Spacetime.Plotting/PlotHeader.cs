namespace Spacetime.Plotting;

/// <summary>
/// Represents the header of a plot file containing metadata and configuration.
/// </summary>
public sealed class PlotHeader
{
    /// <summary>
    /// Magic bytes identifying a valid Spacetime plot file: "SPTP" (Spacetime Plot)
    /// </summary>
    public static readonly byte[] MagicBytes = [0x53, 0x50, 0x54, 0x50]; // "SPTP"

    /// <summary>
    /// Current plot file format version
    /// </summary>
    public const byte FormatVersion = 1;

    /// <summary>
    /// Size of the header in bytes (without checksum)
    /// </summary>
    public const int HeaderSize = 4 + 1 + 32 + 8 + 4 + 8 + 32; // 89 bytes

    /// <summary>
    /// Size of the SHA256 checksum in bytes
    /// </summary>
    public const int ChecksumSize = 32;

    /// <summary>
    /// Total size including checksum
    /// </summary>
    public const int TotalHeaderSize = HeaderSize + ChecksumSize;

    /// <summary>
    /// Gets the plot seed used for deterministic generation
    /// </summary>
    public byte[] PlotSeed { get; }

    /// <summary>
    /// Gets the total number of leaves in the plot
    /// </summary>
    public long LeafCount { get; }

    /// <summary>
    /// Gets the size of each leaf entry in bytes
    /// </summary>
    public int LeafSize { get; }

    /// <summary>
    /// Gets the Merkle tree height
    /// </summary>
    public long TreeHeight { get; }

    /// <summary>
    /// Gets the Merkle root hash
    /// </summary>
    public byte[] MerkleRoot { get; }

    /// <summary>
    /// Gets the header checksum (SHA256 of all header fields)
    /// </summary>
    public byte[]? Checksum { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlotHeader"/> class.
    /// </summary>
    public PlotHeader(byte[] plotSeed, long leafCount, int leafSize, long treeHeight, byte[] merkleRoot)
    {
        ArgumentNullException.ThrowIfNull(plotSeed);
        ArgumentNullException.ThrowIfNull(merkleRoot);

        if (plotSeed.Length != 32)
        {
            throw new ArgumentException("Plot seed must be 32 bytes", nameof(plotSeed));
        }

        if (leafCount <= 0)
        {
            throw new ArgumentException("Leaf count must be positive", nameof(leafCount));
        }

        if (leafSize <= 0)
        {
            throw new ArgumentException("Leaf size must be positive", nameof(leafSize));
        }

        if (merkleRoot.Length != 32)
        {
            throw new ArgumentException("Merkle root must be 32 bytes", nameof(merkleRoot));
        }

        PlotSeed = plotSeed;
        LeafCount = leafCount;
        LeafSize = leafSize;
        TreeHeight = treeHeight;
        MerkleRoot = merkleRoot;
    }

    /// <summary>
    /// Computes and sets the checksum for this header.
    /// </summary>
    public void ComputeChecksum()
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var headerBytes = SerializeWithoutChecksum();
        Checksum = sha256.ComputeHash(headerBytes);
    }

    /// <summary>
    /// Verifies that the checksum matches the header contents.
    /// </summary>
    public bool VerifyChecksum()
    {
        if (Checksum == null)
        {
            return false;
        }

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var headerBytes = SerializeWithoutChecksum();
        var expectedChecksum = sha256.ComputeHash(headerBytes);

        return Checksum.SequenceEqual(expectedChecksum);
    }

    /// <summary>
    /// Serializes the header without the checksum.
    /// </summary>
    private byte[] SerializeWithoutChecksum()
    {
        var buffer = new byte[HeaderSize];
        var offset = 0;

        // Magic bytes
        Array.Copy(MagicBytes, 0, buffer, offset, MagicBytes.Length);
        offset += MagicBytes.Length;

        // Version
        buffer[offset++] = FormatVersion;

        // Plot seed
        Array.Copy(PlotSeed, 0, buffer, offset, PlotSeed.Length);
        offset += PlotSeed.Length;

        // Leaf count
        BitConverter.TryWriteBytes(buffer.AsSpan(offset), LeafCount);
        offset += sizeof(long);

        // Leaf size
        BitConverter.TryWriteBytes(buffer.AsSpan(offset), LeafSize);
        offset += sizeof(int);

        // Tree height
        BitConverter.TryWriteBytes(buffer.AsSpan(offset), TreeHeight);
        offset += sizeof(long);

        // Merkle root
        Array.Copy(MerkleRoot, 0, buffer, offset, MerkleRoot.Length);
        offset += MerkleRoot.Length;

        return buffer;
    }

    /// <summary>
    /// Serializes the complete header including checksum.
    /// </summary>
    public byte[] Serialize()
    {
        if (Checksum == null)
        {
            throw new InvalidOperationException("Checksum must be computed before serialization");
        }

        var buffer = new byte[TotalHeaderSize];
        var headerBytes = SerializeWithoutChecksum();

        Array.Copy(headerBytes, 0, buffer, 0, HeaderSize);
        Array.Copy(Checksum, 0, buffer, HeaderSize, ChecksumSize);

        return buffer;
    }

    /// <summary>
    /// Deserializes a plot header from bytes.
    /// </summary>
    public static PlotHeader Deserialize(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < TotalHeaderSize)
        {
            throw new ArgumentException($"Data too short. Expected at least {TotalHeaderSize} bytes", nameof(data));
        }

        var offset = 0;

        // Verify magic bytes
        var magic = data.AsSpan(offset, MagicBytes.Length);
        if (!magic.SequenceEqual(MagicBytes))
        {
            throw new InvalidOperationException("Invalid plot file: magic bytes mismatch");
        }
        offset += MagicBytes.Length;

        // Verify version
        var version = data[offset++];
        if (version != FormatVersion)
        {
            throw new InvalidOperationException($"Unsupported plot file version: {version}");
        }

        // Read plot seed
        var plotSeed = new byte[32];
        Array.Copy(data, offset, plotSeed, 0, 32);
        offset += 32;

        // Read leaf count
        var leafCount = BitConverter.ToInt64(data, offset);
        offset += sizeof(long);

        // Read leaf size
        var leafSize = BitConverter.ToInt32(data, offset);
        offset += sizeof(int);

        // Read tree height
        var treeHeight = BitConverter.ToInt64(data, offset);
        offset += sizeof(long);

        // Read Merkle root
        var merkleRoot = new byte[32];
        Array.Copy(data, offset, merkleRoot, 0, 32);
        offset += 32;

        // Read checksum
        var checksum = new byte[ChecksumSize];
        Array.Copy(data, offset, checksum, 0, ChecksumSize);

        var header = new PlotHeader(plotSeed, leafCount, leafSize, treeHeight, merkleRoot)
        {
            Checksum = checksum
        };

        // Verify checksum
        if (!header.VerifyChecksum())
        {
            throw new InvalidOperationException("Plot header checksum verification failed");
        }

        return header;
    }
}
