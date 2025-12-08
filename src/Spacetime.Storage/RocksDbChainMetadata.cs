using RocksDbSharp;
using System.Buffers.Binary;

namespace Spacetime.Storage;

/// <summary>
/// RocksDB implementation of chain metadata storage.
/// </summary>
internal sealed class RocksDbChainMetadata : IChainMetadata
{
    private const string _metadataColumnFamily = "metadata";
    private const string _bestBlockHashKey = "best_block_hash";
    private const string _chainHeightKey = "chain_height";
    private const string _cumulativeDifficultyPrefix = "cumulative_difficulty:";

    private readonly RocksDb _db;
    private readonly ColumnFamilyHandle _metadataCf;

    public RocksDbChainMetadata(RocksDb db, Dictionary<string, ColumnFamilyHandle> columnFamilies)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(columnFamilies);

        _db = db;
        _metadataCf = columnFamilies[_metadataColumnFamily];
    }

    public ReadOnlyMemory<byte>? GetBestBlockHash()
    {
        var key = System.Text.Encoding.UTF8.GetBytes(_bestBlockHashKey);
        var value = _db.Get(key, _metadataCf);

        if (value == null)
        {
            return null;
        }

        return new ReadOnlyMemory<byte>(value);
    }

    public void SetBestBlockHash(ReadOnlyMemory<byte> hash)
    {
        if (hash.Length != 32)
        {
            throw new ArgumentException("Hash must be 32 bytes.", nameof(hash));
        }

        var key = System.Text.Encoding.UTF8.GetBytes(_bestBlockHashKey);
        _db.Put(key, hash.Span.ToArray(), _metadataCf);
    }

    public long? GetChainHeight()
    {
        var key = System.Text.Encoding.UTF8.GetBytes(_chainHeightKey);
        var value = _db.Get(key, _metadataCf);

        if (value == null)
        {
            return null;
        }

        if (value.Length != 8)
        {
            throw new InvalidOperationException("Invalid chain height data.");
        }

        var height = BinaryPrimitives.ReadInt64LittleEndian(value);
        return height;
    }

    public void SetChainHeight(long height)
    {
        if (height < 0)
        {
            throw new ArgumentException("Height must be non-negative.", nameof(height));
        }

        var key = System.Text.Encoding.UTF8.GetBytes(_chainHeightKey);
        var value = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(value, height);

        _db.Put(key, value, _metadataCf);
    }

    public long? GetCumulativeDifficulty(ReadOnlyMemory<byte> blockHash)
    {
        if (blockHash.Length != 32)
        {
            throw new ArgumentException("Block hash must be 32 bytes.", nameof(blockHash));
        }

        var key = MakeCumulativeDifficultyKey(blockHash.Span);
        var value = _db.Get(key, _metadataCf);

        if (value == null)
        {
            return null;
        }

        if (value.Length != 8)
        {
            throw new InvalidOperationException("Invalid cumulative difficulty data.");
        }

        return BinaryPrimitives.ReadInt64LittleEndian(value);
    }

    public void SetCumulativeDifficulty(ReadOnlyMemory<byte> blockHash, long cumulativeDifficulty)
    {
        if (blockHash.Length != 32)
        {
            throw new ArgumentException("Block hash must be 32 bytes.", nameof(blockHash));
        }

        if (cumulativeDifficulty < 0)
        {
            throw new ArgumentException("Cumulative difficulty must be non-negative.", nameof(cumulativeDifficulty));
        }

        var key = MakeCumulativeDifficultyKey(blockHash.Span);
        var value = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(value, cumulativeDifficulty);

        _db.Put(key, value, _metadataCf);
    }

    private static byte[] MakeCumulativeDifficultyKey(ReadOnlySpan<byte> blockHash)
    {
        var prefix = System.Text.Encoding.UTF8.GetBytes(_cumulativeDifficultyPrefix);
        var key = new byte[prefix.Length + blockHash.Length];
        prefix.CopyTo(key, 0);
        blockHash.CopyTo(key.AsSpan(prefix.Length));
        return key;
    }
}
