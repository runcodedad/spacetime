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

    public Task StoreHeaderAsync(BlockHeader header, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(header);
        cancellationToken.ThrowIfCancellationRequested();

        var hash = header.ComputeHash();
        var key = MakeHeaderKey(hash);
        var value = header.Serialize();

        _db.Put(key, value, _blocksCf);

        // Also store height-to-hash mapping
        var heightKey = MakeHeightKey(header.Height);
        _db.Put(heightKey, hash, _heightsCf);

        return Task.CompletedTask;
    }

    public Task StoreBodyAsync(ReadOnlyMemory<byte> hash, BlockBody body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (hash.Length != 32)
        {
            throw new ArgumentException("Hash must be 32 bytes.", nameof(hash));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var key = MakeBodyKey(hash.Span);
        var value = SerializeBody(body);

        _db.Put(key, value, _blocksCf);

        return Task.CompletedTask;
    }

    public async Task StoreBlockAsync(Block block, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(block);
        cancellationToken.ThrowIfCancellationRequested();

        var hash = block.Header.ComputeHash();
        
        await StoreHeaderAsync(block.Header, cancellationToken);
        await StoreBodyAsync(hash, block.Body, cancellationToken);
    }

    public Task<BlockHeader?> GetHeaderByHashAsync(ReadOnlyMemory<byte> hash, CancellationToken cancellationToken = default)
    {
        if (hash.Length != 32)
        {
            throw new ArgumentException("Hash must be 32 bytes.", nameof(hash));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var key = MakeHeaderKey(hash.Span);
        var value = _db.Get(key, _blocksCf);

        if (value == null)
        {
            return Task.FromResult<BlockHeader?>(null);
        }

        using var ms = new MemoryStream(value);
        using var reader = new BinaryReader(ms);
        var header = BlockHeader.Deserialize(reader);
        return Task.FromResult<BlockHeader?>(header);
    }

    public async Task<BlockHeader?> GetHeaderByHeightAsync(long height, CancellationToken cancellationToken = default)
    {
        if (height < 0)
        {
            throw new ArgumentException("Height must be non-negative.", nameof(height));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var heightKey = MakeHeightKey(height);
        var hash = _db.Get(heightKey, _heightsCf);

        if (hash == null)
        {
            return null;
        }

        return await GetHeaderByHashAsync(hash, cancellationToken);
    }

    public Task<BlockBody?> GetBodyByHashAsync(ReadOnlyMemory<byte> hash, CancellationToken cancellationToken = default)
    {
        if (hash.Length != 32)
        {
            throw new ArgumentException("Hash must be 32 bytes.", nameof(hash));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var key = MakeBodyKey(hash.Span);
        var value = _db.Get(key, _blocksCf);

        if (value == null)
        {
            return Task.FromResult<BlockBody?>(null);
        }

        var body = DeserializeBody(value);
        return Task.FromResult<BlockBody?>(body);
    }

    public async Task<Block?> GetBlockByHashAsync(ReadOnlyMemory<byte> hash, CancellationToken cancellationToken = default)
    {
        if (hash.Length != 32)
        {
            throw new ArgumentException("Hash must be 32 bytes.", nameof(hash));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var header = await GetHeaderByHashAsync(hash, cancellationToken);
        if (header == null)
        {
            return null;
        }

        var body = await GetBodyByHashAsync(hash, cancellationToken);
        if (body == null)
        {
            return null;
        }

        return new Block(header, body);
    }

    public async Task<Block?> GetBlockByHeightAsync(long height, CancellationToken cancellationToken = default)
    {
        if (height < 0)
        {
            throw new ArgumentException("Height must be non-negative.", nameof(height));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var header = await GetHeaderByHeightAsync(height, cancellationToken);
        if (header == null)
        {
            return null;
        }

        var hash = header.ComputeHash();
        var body = await GetBodyByHashAsync(hash, cancellationToken);
        if (body == null)
        {
            return null;
        }

        return new Block(header, body);
    }

    public Task<bool> ExistsAsync(ReadOnlyMemory<byte> hash, CancellationToken cancellationToken = default)
    {
        if (hash.Length != 32)
        {
            throw new ArgumentException("Hash must be 32 bytes.", nameof(hash));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var key = MakeHeaderKey(hash.Span);
        var value = _db.Get(key, _blocksCf);

        return Task.FromResult(value != null);
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
