using RocksDbSharp;

namespace Spacetime.Storage;

/// <summary>
/// RocksDB implementation of write batch for atomic operations.
/// </summary>
internal sealed class RocksDbWriteBatch : IWriteBatch
{
    private readonly WriteBatch _batch;
    private readonly RocksDb _db;
    private readonly Dictionary<string, ColumnFamilyHandle> _columnFamilies;
    private bool _disposed;

    public RocksDbWriteBatch(RocksDb db, WriteBatch batch, Dictionary<string, ColumnFamilyHandle> columnFamilies)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(columnFamilies);

        _db = db;
        _batch = batch;
        _columnFamilies = columnFamilies;
    }

    public void Put(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, string? columnFamily = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (columnFamily != null)
        {
            if (!_columnFamilies.TryGetValue(columnFamily, out var cf))
            {
                throw new ArgumentException($"Column family '{columnFamily}' not found.", nameof(columnFamily));
            }
            _batch.Put(key.ToArray(), value.ToArray(), cf);
        }
        else
        {
            _batch.Put(key.ToArray(), value.ToArray());
        }
    }

    public void Delete(ReadOnlySpan<byte> key, string? columnFamily = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (columnFamily != null)
        {
            if (!_columnFamilies.TryGetValue(columnFamily, out var cf))
            {
                throw new ArgumentException($"Column family '{columnFamily}' not found.", nameof(columnFamily));
            }
            _batch.Delete(key.ToArray(), cf);
        }
        else
        {
            _batch.Delete(key.ToArray());
        }
    }

    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _batch.Clear();
    }

    internal WriteBatch GetWriteBatch() => _batch;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _batch.Dispose();
        _disposed = true;
    }
}
