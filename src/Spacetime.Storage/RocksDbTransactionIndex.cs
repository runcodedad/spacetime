using RocksDbSharp;
using Spacetime.Core;
using System.Buffers.Binary;

namespace Spacetime.Storage;

/// <summary>
/// RocksDB implementation of transaction index.
/// </summary>
internal sealed class RocksDbTransactionIndex : ITransactionIndex
{
    private const string TransactionsColumnFamily = "transactions";

    private readonly RocksDb _db;
    private readonly ColumnFamilyHandle _transactionsCf;
    private readonly IBlockStorage _blockStorage;

    public RocksDbTransactionIndex(
        RocksDb db,
        Dictionary<string, ColumnFamilyHandle> columnFamilies,
        IBlockStorage blockStorage)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(columnFamilies);
        ArgumentNullException.ThrowIfNull(blockStorage);

        _db = db;
        _transactionsCf = columnFamilies[TransactionsColumnFamily];
        _blockStorage = blockStorage;
    }

    public Task IndexTransactionAsync(
        ReadOnlyMemory<byte> txHash,
        ReadOnlyMemory<byte> blockHash,
        long blockHeight,
        int txIndex,
        CancellationToken cancellationToken = default)
    {
        if (txHash.Length != 32)
        {
            throw new ArgumentException("Transaction hash must be 32 bytes.", nameof(txHash));
        }
        if (blockHash.Length != 32)
        {
            throw new ArgumentException("Block hash must be 32 bytes.", nameof(blockHash));
        }
        if (blockHeight < 0)
        {
            throw new ArgumentException("Block height must be non-negative.", nameof(blockHeight));
        }
        if (txIndex < 0)
        {
            throw new ArgumentException("Transaction index must be non-negative.", nameof(txIndex));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var value = SerializeLocation(blockHash, blockHeight, txIndex);
        _db.Put(txHash.Span.ToArray(), value, _transactionsCf);

        return Task.CompletedTask;
    }

    public Task<TransactionLocation?> GetTransactionLocationAsync(
        ReadOnlyMemory<byte> txHash,
        CancellationToken cancellationToken = default)
    {
        if (txHash.Length != 32)
        {
            throw new ArgumentException("Transaction hash must be 32 bytes.", nameof(txHash));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var value = _db.Get(txHash.Span.ToArray(), _transactionsCf);

        if (value == null)
        {
            return Task.FromResult<TransactionLocation?>(null);
        }

        var location = DeserializeLocation(value);
        return Task.FromResult<TransactionLocation?>(location);
    }

    public async Task<Transaction?> GetTransactionAsync(
        ReadOnlyMemory<byte> txHash,
        CancellationToken cancellationToken = default)
    {
        if (txHash.Length != 32)
        {
            throw new ArgumentException("Transaction hash must be 32 bytes.", nameof(txHash));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var location = await GetTransactionLocationAsync(txHash, cancellationToken);
        if (location == null)
        {
            return null;
        }

        var body = await _blockStorage.GetBodyByHashAsync(location.BlockHash, cancellationToken);
        if (body == null)
        {
            return null;
        }

        if (location.TransactionIndex >= body.Transactions.Count)
        {
            return null;
        }

        return body.Transactions[location.TransactionIndex];
    }

    private static byte[] SerializeLocation(ReadOnlyMemory<byte> blockHash, long blockHeight, int txIndex)
    {
        var buffer = new byte[32 + 8 + 4]; // hash + height + index
        blockHash.Span.CopyTo(buffer);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(32), blockHeight);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(40), txIndex);
        return buffer;
    }

    private static TransactionLocation DeserializeLocation(byte[] data)
    {
        if (data.Length != 44)
        {
            throw new InvalidOperationException("Invalid transaction location data.");
        }

        var blockHash = new ReadOnlyMemory<byte>(data, 0, 32);
        var blockHeight = BinaryPrimitives.ReadInt64LittleEndian(data.AsSpan(32));
        var txIndex = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(40));

        return new TransactionLocation(blockHash, blockHeight, txIndex);
    }
}
