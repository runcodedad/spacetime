using System.Security.Cryptography;

namespace Spacetime.Plotting;

/// <summary>
/// Represents the header of a plot file containing metadata and configuration.
/// </summary>
public sealed class PlotHeader
{
    /// <summary>
    /// Size of plot seed and Merkle root in bytes (SHA256)
    /// </summary>
    private const int _hashSize = 32;

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
    public const int HeaderSize = 
        4 +         // Magic bytes
        1 +         // Version
        _hashSize +  // Plot seed
        8 +         // Leaf count (long)
        4 +         // Leaf size (int)
        8 +         // Tree height (long)
        _hashSize;   // Merkle root

    /// <summary>
    /// Size of the SHA256 checksum in bytes
    /// </summary>
    public const int ChecksumSize = _hashSize;

    /// <summary>
    /// Total size including checksum
    /// </summary>
    public const int TotalHeaderSize = HeaderSize + ChecksumSize;

    private readonly byte[] _plotSeed = new byte[_hashSize];
    private readonly byte[] _merkleRoot = new byte[_hashSize];
    private byte[]? _checksum = new byte[ChecksumSize];

    /// <summary>
    /// Gets the plot seed used for deterministic generation
    /// </summary>
    public ReadOnlySpan<byte> PlotSeed => _plotSeed;

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
    public ReadOnlySpan<byte> MerkleRoot => _merkleRoot;

    /// <summary>
    /// Gets the header checksum (SHA256 of all header fields)
    /// </summary>
    public ReadOnlySpan<byte> Checksum => _checksum;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlotHeader"/> class.
    /// </summary>
    public PlotHeader(ReadOnlySpan<byte> plotSeed, long leafCount, int leafSize, long treeHeight, ReadOnlySpan<byte> merkleRoot)
    {
        if (plotSeed.Length != _hashSize)
        {
            throw new ArgumentException($"Plot seed must be {_hashSize} bytes", nameof(plotSeed));
        }

        if (leafCount <= 0)
        {
            throw new ArgumentException("Leaf count must be positive", nameof(leafCount));
        }

        if (leafSize <= 0)
        {
            throw new ArgumentException("Leaf size must be positive", nameof(leafSize));
        }

        if (merkleRoot.Length != _hashSize)
        {
            throw new ArgumentException($"Merkle root must be {_hashSize} bytes", nameof(merkleRoot));
        }

        plotSeed.CopyTo(_plotSeed);
        merkleRoot.CopyTo(_merkleRoot);

        LeafCount = leafCount;
        LeafSize = leafSize;
        TreeHeight = treeHeight;
    }

    /// <summary>
    /// Computes and sets the checksum for this header.
    /// </summary>
    public void ComputeChecksum()
    {
        var headerBytes = SerializeWithoutChecksum();
        SHA256.TryHashData(headerBytes, _checksum, out _);
    }

    /// <summary>
    /// Verifies that the checksum matches the header contents.
    /// </summary>
    public bool VerifyChecksum()
    {
        if (_checksum == null)
        {
            return false;
        }

        Span<byte> expectedChecksum = stackalloc byte[ChecksumSize];
        var headerBytes = SerializeWithoutChecksum();
        SHA256.TryHashData(headerBytes, expectedChecksum, out _);

        return _checksum.AsSpan().SequenceEqual(expectedChecksum);
    }

    /// <summary>
    /// Serializes the header without the checksum.
    /// </summary>
    private byte[] SerializeWithoutChecksum()
    {
        var buffer = new byte[HeaderSize];
        var offset = 0;

        // Magic bytes
        MagicBytes.CopyTo(buffer.AsSpan(offset));
        offset += MagicBytes.Length;

        // Version
        buffer[offset++] = FormatVersion;

        // Plot seed
        _plotSeed.CopyTo(buffer.AsSpan(offset));
        offset += _hashSize;

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
        _merkleRoot.CopyTo(buffer.AsSpan(offset));

        return buffer;
    }

    /// <summary>
    /// Serializes the complete header including checksum.
    /// </summary>
    public byte[] Serialize()
    {
        if (_checksum == null)
        {
            throw new InvalidOperationException("Checksum must be computed before serialization");
        }

        var buffer = new byte[TotalHeaderSize];
        var headerBytes = SerializeWithoutChecksum();

        headerBytes.CopyTo(buffer.AsSpan());
        _checksum.CopyTo(buffer.AsSpan(HeaderSize));

        return buffer;
    }

    /// <summary>
    /// Deserializes a plot header from bytes.
    /// </summary>
    public static PlotHeader Deserialize(ReadOnlySpan<byte> data)
    {
        if (data.Length < TotalHeaderSize)
        {
            throw new ArgumentException($"Data too short. Expected at least {TotalHeaderSize} bytes", nameof(data));
        }

        var offset = 0;

        // Verify magic bytes
        var magic = data.Slice(offset, MagicBytes.Length);
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
        var plotSeed = data.Slice(offset, _hashSize);
        offset += _hashSize;

        // Read leaf count
        var leafCount = BitConverter.ToInt64(data[offset..]);
        offset += sizeof(long);

        // Read leaf size
        var leafSize = BitConverter.ToInt32(data[offset..]);
        offset += sizeof(int);

        // Read tree height
        var treeHeight = BitConverter.ToInt64(data[offset..]);
        offset += sizeof(long);

        // Read Merkle root
        var merkleRoot = data.Slice(offset, _hashSize);
        offset += _hashSize;

        // Read checksum
        var checksum = data.Slice(offset, ChecksumSize);

        var header = new PlotHeader(plotSeed, leafCount, leafSize, treeHeight, merkleRoot)
        {
            _checksum = checksum.ToArray()
        };

        // Verify checksum
        if (!header.VerifyChecksum())
        {
            throw new InvalidOperationException("Plot header checksum verification failed");
        }

        return header;
    }
}
