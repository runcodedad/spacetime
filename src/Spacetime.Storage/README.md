# Spacetime.Storage

Persistent blockchain storage layer using RocksDB for the Spacetime blockchain project.

## Overview

`Spacetime.Storage` provides a high-performance, reliable storage layer for blockchain data using RocksDB. It implements the **account model** for chain state management and organizes data using column families for optimal performance and maintainability.

## Features

- **RocksDB-based persistence** - Industry-standard embedded database
- **Account model support** - Modern approach for chain state with balances and nonces
- **Column family organization** - Logical separation of data types
- **Atomic operations** - Batch writes for consistency
- **Efficient lookups** - Fast access by hash, height, or address
- **Database integrity** - Corruption detection and health checks
- **Database compaction** - Space reclamation and performance optimization

## Architecture

### Column Families

Data is organized into separate column families for performance and clarity:

- **blocks**: Block headers and bodies indexed by hash
- **heights**: Block height to hash mapping
- **transactions**: Transaction index (hash to block location)
- **accounts**: Account state (address to balance/nonce)
- **metadata**: Chain metadata (best block, chain height)

### Storage Interfaces

#### IChainStorage
Main storage interface providing access to all sub-storages and atomic operations.

```csharp
var storage = RocksDbChainStorage.Open("/path/to/db");
await storage.Blocks.StoreBlockAsync(block);
await storage.Accounts.StoreAccountAsync(address, account);
await storage.DisposeAsync();
```

#### IBlockStorage
Stores and retrieves blocks, headers, and bodies.

```csharp
// Store a complete block
await storage.Blocks.StoreBlockAsync(block);

// Retrieve by hash
var block = await storage.Blocks.GetBlockByHashAsync(blockHash);

// Retrieve by height
var block = await storage.Blocks.GetBlockByHeightAsync(100);

// Check existence
bool exists = await storage.Blocks.ExistsAsync(blockHash);
```

#### ITransactionIndex
Indexes transactions for fast lookup.

```csharp
// Index a transaction
await storage.Transactions.IndexTransactionAsync(
    txHash, blockHash, blockHeight, txIndex);

// Get transaction location
var location = await storage.Transactions.GetTransactionLocationAsync(txHash);

// Get full transaction
var tx = await storage.Transactions.GetTransactionAsync(txHash);
```

#### IAccountStorage
Stores account state for the account model.

```csharp
// Store account state
var account = new AccountState(Balance: 1000, Nonce: 1);
await storage.Accounts.StoreAccountAsync(address, account);

// Retrieve account
var account = await storage.Accounts.GetAccountAsync(address);

// Check existence
bool exists = await storage.Accounts.ExistsAsync(address);

// Delete account
await storage.Accounts.DeleteAccountAsync(address);
```

#### IChainMetadata
Manages chain metadata like best block and height.

```csharp
// Set best block
await storage.Metadata.SetBestBlockHashAsync(blockHash);
await storage.Metadata.SetChainHeightAsync(height);

// Get best block
var bestHash = await storage.Metadata.GetBestBlockHashAsync();
var height = await storage.Metadata.GetChainHeightAsync();
```

### Atomic Operations

Use write batches for atomic multi-operation commits:

```csharp
using var batch = storage.CreateWriteBatch();

// Add multiple operations
batch.Put(key1, value1, "blocks");
batch.Put(key2, value2, "accounts");
batch.Delete(key3, "metadata");

// Commit atomically
await storage.CommitBatchAsync(batch);
```

### Database Maintenance

```csharp
// Compact database
await storage.CompactAsync();

// Check integrity
bool isHealthy = await storage.CheckIntegrityAsync();
```

## Data Model

### Account State

Accounts follow the account model with:
- **Balance**: Account balance (int64)
- **Nonce**: Transaction counter for replay protection (int64)

Future extensions may include:
- Contract code and storage
- Staking information
- Additional metadata

### Binary Serialization

All data is serialized using little-endian binary format for:
- Cross-platform compatibility
- Efficient storage
- Fast serialization/deserialization

## Installation & Dependencies

### NuGet Packages

```xml
<PackageReference Include="RocksDB" Version="10.4.2.62659" />
```

This package includes both the C# bindings and native RocksDB libraries for all platforms (Windows, Linux, macOS).

## Usage Example

```csharp
using Spacetime.Storage;
using Spacetime.Core;

// Open database
var storage = RocksDbChainStorage.Open("./blockchain-data");

try
{
    // Store a block
    var block = CreateBlock();
    await storage.Blocks.StoreBlockAsync(block);
    
    // Index transactions
    for (int i = 0; i < block.Body.Transactions.Count; i++)
    {
        var tx = block.Body.Transactions[i];
        var txHash = tx.ComputeHash();
        var blockHash = block.Header.ComputeHash();
        
        await storage.Transactions.IndexTransactionAsync(
            txHash, blockHash, block.Header.Height, i);
    }
    
    // Update account states
    var account = new AccountState(Balance: 1000, Nonce: 1);
    await storage.Accounts.StoreAccountAsync(minerAddress, account);
    
    // Update chain metadata
    await storage.Metadata.SetBestBlockHashAsync(block.Header.ComputeHash());
    await storage.Metadata.SetChainHeightAsync(block.Header.Height);
    
    // Retrieve data
    var retrievedBlock = await storage.Blocks.GetBlockByHeightAsync(block.Header.Height);
    var tx = await storage.Transactions.GetTransactionAsync(txHash);
    var account = await storage.Accounts.GetAccountAsync(minerAddress);
}
finally
{
    await storage.DisposeAsync();
}
```

## Performance Considerations

- **Batch writes**: Use `IWriteBatch` for multiple operations
- **Column families**: Data is organized for optimal access patterns
- **Async operations**: All I/O is async to avoid blocking
- **Disposal**: Always dispose storage to flush data and release resources
- **Compaction**: Periodically compact to reclaim space and improve performance

## Testing

Unit tests verify all storage operations:

```bash
dotnet test tests/Spacetime.Storage.Tests
```

Integration tests verify real RocksDB operations:

```bash
dotnet test tests/Spacetime.Storage.IntegrationTests
```

## Migration Strategy

When schema changes are needed:

1. Version the database format
2. Implement migration code
3. Support backward compatibility when possible
4. Document migration steps

## Related Projects

- [Spacetime.Core](../Spacetime.Core/README.md) - Core blockchain data structures
- [Spacetime.Consensus](../Spacetime.Consensus/README.md) - Consensus and validation
- [Spacetime.Network](../Spacetime.Network/README.md) - P2P networking (future)

## References

- [RocksDB Documentation](https://github.com/facebook/rocksdb/wiki)
- [RocksDB-Sharp](https://github.com/curiosity-ai/rocksdb-sharp)
- [Account Model vs UTXO](https://ethereum.org/en/developers/docs/accounts/)
