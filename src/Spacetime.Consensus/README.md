# Spacetime.Consensus

Consensus logic, proof validation, and difficulty adjustment for the Spacetime blockchain.

## Overview

This project contains the core consensus mechanisms for the Spacetime Proof-of-Space-Time blockchain, including:

- **Chain State Management**: Account-based state transitions with atomic updates and replay protection
- **Proof Validation**: Cryptographic verification of miner proofs
- **Score Calculation**: Computing proof scores from challenges and plot leaves
- **Difficulty Adjustment**: Automatic difficulty recalculation to maintain target block time
- **Difficulty Target Management**: Conversion between difficulty integers and 32-byte targets
- **Merkle Path Verification**: Integration with MerkleTree library for proof paths
- **Transaction Validation**: Signature verification, balance checks, and nonce validation

## Architecture

### Chain State Management

The `ChainStateManager` implements account-based blockchain state management with atomic transitions, replay attack prevention, and support for chain reorganizations.

#### Account Model

Spacetime uses an **account-based model** (similar to Ethereum) rather than UTXO. Each account is identified by a 33-byte public key and maintains:

- **Balance**: Current coin balance (int64)
- **Nonce**: Transaction counter for replay protection (int64)

#### State Transitions

State transitions occur when blocks are applied:

1. **Validation Phase** - Checks all transactions in the block:
   - Signature verification using ECDSA
   - Balance sufficiency (amount + fee)
   - Nonce correctness (must equal current account nonce)
   - No double-spending within the block

2. **Application Phase** - Atomically updates state:
   - Deducts amount + fee from sender
   - Increments sender nonce
   - Credits amount to recipient
   - Distributes fees to miner

All changes use **RocksDB write batches** to ensure atomicity - either all transactions in a block succeed, or none do.

#### Replay Protection

Transaction nonces prevent replay attacks:
- Each account starts with nonce = 0
- Each transaction must use the account's current nonce
- After processing, nonce increments by 1
- Transactions with incorrect nonces are rejected

This ensures transactions cannot be replayed on the same chain or copied to other chains.

#### Chain Reorganization Support

The state manager supports snapshots for handling reorganizations (reorgs):

1. **Create Snapshot** - Capture current state before applying speculative blocks
2. **Apply Blocks** - Process blocks on a potential fork
3. **Revert or Commit**:
   - If fork loses: revert to snapshot
   - If fork wins: release old snapshot

Snapshots are lightweight and enable efficient chain switching.

#### State Root Computation

The state root is a cryptographic commitment to the entire state:
- Computed from all account states using Merkle Patricia Trie (planned)
- Currently uses simplified hashing (production will use proper trie)
- Enables light clients to verify state without downloading all accounts
- Included in block headers for verification

### Proof Validation

The `ProofValidator` class provides comprehensive proof verification with these checks (in order):

1. **Challenge Correctness** - Ensures the challenge matches the expected value
2. **Plot Root Verification** - Validates the Merkle root matches the known plot identity
3. **Score Recalculation** - Recomputes score = H(challenge || leaf) and verifies match
4. **Difficulty Target Check** - Validates score < target (optional parameter)
5. **Merkle Path Verification** - Uses MerkleTree library to verify proof path

### Difficulty System

The blockchain uses a two-level difficulty system:

#### 1. Difficulty Integer (Stored in Blocks)
- A positive integer where **higher values = more difficult**
- Human-readable, matches Bitcoin's convention
- Examples:
  - Mainnet: 1,000,000 (hardest)
  - Testnet: 10,000 (moderate)
  - DevNet: 100 (easiest)

#### 2. Difficulty Target (32-byte Hash)
- The maximum score that can be considered valid
- A proof score must be **strictly less than** this target
- **Lower targets = more difficult** (fewer hashes satisfy the constraint)
- Derived from difficulty integer via conversion formula

**Important**: This module currently works with the 32-byte difficulty target directly. The conversion from difficulty integer to target hash will be implemented in the difficulty adjustment system.

### Score Calculation

Proof scores are computed as:

```
score = SHA256(challenge || leaf)
```

Where:
- `challenge` is the 32-byte epoch challenge
- `leaf` is the 32-byte leaf value from the plot
- `||` denotes byte concatenation
- **Lower scores are better** in consensus

### Validation Results

`ProofValidationResult` encapsulates validation outcomes with detailed error information:

```csharp
var result = validator.ValidateProof(proof, challenge, plotRoot, difficultyTarget);

if (!result.IsValid)
{
    // result.Error.Type - enum indicating specific failure
    // result.ErrorMessage - detailed message with hex values
}
```

#### Error Types

- `ChallengeMismatch` - Challenge doesn't match expected value
- `PlotRootMismatch` - Merkle root doesn't match known plot identity
- `ScoreMismatch` - Recalculated score doesn't match proof's score
- `ScoreAboveTarget` - Score doesn't meet difficulty threshold
- `InvalidMerklePath` - Merkle proof path verification failed
- `InvalidLeafValue` - Leaf value is incorrect

## Usage

### State Management

#### Creating a State Manager

```csharp
using Spacetime.Consensus;
using Spacetime.Storage;

// Initialize with storage and signature verifier
var storage = new RocksDbChainStorage(dbPath);
var signatureVerifier = new EcdsaSignatureVerifier();
var stateManager = new ChainStateManager(storage, signatureVerifier);
```

#### Applying Blocks

```csharp
// Validate block state before applying
if (await stateManager.ValidateBlockStateAsync(block, cancellationToken))
{
    // Apply block and get new state root
    byte[] stateRoot = await stateManager.ApplyBlockAsync(block, cancellationToken);
    
    Console.WriteLine($"Block applied. New state root: {Convert.ToHexString(stateRoot)}");
}
else
{
    Console.WriteLine("Block validation failed");
}
```

#### Querying Account State

```csharp
// Get account balance
byte[] accountAddress = publicKey; // 33-byte public key
long balance = await stateManager.GetBalanceAsync(accountAddress, cancellationToken);
Console.WriteLine($"Balance: {balance} coins");

// Get account nonce
long nonce = await stateManager.GetNonceAsync(accountAddress, cancellationToken);
Console.WriteLine($"Next nonce: {nonce}");
```

#### Handling Chain Reorganizations

```csharp
// Scenario: New fork appears that might be longer

// 1. Create snapshot before processing fork
long snapshotId = await stateManager.CreateSnapshotAsync(cancellationToken);

try
{
    // 2. Apply fork blocks
    foreach (var forkBlock in forkBlocks)
    {
        await stateManager.ApplyBlockAsync(forkBlock, cancellationToken);
    }
    
    // 3. Fork wins - release old snapshot
    await stateManager.ReleaseSnapshotAsync(snapshotId, cancellationToken);
}
catch (Exception)
{
    // 4. Fork invalid - revert to snapshot
    await stateManager.RevertToSnapshotAsync(snapshotId, cancellationToken);
    await stateManager.ReleaseSnapshotAsync(snapshotId, cancellationToken);
    throw;
}
```

#### State Consistency Checks

```csharp
// Check if state is consistent (detects corruption)
bool isConsistent = await stateManager.CheckConsistencyAsync(cancellationToken);

if (!isConsistent)
{
    Console.WriteLine("WARNING: State corruption detected!");
    // Trigger recovery procedures
}
```

#### Computing State Root

```csharp
// Get current state root for light client verification
byte[] stateRoot = await stateManager.ComputeStateRootAsync(cancellationToken);

// Include in block header
blockHeader.StateRoot = stateRoot;
```

### Block Validation

The state manager integrates with block validation:

```csharp
// Full block validation workflow
public async Task<bool> ValidateAndApplyBlockAsync(Block block)
{
    // 1. Validate proof (ProofValidator)
    var proofResult = proofValidator.ValidateProof(
        block.Proof,
        expectedChallenge,
        expectedPlotRoot,
        difficultyTarget);
    
    if (!proofResult.IsValid)
        return false;
    
    // 2. Validate state transitions (ChainStateManager)
    if (!await stateManager.ValidateBlockStateAsync(block))
        return false;
    
    // 3. Apply block to state
    await stateManager.ApplyBlockAsync(block);
    
    return true;
}
```

### Basic Validation

```csharp
using Spacetime.Consensus;
using MerkleTree.Hashing;

var hashFunction = new Sha256HashFunction();
var validator = new ProofValidator(hashFunction);

// Validate proof without difficulty check
var result = validator.ValidateProof(
    proof,
    expectedChallenge,
    expectedPlotRoot);

if (result.IsValid)
{
    // Proof is valid
}
```

### Validation with Difficulty Target

```csharp
// Create a 32-byte difficulty target
byte[] difficultyTarget = new byte[32];
// ... set target value (derived from difficulty integer)

var result = validator.ValidateProof(
    proof,
    expectedChallenge,
    expectedPlotRoot,
    difficultyTarget,
    treeHeight);

if (!result.IsValid)
{
    Console.WriteLine($"Validation failed: {result.ErrorMessage}");
    // Example output:
    // "Score ABC123... does not meet difficulty target DEF456..."
}
```

### Score Calculation

```csharp
var validator = new ProofValidator(hashFunction);

byte[] score = validator.ComputeScore(challenge, leafValue);
// score = SHA256(challenge || leafValue)
```

### Score Comparison

```csharp
bool meetsTarget = validator.IsScoreBelowTarget(score, difficultyTarget);
// Returns true if score < target
```

## Difficulty Adjustment

The `DifficultyAdjuster` class implements automatic difficulty adjustment to maintain target block time:

```csharp
using Spacetime.Consensus;

// Configure difficulty adjustment
var config = new DifficultyAdjustmentConfig(
    targetBlockTimeSeconds: 10,
    adjustmentIntervalBlocks: 100,
    dampeningFactor: 4,
    minimumDifficulty: 1,
    maximumDifficulty: long.MaxValue);

var adjuster = new DifficultyAdjuster(config);

// Check if difficulty should adjust at this height
if (adjuster.ShouldAdjustDifficulty(currentHeight))
{
    // Calculate new difficulty based on actual vs target block times
    long newDifficulty = adjuster.CalculateNextDifficulty(
        currentDifficulty,
        currentHeight,
        currentTimestamp,
        intervalStartTimestamp);
}

// Convert difficulty to 32-byte target for validation
byte[] target = DifficultyAdjuster.DifficultyToTarget(newDifficulty);

// Use target with ProofValidator
var validator = new ProofValidator(hashFunction);
var result = validator.ValidateProof(proof, challenge, plotRoot, target);
```

### Adjustment Algorithm

The algorithm maintains target block time by:
1. Calculating actual time taken for N blocks (adjustment interval)
2. Comparing to expected time (N × target block time)
3. Adjusting difficulty proportionally: `newDifficulty = currentDifficulty × targetTime / actualTime`
4. Applying dampening factor to smooth adjustments: `adjustment = adjustment / dampeningFactor`
5. Enforcing minimum and maximum bounds

### Difficulty-to-Target Conversion

- **Formula**: `target = (2^256 - 1) / difficulty`
- **Properties**:
  - Higher difficulty → lower target → harder to mine
  - `difficulty = 1` → maximum target (easiest)
  - Target is 32-byte big-endian value for comparison with proof scores

## API Reference

### IStateManager

Core interface for blockchain state management.

#### Methods

##### ApplyBlockAsync
```csharp
Task<byte[]> ApplyBlockAsync(Block block, CancellationToken cancellationToken = default)
```
Applies a block to the current state, updating account balances and nonces atomically.
- **Returns**: State root hash after applying the block
- **Throws**: `InvalidOperationException` if validation fails

##### ValidateBlockStateAsync
```csharp
Task<bool> ValidateBlockStateAsync(Block block, CancellationToken cancellationToken = default)
```
Validates that a block can be applied to current state. Checks:
- Transaction signatures (ECDSA verification)
- Sufficient balances (amount + fee)
- Correct nonces (sequential per account)
- No double-spending within block

##### GetBalanceAsync
```csharp
Task<long> GetBalanceAsync(ReadOnlyMemory<byte> address, CancellationToken cancellationToken = default)
```
Gets the balance of an account. Returns 0 if account doesn't exist.
- **Parameters**: 33-byte public key address

##### GetNonceAsync
```csharp
Task<long> GetNonceAsync(ReadOnlyMemory<byte> address, CancellationToken cancellationToken = default)
```
Gets the nonce of an account. Returns 0 if account doesn't exist.
- **Parameters**: 33-byte public key address

##### ComputeStateRootAsync
```csharp
Task<byte[]> ComputeStateRootAsync(CancellationToken cancellationToken = default)
```
Computes the current state root hash (Merkle root of all account states).
- **Returns**: 32-byte state root hash

##### CreateSnapshotAsync
```csharp
Task<long> CreateSnapshotAsync(CancellationToken cancellationToken = default)
```
Creates a snapshot of current state for potential rollback.
- **Returns**: Snapshot identifier

##### RevertToSnapshotAsync
```csharp
Task RevertToSnapshotAsync(long snapshotId, CancellationToken cancellationToken = default)
```
Reverts state to a previous snapshot.
- **Throws**: `ArgumentException` if snapshot ID is invalid

##### ReleaseSnapshotAsync
```csharp
Task ReleaseSnapshotAsync(long snapshotId, CancellationToken cancellationToken = default)
```
Releases a snapshot, freeing resources.

##### CheckConsistencyAsync
```csharp
Task<bool> CheckConsistencyAsync(CancellationToken cancellationToken = default)
```
Checks state consistency and detects corruption.

### IChainState

Provides read-only access to current blockchain state for validation.

#### Methods

- `GetChainTipHashAsync()` - Hash of current chain tip
- `GetChainTipHeightAsync()` - Height of current chain tip
- `GetExpectedDifficultyAsync()` - Expected difficulty for next block
- `GetExpectedEpochAsync()` - Expected epoch number for next block
- `GetExpectedChallengeAsync()` - Expected 32-byte challenge for current epoch

## Integration

### Component Dependencies

**ChainStateManager** integrates with:

- **Spacetime.Storage** (`IChainStorage`) - Persists account data using RocksDB
- **Spacetime.Core** (`Block`, `Transaction`, `ISignatureVerifier`) - Block and transaction structures
- **Spacetime.Common** - Shared utilities and cryptographic helpers

### Storage Layer

Account data is stored in RocksDB:
- **Key**: 33-byte public key (account address)
- **Value**: 16 bytes (8-byte balance + 8-byte nonce, little-endian)
- **Column Family**: `accounts`

Write batches ensure atomic updates across multiple accounts.

### Validation Pipeline

1. **Proof Validation** (`ProofValidator`) - Verifies proof-of-space-time
2. **Block Structure Validation** (`BlockValidator`) - Validates block format and headers
3. **State Validation** (`ChainStateManager`) - Validates and applies state transitions
4. **Difficulty Adjustment** (`DifficultyAdjuster`) - Recalculates difficulty periodically

## Future Development

- **Merkle Patricia Trie**: Replace simplified state root with proper trie implementation
- **RocksDB Snapshots**: Implement full snapshot/revert using RocksDB native snapshots
- **Consensus Rules**: Additional validation rules for block acceptance
- **Fork Choice**: Logic for selecting the canonical chain (longest chain rule)
- **Difficulty History**: Store and query historical difficulty values
- **State Pruning**: Archive old state to reduce storage requirements
- **Light Client Proofs**: Generate Merkle proofs for specific accounts

## Dependencies

- **Spacetime.Common** - Shared utilities and cryptographic helpers
- **Spacetime.Core** - Block, transaction, and signature verification interfaces
- **Spacetime.Storage** - Chain storage abstractions (RocksDB)
- **Spacetime.Plotting** - Proof data structures
- **MerkleTree** - Merkle tree proof verification

## Testing

The consensus module is comprehensively tested with high coverage:

### Unit Tests (`tests/Spacetime.Consensus.Tests/`)

**Proof Validation** (34 tests):
- Score calculation and target comparison
- Challenge and plot root verification
- Merkle path validation
- Error handling and edge cases

**Difficulty Adjustment** (54 tests):
- Difficulty adjustment algorithm
- Boundary conditions and dampening
- Time-based difficulty scaling

**Difficulty Configuration** (42 tests):
- Configuration validation
- Parameter boundary checks

**State Management** (22 tests):
- Account state transitions
- Transaction validation (signatures, balances, nonces)
- Atomic block application
- Replay attack prevention
- Balance and nonce queries
- Snapshot creation and release
- State consistency checks

**Simulation Tests** (12 tests):
- Various mining scenarios with realistic conditions

**Total**: 164 unit tests

### Integration Tests (`tests/Spacetime.Consensus.IntegrationTests/`)

**Proof Validation** (9 tests):
- Real plot generation and end-to-end validation
- Full proof pipeline integration

**State Management** (8 tests):
- Multi-transaction block processing
- Chain reorganization scenarios
- State snapshot and revert workflows
- Integration with RocksDB storage
- Concurrent transaction validation

**Total**: 17 integration tests

All tests passing with 90%+ code coverage for business logic.
