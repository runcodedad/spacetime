using RocksDbSharp;
using Spacetime.Core;
using System.Buffers.Binary;

namespace Spacetime.Storage;

/// <summary>
/// RocksDB implementation of block storage.
/// </summary>
internal sealed class RocksDbBlockStorage : IBlockStorage
{
    private const string BlocksColumnFamily = "blocks";
    private const string HeightsColumnFamily = "heights";
    private const string HeaderPrefix = "h:";
    private const string BodyPrefix = "b:";

    private readonly RocksDb _db;
    private readonly ColumnFamilyHandle _blocksCf;
    private readonly ColumnFamilyHandle _heightsCf;

    public RocksDbBlockStorage(RocksDb db, Dictionary<string, ColumnFamilyHandle> columnFamilies)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(columnFamilies);

        _db = db;
        _blocksCf = columnFamilies[BlocksColumnFamily];
        _heightsCf = columnFamilies[HeightsColumnFamily];
    }

    public void StoreHeader(BlockHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);

        var hash = header.ComputeHash();
        var key = MakeHeaderKey(hash);
        var value = header.Serialize();

        _db.Put(key, value, _blocksCf);

        // Also store height-to-hash mapping
        var heightKey = MakeHeightKey(header.Height);
        _db.Put(heightKey, hash, _heightsCf);
    }

    public void StoreBody(ReadOnlyMemory<byte> hash, BlockBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (hash.Length != 32)
        {
            throw new ArgumentException("Hash must be 32 bytes.", nameof(hash));
        }

        var key = MakeBodyKey(hash.Span);
        var value = SerializeBody(body);

        _db.Put(key, value, _blocksCf);
    }

    public void StoreBlock(Block block)
    {
        ArgumentNullException.ThrowIfNull(block);

        var hash = block.Header.ComputeHash();
        
        StoreHeader(block.Header);
        StoreBody(hash, block.Body);
    }

    public BlockHeader? GetHeaderByHash(ReadOnlyMemory<byte> hash)
    {
        if (hash.Length != 32)
        {
            throw new ArgumentException("Hash must be 32 bytes.", nameof(hash));
        }

        var key = MakeHeaderKey(hash.Span);
        var value = _db.Get(key, _blocksCf);

        if (value == null)
        {
            return null;
        }

        using var ms = new MemoryStream(value);
        using var reader = new BinaryReader(ms);
        return BlockHeader.Deserialize(reader);
    }

    public BlockHeader? GetHeaderByHeight(long height)
    {
        if (height < 0)
        {
            throw new ArgumentException("Height must be non-negative.", nameof(height));
        }

        var heightKey = MakeHeightKey(height);
        var hash = _db.Get(heightKey, _heightsCf);

        if (hash == null)
        {
            return null;
        }

        return GetHeaderByHash(hash);
    }

    public BlockBody? GetBodyByHash(ReadOnlyMemory<byte> hash)
    {
        if (hash.Length != 32)
        {
            throw new ArgumentException("Hash must be 32 bytes.", nameof(hash));
        }

        var key = MakeBodyKey(hash.Span);
        var value = _db.Get(key, _blocksCf);

        if (value == null)
        {
            return null;
        }

        return DeserializeBody(value);
    }

    public Block? GetBlockByHash(ReadOnlyMemory<byte> hash)
    {
        if (hash.Length != 32)
        {
            throw new ArgumentException("Hash must be 32 bytes.", nameof(hash));
        }

        var header = GetHeaderByHash(hash);
        if (header == null)
        {
            return null;
        }

        var body = GetBodyByHash(hash);
        if (body == null)
        {
            return null;
        }

        return new Block(header, body);
    }

    public Block? GetBlockByHeight(long height)
    {
        if (height < 0)
        {
            throw new ArgumentException("Height must be non-negative.", nameof(height));
        }

        var header = GetHeaderByHeight(height);
        if (header == null)
        {
            return null;
        }

        var hash = header.ComputeHash();
        var body = GetBodyByHash(hash);
        if (body == null)
        {
            return null;
        }

        return new Block(header, body);
    }

    public bool Exists(ReadOnlyMemory<byte> hash)
    {
        if (hash.Length != 32)
        {
            throw new ArgumentException("Hash must be 32 bytes.", nameof(hash));
        }

        var key = MakeHeaderKey(hash.Span);
        var value = _db.Get(key, _blocksCf);

        return value != null;
    }

    private static byte[] MakeHeaderKey(ReadOnlySpan<byte> hash)
    {
        var key = new byte[HeaderPrefix.Length + hash.Length];
        System.Text.Encoding.ASCII.GetBytes(HeaderPrefix).CopyTo(key, 0);
        hash.CopyTo(key.AsSpan(HeaderPrefix.Length));
        return key;
    }

    private static byte[] MakeBodyKey(ReadOnlySpan<byte> hash)
    {
        var key = new byte[BodyPrefix.Length + hash.Length];
        System.Text.Encoding.ASCII.GetBytes(BodyPrefix).CopyTo(key, 0);
        hash.CopyTo(key.AsSpan(BodyPrefix.Length));
        return key;
    }

    private static byte[] MakeHeightKey(long height)
    {
        var key = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(key, height);
        return key;
    }

    private static byte[] SerializeBody(BlockBody body)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        body.Serialize(writer);
        return ms.ToArray();
    }

    private static BlockBody DeserializeBody(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        return BlockBody.Deserialize(reader);
    }
}
