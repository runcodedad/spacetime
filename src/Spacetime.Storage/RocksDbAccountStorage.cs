using RocksDbSharp;
using System.Buffers.Binary;

namespace Spacetime.Storage;

/// <summary>
/// RocksDB implementation of account storage.
/// </summary>
internal sealed class RocksDbAccountStorage : IAccountStorage
{
    private const string AccountsColumnFamily = "accounts";

    private readonly RocksDb _db;
    private readonly ColumnFamilyHandle _accountsCf;

    public RocksDbAccountStorage(RocksDb db, Dictionary<string, ColumnFamilyHandle> columnFamilies)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(columnFamilies);

        _db = db;
        _accountsCf = columnFamilies[AccountsColumnFamily];
    }

    public void StoreAccount(ReadOnlyMemory<byte> address, AccountState account)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (address.Length == 0)
        {
            throw new ArgumentException("Address cannot be empty.", nameof(address));
        }

        var value = SerializeAccount(account);
        _db.Put(address.Span.ToArray(), value, _accountsCf);
    }

    public AccountState? GetAccount(ReadOnlyMemory<byte> address)
    {
        if (address.Length == 0)
        {
            throw new ArgumentException("Address cannot be empty.", nameof(address));
        }

        var value = _db.Get(address.Span.ToArray(), _accountsCf);

        if (value == null)
        {
            return null;
        }

        return DeserializeAccount(value);
    }

    public bool Exists(ReadOnlyMemory<byte> address)
    {
        if (address.Length == 0)
        {
            throw new ArgumentException("Address cannot be empty.", nameof(address));
        }

        var value = _db.Get(address.Span.ToArray(), _accountsCf);
        return value != null;
    }

    public void DeleteAccount(ReadOnlyMemory<byte> address)
    {
        if (address.Length == 0)
        {
            throw new ArgumentException("Address cannot be empty.", nameof(address));
        }

        _db.Remove(address.Span.ToArray(), _accountsCf);
    }

    private static byte[] SerializeAccount(AccountState account)
    {
        var buffer = new byte[16]; // 8 bytes for balance + 8 bytes for nonce
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(0), account.Balance);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(8), account.Nonce);
        return buffer;
    }

    private static AccountState DeserializeAccount(byte[] data)
    {
        if (data.Length != 16)
        {
            throw new InvalidOperationException("Invalid account data.");
        }

        var balance = BinaryPrimitives.ReadInt64LittleEndian(data.AsSpan(0));
        var nonce = BinaryPrimitives.ReadInt64LittleEndian(data.AsSpan(8));

        return new AccountState(balance, nonce);
    }
}
