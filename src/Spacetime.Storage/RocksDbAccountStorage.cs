using RocksDbSharp;

namespace Spacetime.Storage;

/// <summary>
/// RocksDB implementation of account storage.
/// </summary>
internal sealed class RocksDbAccountStorage : IAccountStorage
{
    private const string _accountsColumnFamily = "accounts";

    private readonly RocksDb _db;
    private readonly ColumnFamilyHandle _accountsCf;

    public RocksDbAccountStorage(RocksDb db, Dictionary<string, ColumnFamilyHandle> columnFamilies)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(columnFamilies);

        _db = db;
        _accountsCf = columnFamilies[_accountsColumnFamily];
    }

    public void StoreAccount(ReadOnlyMemory<byte> address, AccountState account)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (address.Length == 0)
        {
            throw new ArgumentException("Address cannot be empty.", nameof(address));
        }

        var value = account.Serialize();
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

        return AccountState.Deserialize(value);
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


}
