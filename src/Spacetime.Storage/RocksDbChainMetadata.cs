using RocksDbSharp;
using System.Buffers.Binary;

namespace Spacetime.Storage;

/// <summary>
/// RocksDB implementation of chain metadata storage.
/// </summary>
internal sealed class RocksDbChainMetadata : IChainMetadata
{
    private const string MetadataColumnFamily = "metadata";
    private const string BestBlockHashKey = "best_block_hash";
    private const string ChainHeightKey = "chain_height";

    private readonly RocksDb _db;
    private readonly ColumnFamilyHandle _metadataCf;

    public RocksDbChainMetadata(RocksDb db, Dictionary<string, ColumnFamilyHandle> columnFamilies)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(columnFamilies);

        _db = db;
        _metadataCf = columnFamilies[MetadataColumnFamily];
    }

    public Task<ReadOnlyMemory<byte>?> GetBestBlockHashAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = System.Text.Encoding.UTF8.GetBytes(BestBlockHashKey);
        var value = _db.Get(key, _metadataCf);

        if (value == null)
        {
            return Task.FromResult<ReadOnlyMemory<byte>?>(null);
        }

        return Task.FromResult<ReadOnlyMemory<byte>?>(new ReadOnlyMemory<byte>(value));
    }

    public Task SetBestBlockHashAsync(ReadOnlyMemory<byte> hash, CancellationToken cancellationToken = default)
    {
        if (hash.Length != 32)
        {
            throw new ArgumentException("Hash must be 32 bytes.", nameof(hash));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var key = System.Text.Encoding.UTF8.GetBytes(BestBlockHashKey);
        _db.Put(key, hash.Span.ToArray(), _metadataCf);

        return Task.CompletedTask;
    }

    public Task<long?> GetChainHeightAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = System.Text.Encoding.UTF8.GetBytes(ChainHeightKey);
        var value = _db.Get(key, _metadataCf);

        if (value == null)
        {
            return Task.FromResult<long?>(null);
        }

        if (value.Length != 8)
        {
            throw new InvalidOperationException("Invalid chain height data.");
        }

        var height = BinaryPrimitives.ReadInt64LittleEndian(value);
        return Task.FromResult<long?>(height);
    }

    public Task SetChainHeightAsync(long height, CancellationToken cancellationToken = default)
    {
        if (height < 0)
        {
            throw new ArgumentException("Height must be non-negative.", nameof(height));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var key = System.Text.Encoding.UTF8.GetBytes(ChainHeightKey);
        var value = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(value, height);

        _db.Put(key, value, _metadataCf);

        return Task.CompletedTask;
    }
}
