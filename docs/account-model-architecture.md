# Account Model Architecture

## Overview

Spacetime blockchain uses an **account-based state model** similar to Ethereum, rather than the UTXO model used by Bitcoin. This design was chosen for its extensibility, modern design patterns, and suitability for future smart contracts and decentralized applications.

## Architecture Components

### 1. Account State

Each account in Spacetime maintains:

- **Balance** (int64): The amount of coins the account holds
- **Nonce** (int64): A counter that increments with each transaction, preventing replay attacks

Future extensibility:
- **Code Hash** (reserved): For smart contract bytecode
- **Storage Root** (reserved): For contract storage

### 2. State Storage Layer

**Implementation**: `Spacetime.Storage.IAccountStorage` and `RocksDbAccountStorage`

The storage layer provides:
- **Direct account lookups** by address (33-byte compressed ECDSA secp256k1 public key)
- **Atomic updates** using RocksDB write batches
- **Efficient serialization** using little-endian binary format

**Storage Format**:
```
Key: 33-byte public key (account address)
Value: 16 bytes (8-byte balance + 8-byte nonce)
```

### 3. State Manager

**Implementation**: `Spacetime.Consensus.IStateManager` and `ChainStateManager`

The state manager orchestrates all state transitions:

#### Core Responsibilities:

1. **Block Application** (`ApplyBlockAsync`)
   - Validates block state transitions
   - Updates all account balances atomically
   - Increments nonces for sending accounts
   - Distributes transaction fees to miners
   - Computes new state root

2. **State Validation** (`ValidateBlockStateAsync`)
   - Verifies transaction signatures
   - Checks sufficient balance for all transactions
   - Validates sequential nonces
   - Detects double-spending within blocks

3. **Balance and Nonce Queries** (`GetBalanceAsync`, `GetNonceAsync`)
   - Direct account state retrieval
   - Returns 0 for non-existent accounts

4. **Snapshot Management** (for chain reorganizations)
   - `CreateSnapshotAsync`: Creates restoration points
   - `RevertToSnapshotAsync`: Reverts to previous state
   - `ReleaseSnapshotAsync`: Frees snapshot resources

5. **State Root Computation** (`ComputeStateRootAsync`)
   - Generates Merkle root for light client verification
   - Enables state proofs without full state download

6. **Consistency Checks** (`CheckConsistencyAsync`)
   - Validates database integrity
   - Detects corruption

## Transaction Flow

### 1. Transaction Creation

```csharp
var tx = new Transaction(
    sender: senderPublicKey,      // 33-byte compressed public key
    recipient: recipientPublicKey, // 33-byte compressed public key
    amount: 1000,                  // Amount to transfer
    nonce: currentNonce,           // Current account nonce
    fee: 10,                       // Fee paid to miner
    signature: ecdsaSignature      // 64-byte signature
);
```

### 2. Transaction Validation

Basic validation checks:
- Transaction is signed
- Amount > 0
- Fee ≥ 0
- Nonce ≥ 0
- Sender ≠ Recipient

State validation checks:
- Signature is valid for the transaction hash
- Sender has sufficient balance (amount + fee)
- Nonce matches sender's current nonce
- No double-spending within block

### 3. State Transition

When a block is applied:

```
For each transaction in block:
  1. Get sender account state
  2. Get recipient account state
  3. Deduct from sender: balance -= (amount + fee)
  4. Increment sender nonce: nonce += 1
  5. Add to recipient: balance += amount

After all transactions:
  6. Add total fees to miner account
  7. Commit all changes atomically
  8. Compute new state root
```

### 4. Atomicity Guarantees

All state changes in a block are applied atomically using RocksDB write batches:
- Either all transactions succeed, or none do
- No partial state updates
- Consistent state across all accounts

## Replay Attack Prevention

**Nonce Mechanism**:
- Each account maintains a nonce counter starting at 0
- Transactions must use the current nonce
- Nonce increments after each transaction
- Out-of-order nonces are rejected

**Example**:
```
Account A (Balance: 1000, Nonce: 0)

Transaction 1: nonce=0, amount=100 ✓ (Valid)
  → Account A (Balance: 890, Nonce: 1)

Transaction 2: nonce=0, amount=100 ✗ (Rejected - nonce mismatch)
Transaction 2: nonce=1, amount=100 ✓ (Valid)
  → Account A (Balance: 780, Nonce: 2)
```

## Chain Reorganization Support

### Snapshot-Based Rollback

The state manager supports creating snapshots for potential rollbacks during chain reorganizations:

```csharp
// Before applying new blocks
var snapshotId = await stateManager.CreateSnapshotAsync();

try {
    // Apply new chain blocks
    await stateManager.ApplyBlockAsync(block1);
    await stateManager.ApplyBlockAsync(block2);
    
    // If successful, release snapshot
    await stateManager.ReleaseSnapshotAsync(snapshotId);
}
catch {
    // Revert to snapshot on failure
    await stateManager.RevertToSnapshotAsync(snapshotId);
}
```

### Current Implementation

The current implementation provides the snapshot API but uses a simplified approach:
- Snapshots track metadata (ID, timestamp)
- Full RocksDB snapshot integration is planned for future versions
- For production use, proper RocksDB snapshot/restoration is required

### Future Enhancement: RocksDB Snapshots

Production-ready rollback will leverage RocksDB features:
- **Snapshots**: Create consistent point-in-time views
- **Checkpoints**: Backup database state to separate directory
- **Write Batches**: Atomic multi-operation commits

## State Root and Light Clients

### Purpose

The state root is a 32-byte hash representing the entire account state:
- Enables light clients to verify account balances
- Allows Merkle proofs for specific accounts
- Included in block headers for consensus

### Current Implementation

Placeholder using SHA256 hash:
```csharp
var emptyRoot = SHA256.HashData(Array.Empty<byte>());
```

### Future Enhancement: Merkle Patricia Trie

Production implementation will use a Merkle Patricia Trie:
- Efficient insertion, deletion, and proof generation
- Used by Ethereum and other account-based chains
- Enables light client state verification

**Structure**:
```
State Root (32 bytes)
    |
    ├─ Branch Node
    │   ├─ Account 0x01... → (Balance, Nonce)
    │   └─ Account 0x02... → (Balance, Nonce)
    └─ Branch Node
        ├─ Account 0xAB... → (Balance, Nonce)
        └─ Account 0xCD... → (Balance, Nonce)
```

## Performance Characteristics

### Storage Operations

- **Account Lookup**: O(1) - Direct RocksDB key lookup
- **Account Update**: O(1) - Direct RocksDB put operation
- **Batch Commit**: O(n) - Where n is number of modified accounts

### Block Application

For a block with T transactions:
- **Validation**: O(T) - Verify each transaction
- **State Updates**: O(T) - Update sender and recipient accounts
- **Commit**: O(T) - Atomic batch write

### Memory Usage

- Minimal memory footprint - no in-memory state cache
- RocksDB handles caching and memory management
- Write batches hold pending changes before commit

## Security Considerations

### Transaction Security

1. **Signature Verification**: All transactions must be signed with sender's private key
2. **Nonce Protection**: Prevents replay attacks and transaction ordering issues
3. **Balance Validation**: Prevents spending more than available balance
4. **Atomic Updates**: Ensures consistent state even on failures

### State Integrity

1. **Atomic Writes**: All-or-nothing state updates per block
2. **Consistency Checks**: Database integrity validation
3. **Snapshot Isolation**: Safe rollback during reorganizations

### Attack Vectors and Mitigations

**Double-Spending in Block**:
- Mitigated by tracking in-block account state
- Validation rejects multiple spends exceeding balance

**Nonce Gaps**:
- Rejected during validation
- Nonces must be sequential

**Signature Forgery**:
- ECDSA secp256k1 signature verification
- Transaction hash includes all transaction data

## Future Extensions

### Smart Contracts

The account model naturally extends to support smart contracts:

```csharp
public record AccountState(
    long Balance,
    long Nonce,
    byte[]? CodeHash,      // Hash of contract bytecode
    byte[]? StorageRoot    // Merkle root of contract storage
);
```

### Contract Execution

1. External Owned Accounts (EOA): Current user accounts
2. Contract Accounts: Accounts with code and storage
3. Transaction to contract triggers code execution
4. Gas metering limits computation

### Advanced State Features

- **State Tries**: Full Merkle Patricia Trie implementation
- **State Pruning**: Remove old historical states
- **State Snapshots**: Fast sync for new nodes
- **State Caching**: In-memory recent state cache

## Testing Strategy

### Unit Tests (22 tests)

Located in `tests/Spacetime.Consensus.Tests/ChainStateManagerTests.cs`:
- Constructor validation
- Balance and nonce queries
- Block state validation
- Block application
- Snapshot management
- State root computation
- Consistency checks

### Integration Tests (8 tests)

Located in `tests/Spacetime.Consensus.IntegrationTests/ChainStateManagerIntegrationTests.cs`:
- Multiple block sequences
- Complex transaction chains
- Many transactions per block
- Sequential block validation
- Out-of-order nonce rejection
- Concurrent account updates
- Atomicity guarantees
- Long-term consistency

### Coverage Goals

- **Unit Tests**: 90%+ coverage of ChainStateManager
- **Integration Tests**: Real-world block application scenarios
- **Edge Cases**: Boundary conditions, error paths

## References

### Related Components

- `Spacetime.Core.Transaction`: Transaction data structure
- `Spacetime.Core.Block`: Block structure with transactions
- `Spacetime.Storage.IAccountStorage`: Storage interface
- `Spacetime.Storage.RocksDbAccountStorage`: RocksDB implementation

### Design Decisions

1. **Account Model vs UTXO**: See issue #15 discussion
2. **RocksDB Selection**: Chosen for performance and reliability (issue #14)
3. **Nonce-based Ordering**: Prevents replay attacks, enables mempool ordering

### Standards and Best Practices

- ECDSA secp256k1 for signatures (Bitcoin/Ethereum compatible)
- SHA256 for hashing
- Little-endian binary serialization
- Atomic database operations
- Comprehensive error handling
