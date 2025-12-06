using RocksDbSharp;
using System.Buffers.Binary;

namespace Spacetime.Storage;

/// <summary>
/// RocksDB-based implementation of blockchain storage.
/// </summary>
/// <remarks>
/// Uses column families to organize data:
/// - blocks: Block headers and bodies indexed by hash
/// - heights: Block height to hash mapping
/// - transactions: Transaction index
/// - accounts: Account state
/// - metadata: Chain metadata (best block, height, etc.)
/// </remarks>
public sealed class RocksDbChainStorage : IChainStorage
{
    private const string BlocksColumnFamily = "blocks";
    private const string HeightsColumnFamily = "heights";
    private const string TransactionsColumnFamily = "transactions";
    private const string AccountsColumnFamily = "accounts";
    private const string MetadataColumnFamily = "metadata";

    private readonly RocksDb _db;
    private readonly Dictionary<string, ColumnFamilyHandle> _columnFamilies;
    private readonly RocksDbBlockStorage _blockStorage;
    private readonly RocksDbTransactionIndex _transactionIndex;
    private readonly RocksDbAccountStorage _accountStorage;
    private readonly RocksDbChainMetadata _metadata;
    private bool _disposed;

    private RocksDbChainStorage(
        RocksDb db,
        Dictionary<string, ColumnFamilyHandle> columnFamilies)
    {
        _db = db;
        _columnFamilies = columnFamilies;
        _blockStorage = new RocksDbBlockStorage(db, columnFamilies);
        _transactionIndex = new RocksDbTransactionIndex(db, columnFamilies, _blockStorage);
        _accountStorage = new RocksDbAccountStorage(db, columnFamilies);
        _metadata = new RocksDbChainMetadata(db, columnFamilies);
    }

    /// <summary>
    /// Opens or creates a RocksDB chain storage at the specified path.
    /// </summary>
    /// <param name="path">The database path.</param>
    /// <returns>A new chain storage instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when path is null.</exception>
    /// <exception cref="ArgumentException">Thrown when path is empty or whitespace.</exception>
    public static RocksDbChainStorage Open(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be empty or whitespace.", nameof(path));
        }

        // Create column families if they don't exist
        var columnFamilies = new ColumnFamilies
        {
            { BlocksColumnFamily, new ColumnFamilyOptions() },
            { HeightsColumnFamily, new ColumnFamilyOptions() },
            { TransactionsColumnFamily, new ColumnFamilyOptions() },
            { AccountsColumnFamily, new ColumnFamilyOptions() },
            { MetadataColumnFamily, new ColumnFamilyOptions() }
        };

        var options = new DbOptions()
            .SetCreateIfMissing(true)
            .SetCreateMissingColumnFamilies(true);

        var db = RocksDb.Open(options, path, columnFamilies);

        var cfHandles = new Dictionary<string, ColumnFamilyHandle>
        {
            [BlocksColumnFamily] = db.GetColumnFamily(BlocksColumnFamily),
            [HeightsColumnFamily] = db.GetColumnFamily(HeightsColumnFamily),
            [TransactionsColumnFamily] = db.GetColumnFamily(TransactionsColumnFamily),
            [AccountsColumnFamily] = db.GetColumnFamily(AccountsColumnFamily),
            [MetadataColumnFamily] = db.GetColumnFamily(MetadataColumnFamily)
        };

        return new RocksDbChainStorage(db, cfHandles);
    }

    public IBlockStorage Blocks => _blockStorage;
    public ITransactionIndex Transactions => _transactionIndex;
    public IAccountStorage Accounts => _accountStorage;
    public IChainMetadata Metadata => _metadata;

    public IWriteBatch CreateWriteBatch()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new RocksDbWriteBatch(_db, new WriteBatch(), _columnFamilies);
    }

    public void CommitBatch(IWriteBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (batch is not RocksDbWriteBatch rocksDbBatch)
        {
            throw new ArgumentException("Batch must be created by this storage instance.", nameof(batch));
        }

        _db.Write(rocksDbBatch.GetWriteBatch());
    }

    public void Compact()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Compact all column families
        foreach (var cf in _columnFamilies.Values)
        {
            _db.CompactRange(null, null, cf);
        }
    }

    public bool CheckIntegrity()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            // Try to get a property - if database is corrupted, this will throw
            _db.GetProperty("rocksdb.stats");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _db.Dispose();
        _disposed = true;

        await Task.CompletedTask;
    }
}
