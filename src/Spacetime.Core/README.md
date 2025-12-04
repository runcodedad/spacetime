# Spacetime.Core

Core blockchain data structures for the Spacetime blockchain.

## Overview

This project provides the fundamental data structures for the Spacetime Proof-of-Space-Time blockchain:

- **Block** - Complete block combining header and body
- **BlockHeader** - Block metadata, consensus data, and authentication
- **BlockBody** - Transactions and PoST proof
- **BlockProof** - Proof-of-Space-Time proof data
- **BlockPlotMetadata** - Plot file metadata included in proofs
- **Transaction** - Account-based transaction with sender, recipient, amount, nonce, fee, and signature
- **BlockBuilder** - Constructs valid blocks when a miner wins (collects transactions, builds Merkle tree, signs)
- **IMempool** - Interface for accessing pending transactions
- **IBlockSigner** - Interface for cryptographic block signing
- **IBlockValidator** - Interface for block validation
- **EpochManager** - Manages epoch transitions and challenge generation
- **ChallengeDerivation** - Deterministic challenge derivation from block hash
- **EpochConfig** - Configuration for epoch timing and challenge windows
- **IEpochManager** - Interface for epoch management abstraction
- **IChallengeProvider** - Interface for challenge broadcasting to miners

## Block Structure

### Block Header (290 bytes)

| Field | Type | Size (bytes) | Description |
|-------|------|--------------|-------------|
| version | byte | 1 | Protocol version for this block |
| parent_hash | bytes | 32 | SHA256 hash of the previous block |
| height | long | 8 | Block height (parent height + 1) |
| timestamp | long | 8 | UTC timestamp when block was assembled |
| difficulty | long | 8 | Difficulty used for this epoch's scoring threshold |
| epoch | long | 8 | Challenge epoch this block belongs to |
| challenge | bytes | 32 | Challenge issued for this epoch |
| plot_root | bytes | 32 | Merkle root of the winning miner's plot file |
| proof_score | bytes | 32 | Computed score for the winning leaf |
| tx_root | bytes | 32 | Merkle root of all included transactions |
| miner_id | bytes | 33 | Public key of the winning miner (compressed ECDSA secp256k1) |
| signature | bytes | 64 | ECDSA signature of the header (without signature field) |

**Header Hash**: `SHA256(serialize(header_without_signature))`

### Block Body (variable size)

| Field | Type | Description |
|-------|------|-------------|
| transactions[] | Transaction[] | List of transactions (length-prefixed) |
| proof | BlockProof | The winning PoST proof |

### Transaction (155 bytes)

| Field | Type | Size (bytes) | Description |
|-------|------|--------------|-------------|
| version | byte | 1 | Transaction format version |
| sender | bytes | 33 | Public key of the sender (compressed ECDSA secp256k1) |
| recipient | bytes | 33 | Public key of the recipient (compressed ECDSA secp256k1) |
| amount | long | 8 | Amount to transfer (must be positive) |
| nonce | long | 8 | Nonce for replay protection (sequential per account) |
| fee | long | 8 | Transaction fee paid to miner |
| signature | bytes | 64 | ECDSA signature of the transaction (without signature field) |

**Transaction Hash**: `SHA256(serialize(transaction_without_signature))`

**Transaction Model**: Spacetime uses an **account-based** transaction model for extensibility and future smart contract support.

### Block Proof (variable size)

| Field | Type | Description |
|-------|------|-------------|
| leaf_value | bytes[32] | The leaf value that produced the best score |
| leaf_index | long | Zero-based index of the leaf in the plot |
| merkle_proof_path[] | bytes[32][] | Sibling hashes for Merkle proof verification |
| orientation_bits[] | bool[] | Direction bits for Merkle proof path |
| plot_metadata | BlockPlotMetadata | Metadata about the plot file |

### Block Plot Metadata (73 bytes)

| Field | Type | Size (bytes) | Description |
|-------|------|--------------|-------------|
| leaf_count | long | 8 | Total number of leaves in the plot |
| plot_id | bytes | 32 | Unique identifier of the plot |
| plot_header_hash | bytes | 32 | SHA256 hash of the plot header |
| version | byte | 1 | Plot format version |

## Serialization Format

All data is serialized using little-endian byte order. The format is versioned through the `version` field in both the block header and plot metadata.

### Binary Serialization

```csharp
// Serialize a block
var bytes = block.Serialize();

// Deserialize a block
var block = Block.Deserialize(bytes);

// Or use BinaryReader/BinaryWriter for streaming
using var writer = new BinaryWriter(stream);
block.Serialize(writer);

using var reader = new BinaryReader(stream);
var block = Block.Deserialize(reader);
```

### Computing Block Hash

```csharp
// Block hash is the SHA256 of the header (without signature)
var hash = block.ComputeHash();

// Or directly from header
var hash = blockHeader.ComputeHash();
```

## Usage Examples

### Building a Block (Recommended for Miners)

```csharp
using Spacetime.Core;
using System.Security.Cryptography;

// Create implementations of required interfaces (application-specific)
IMempool mempool = new YourMempoolImplementation();
IBlockSigner signer = new YourSignerImplementation(minerPrivateKey);
IBlockValidator validator = new YourValidatorImplementation();

// Create the block builder
var builder = new BlockBuilder(mempool, signer, validator);

// Create the winning proof
var proof = new BlockProof(
    leafValue: winningLeafValue,
    leafIndex: winningLeafIndex,
    merkleProofPath: merkleProofSiblings,
    orientationBits: merkleProofOrientationBits,
    plotMetadata: plotMetadata);

// Build a complete, signed, and validated block
var block = await builder.BuildBlockAsync(
    parentHash: previousBlockHash,
    height: previousHeight + 1,
    difficulty: currentDifficulty,
    epoch: currentEpoch,
    challenge: currentChallenge,
    proof: proof,
    plotRoot: plotMerkleRoot,
    proofScore: computedProofScore,
    maxTransactions: 1000);

// Block is ready for broadcast
var blockBytes = block.Serialize();
```

### Creating a Block Manually (Low-Level)

```csharp
using Spacetime.Core;
using System.Security.Cryptography;

// Create plot metadata
var plotMetadata = BlockPlotMetadata.Create(
    leafCount: 1_000_000,
    plotId: plotIdBytes,
    plotHeaderHash: plotHeaderHashBytes,
    version: 1);

// Create the proof
var proof = new BlockProof(
    leafValue: leafValueBytes,
    leafIndex: 12345,
    merkleProofPath: siblingHashes,
    orientationBits: orientationBits,
    plotMetadata: plotMetadata);

// Create block body
var body = new BlockBody(transactions: transactionList, proof: proof);

// Create block header (unsigned)
var header = new BlockHeader(
    version: BlockHeader.CurrentVersion,
    parentHash: previousBlockHash,
    height: previousHeight + 1,
    timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    difficulty: currentDifficulty,
    epoch: currentEpoch,
    challenge: currentChallenge,
    plotRoot: plotMerkleRoot,
    proofScore: computedScore,
    txRoot: transactionMerkleRoot,
    minerId: minerPublicKey,
    signature: Array.Empty<byte>());

// Sign the header (implement your own signing using ECDSA)
var headerHash = header.ComputeHash();
var signature = SignWithEcdsaSecp256k1(headerHash, minerPrivateKey); // User-implemented
header.SetSignature(signature);

// Create complete block
var block = new Block(header, body);

// Serialize for network transmission
var blockBytes = block.Serialize();
```

### Validating a Block

```csharp
// Deserialize received block
var block = Block.Deserialize(receivedBytes);

// Verify header fields
if (block.Header.Height != expectedHeight)
    throw new InvalidOperationException("Invalid block height");

if (!block.Header.ParentHash.SequenceEqual(previousBlockHash))
    throw new InvalidOperationException("Invalid parent hash");

// Verify signature (implement your own verification using ECDSA)
var headerHash = block.Header.ComputeHash();
if (!VerifyEcdsaSecp256k1(headerHash, block.Header.Signature, block.Header.MinerId)) // User-implemented
    throw new InvalidOperationException("Invalid signature");

// Verify proof (application-specific logic)
// ...
```

### Creating and Signing a Transaction

```csharp
using Spacetime.Core;
using System.Security.Cryptography;

// Create an unsigned transaction
var tx = new Transaction(
    sender: senderPublicKey,        // 33-byte compressed ECDSA secp256k1 public key
    recipient: recipientPublicKey,  // 33-byte compressed ECDSA secp256k1 public key
    amount: 1000,                   // Transfer 1000 units
    nonce: 5,                       // Account nonce (must be sequential)
    fee: 10,                        // Pay 10 units as transaction fee
    signature: Array.Empty<byte>()); // Empty signature for unsigned tx

// Compute transaction hash (for signing)
var txHash = tx.ComputeHash();

// Sign the transaction (implement your own signing using ECDSA)
var signature = SignWithEcdsaSecp256k1(txHash, senderPrivateKey); // User-implemented
tx.SetSignature(signature);

// Serialize for network transmission
var txBytes = tx.Serialize();

// Or create block with transactions
var transactions = new[] { tx };
var body = new BlockBody(transactions, proof);
```

### Validating a Transaction

```csharp
// Deserialize received transaction
var tx = Transaction.Deserialize(receivedBytes);

// Basic validation (structure and rules)
if (!tx.ValidateBasicRules())
    throw new InvalidOperationException("Transaction failed basic validation");

// Verify signature (implement your own verification using ECDSA)
var txHash = tx.ComputeHash();
if (!VerifyEcdsaSecp256k1(txHash, tx.Signature, tx.Sender)) // User-implemented
    throw new InvalidOperationException("Invalid transaction signature");

// Verify sender has sufficient balance (application-specific state check)
var senderBalance = GetAccountBalance(tx.Sender); // User-implemented
if (senderBalance < tx.Amount + tx.Fee)
    throw new InvalidOperationException("Insufficient balance");

// Verify nonce is correct (prevents replay attacks)
var expectedNonce = GetAccountNonce(tx.Sender); // User-implemented
if (tx.Nonce != expectedNonce)
    throw new InvalidOperationException("Invalid nonce");

// Transaction is valid - apply state changes
```

### Working with Block Transactions

```csharp
// Create block with typed transactions
var transactions = new Transaction[]
{
    new Transaction(sender1, recipient1, 1000, 1, 10, signature1),
    new Transaction(sender2, recipient2, 2000, 2, 20, signature2)
};
var body = new BlockBody(transactions, proof);

// Retrieve transactions from block body
var txList = body.Transactions;
foreach (var tx in txList)
{
    Console.WriteLine($"Amount: {tx.Amount}, Fee: {tx.Fee}, Nonce: {tx.Nonce}");
}
```

## Transaction Validation Rules

The `Transaction.ValidateBasicRules()` method performs basic structural validation:

1. **Signature Required**: Transaction must be signed (64-byte signature present)
2. **Positive Amount**: Amount must be greater than zero
3. **Non-negative Fee**: Fee must be zero or positive
4. **Non-negative Nonce**: Nonce must be zero or positive
5. **Distinct Parties**: Sender and recipient must be different

Additional validation (requiring state access) must be performed by the caller:

- **Signature Verification**: Verify the signature matches the sender's public key
- **Balance Check**: Verify sender has sufficient balance (amount + fee)
- **Nonce Check**: Verify nonce matches expected sequential value for the account
- **Account Existence**: Verify accounts exist or handle account creation

## Transaction Model: Account-Based

Spacetime uses an **account-based** transaction model (similar to Ethereum) rather than UTXO (like Bitcoin):

### Why Account-Based?

1. **Extensibility**: Easier to add smart contracts and complex state transitions in the future
2. **Simplicity**: Simpler to track balances and state per account
3. **Efficiency**: No need to track and validate multiple UTXOs per transaction

### Nonce for Replay Protection

Each account maintains a sequential nonce counter:
- First transaction from an account has nonce = 0
- Each subsequent transaction increments the nonce by 1
- Transactions must be processed in nonce order
- Prevents replay attacks where an old signed transaction is resubmitted

### State Management

Account state includes:
- **Balance**: Current account balance
- **Nonce**: Next expected transaction nonce
- Optionally: **Code** (for smart contracts, future feature)
- Optionally: **Storage** (for contract state, future feature)

## Design Decisions

1. **Immutability**: All fields are read-only after construction, except for the signature which can be set once.

2. **Validation**: Constructors validate all inputs immediately, throwing `ArgumentException` or `ArgumentNullException` for invalid data.

3. **Binary Serialization**: Uses `BinaryWriter`/`BinaryReader` for efficient binary serialization with explicit little-endian byte order.

4. **Defensive Copying**: All byte arrays are copied on construction to prevent external modification.

5. **ReadOnlySpan**: Hash fields are exposed as `ReadOnlySpan<byte>` to prevent modification while avoiding heap allocations. Use `.ToArray()` if you need to store or pass the data to async methods.

## BlockBuilder

The `BlockBuilder` class provides a high-level API for constructing valid blocks when a miner wins the challenge. It automates the entire block building process:

### Features

1. **Transaction Collection**: Automatically collects pending transactions from the mempool
2. **Merkle Tree Computation**: Builds the transaction Merkle tree using the MerkleTree library
3. **Header Population**: Populates all block header fields with current values
4. **Automatic Signing**: Signs the block using the provided signer implementation
5. **Self-Validation**: Validates the block before returning to ensure correctness
6. **Cancellation Support**: Respects cancellation tokens throughout the build process

### Required Interfaces

To use the `BlockBuilder`, you must provide implementations of these interfaces:

#### IMempool

Provides access to pending transactions:

```csharp
public interface IMempool
{
    Task<IReadOnlyList<Transaction>> GetPendingTransactionsAsync(
        int maxCount,
        CancellationToken cancellationToken = default);
}
```

Implementation should:
- Return transactions in priority order (typically by fee)
- Ensure all returned transactions are signed and valid
- Respect the `maxCount` limit

#### IBlockSigner

Provides cryptographic signing for blocks:

```csharp
public interface IBlockSigner
{
    Task<byte[]> SignBlockHeaderAsync(
        ReadOnlyMemory<byte> headerHash,
        CancellationToken cancellationToken = default);
    
    byte[] GetPublicKey();
}
```

Implementation should:
- Sign the 32-byte header hash using ECDSA with secp256k1
- Return a 64-byte signature (r, s components)
- Provide the 33-byte compressed public key

#### IBlockValidator

Validates blocks against consensus rules:

```csharp
public interface IBlockValidator
{
    Task<bool> ValidateBlockAsync(
        Block block,
        CancellationToken cancellationToken = default);
}
```

Implementation should verify:
- Block header signature is valid
- Proof score meets difficulty requirement
- Transaction Merkle root is correct
- All transactions are signed and valid
- Block timestamp is reasonable

### Usage Example

```csharp
// Setup dependencies
var mempool = new YourMempoolImplementation();
var signer = new YourSignerImplementation(minerPrivateKey);
var validator = new YourValidatorImplementation();

// Create builder
var builder = new BlockBuilder(mempool, signer, validator);

// Build block when you win
var block = await builder.BuildBlockAsync(
    parentHash: previousBlockHash,
    height: previousHeight + 1,
    difficulty: currentDifficulty,
    epoch: currentEpoch,
    challenge: currentChallenge,
    proof: winningProof,
    plotRoot: plotMerkleRoot,
    proofScore: computedScore,
    maxTransactions: 1000,
    cancellationToken: cancellationToken);

// Block is ready for broadcast
await BroadcastBlockAsync(block);
```

### Transaction Merkle Root

The BlockBuilder automatically computes the transaction Merkle root using the MerkleTree library:

- If there are no transactions, it produces a zero hash (32 zero bytes)
- If there are transactions, it builds a Merkle tree from their hashes
- The Merkle root is placed in the `tx_root` field of the block header

### Validation

The BlockBuilder performs self-validation before returning the block:

- Calls the provided `IBlockValidator` implementation
- Throws `InvalidOperationException` if validation fails
- Ensures the block is safe to broadcast

## Genesis Block Configuration

The genesis block is the first block in a blockchain and defines the initial state and parameters for a network. Spacetime provides comprehensive genesis block support.

### Predefined Network Configurations

```csharp
// Mainnet - Production network
var mainnetConfig = GenesisConfigs.Mainnet;

// Testnet - Test network
var testnetConfig = GenesisConfigs.Testnet;

// Development - Local development
var devnetConfig = GenesisConfigs.Development;
```

### Creating a Custom Genesis Configuration

```csharp
var config = GenesisConfigs.CreateCustom(
    networkId: "my-custom-network",
    timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    difficulty: 5000,
    epochDuration: 30,
    targetBlockTime: 30,
    preminedAllocations: new Dictionary<string, long>
    {
        ["03a1b2c3d4e5f6..."] = 1_000_000
    });
```

### Generating a Genesis Block

```csharp
// Create signer
IBlockSigner signer = new YourBlockSigner(privateKey);

// Generate genesis block
var generator = new GenesisBlockGenerator(signer);
var genesisBlock = await generator.GenerateGenesisBlockAsync(config);

// Genesis block has:
// - Height: 0
// - Parent hash: all zeros
// - Challenge: SHA256(networkId)
// - Plot root: all zeros
// - Proof score: all zeros
```

### Validating a Genesis Block

```csharp
var validator = new GenesisBlockValidator();
bool isValid = await validator.ValidateGenesisBlockAsync(genesisBlock, config);

if (!isValid)
{
    throw new InvalidOperationException("Invalid genesis block");
}
```

### Genesis Block Properties

A valid genesis block must have:
- Height = 0
- Parent hash = all zeros (32 bytes)
- Challenge = SHA256(networkId)
- Plot root = all zeros (32 bytes)
- Proof score = all zeros (32 bytes)
- Timestamp matches configuration
- Difficulty matches configuration
- Epoch matches configuration
- Valid signature from genesis signer

For detailed documentation, see [Genesis Configuration Guide](../../docs/genesis-configuration.md).

## Epoch Management and Challenge System

The epoch management system handles the timing and challenge derivation for Spacetime's Proof-of-Space-Time consensus.

### What is an Epoch?

An epoch is one full challenge cycle:
1. Network issues a challenge
2. Miners compute and submit proofs
3. Network evaluates proofs
4. If a valid proof meets difficulty → block is produced
5. Otherwise → epoch ends with no block

The **challenge window** defines how long miners have to submit proofs. Many epochs may occur before a block is produced.

### Epoch Configuration

```csharp
// Create epoch configuration
var config = new EpochConfig(epochDurationSeconds: 10); // 10 second challenge window

// Or use default (10 seconds)
var defaultConfig = EpochConfig.Default();
```

Configuration options:
- **EpochDurationSeconds**: Duration of each epoch (challenge window) in seconds
- **Default**: 10 seconds
- **Range**: 1-3600 seconds

### Challenge Derivation

Challenges are derived deterministically from the previous block hash to ensure:
- **Determinism**: All nodes derive the same challenge
- **Uniqueness**: Each epoch has a unique challenge (anti-replay)
- **Unpredictability**: Miners cannot pre-compute challenges

```csharp
// Derive challenge for a specific epoch
var blockHash = previousBlock.ComputeHash();
var epochNumber = 10;
var challenge = ChallengeDerivation.DeriveChallenge(blockHash, epochNumber);

// Verify a challenge
bool isValid = ChallengeDerivation.VerifyChallenge(challenge, blockHash, epochNumber);

// Derive genesis challenge (for first epoch)
var genesisChallenge = ChallengeDerivation.DeriveGenesisChallenge("mainnet");
bool isGenesisValid = ChallengeDerivation.VerifyGenesisChallenge(genesisChallenge, "mainnet");
```

### Using the Epoch Manager

The `EpochManager` tracks the current epoch and manages transitions:

```csharp
// Create epoch manager
var config = new EpochConfig(10);
var epochManager = new EpochManager(config);

// Advance to next epoch when a block is produced
var newBlockHash = block.ComputeHash();
await epochManager.AdvanceEpochAsync(newBlockHash);

// Access current epoch state
var currentEpoch = epochManager.CurrentEpoch;
var currentChallenge = epochManager.CurrentChallenge;
var epochStartTime = epochManager.EpochStartTime;
var timeRemaining = epochManager.TimeRemainingInEpoch;
var isExpired = epochManager.IsEpochExpired;

// Validate a challenge for a specific epoch
bool isValid = epochManager.ValidateChallengeForEpoch(
    challenge, 
    epochNumber, 
    previousBlockHash);

// Reset to specific state (e.g., after chain reorganization)
epochManager.Reset(epochNumber: 5, challenge, startTime);
```

### Challenge Broadcasting

The `IChallengeProvider` interface defines how challenges are broadcast to miners:

```csharp
public interface IChallengeProvider
{
    Task BroadcastChallengeAsync(
        ReadOnlyMemory<byte> challenge, 
        long epochNumber, 
        CancellationToken cancellationToken = default);
        
    event EventHandler<ChallengeEventArgs>? ChallengeAvailable;
}

// Implement in networking layer
public class NetworkChallengeProvider : IChallengeProvider
{
    public async Task BroadcastChallengeAsync(
        ReadOnlyMemory<byte> challenge, 
        long epochNumber, 
        CancellationToken cancellationToken = default)
    {
        // Broadcast to all connected miners via P2P network
        await _network.BroadcastAsync(new ChallengeMessage(challenge, epochNumber));
        
        // Raise event for local miners
        ChallengeAvailable?.Invoke(this, new ChallengeEventArgs(
            challenge, 
            epochNumber, 
            DateTimeOffset.UtcNow));
    }
    
    public event EventHandler<ChallengeEventArgs>? ChallengeAvailable;
}
```

### Epoch Lifecycle Example

```csharp
// Initialize epoch manager
var epochManager = new EpochManager(new EpochConfig(10));
var genesisHash = genesisBlock.ComputeHash();

// Start first epoch after genesis
await epochManager.AdvanceEpochAsync(genesisHash);

// Miners receive challenge and compute proofs
var challenge = epochManager.CurrentChallenge;
// ... miners scan plots and submit proofs ...

// After challenge window expires
if (epochManager.IsEpochExpired)
{
    // Check if any valid proofs were received
    if (HasValidProof())
    {
        // Produce block with winning proof
        var newBlock = await BuildBlockAsync(/*...*/);
        
        // Advance to next epoch with new block hash
        await epochManager.AdvanceEpochAsync(newBlock.ComputeHash());
    }
    else
    {
        // No winner, advance epoch without block production
        await epochManager.AdvanceEpochAsync(genesisHash); // Use same parent
    }
}
```

### Epoch and Block Production Integration

```csharp
// Full node workflow
var epochManager = new EpochManager(new EpochConfig(10));
var lastBlockHash = GetLatestBlockHash();

while (true)
{
    // Advance to next epoch
    await epochManager.AdvanceEpochAsync(lastBlockHash);
    
    // Broadcast challenge to miners
    await challengeProvider.BroadcastChallengeAsync(
        epochManager.CurrentChallenge,
        epochManager.CurrentEpoch);
    
    // Wait for challenge window
    await Task.Delay(TimeSpan.FromSeconds(10));
    
    // Collect and evaluate proofs
    var winningProof = EvaluateProofs(receivedProofs);
    
    if (winningProof != null)
    {
        // Build and broadcast block
        var newBlock = await BuildBlockWithProof(winningProof);
        await BroadcastBlock(newBlock);
        
        // Update last block hash for next epoch
        lastBlockHash = newBlock.ComputeHash();
    }
    // If no winner, lastBlockHash stays the same
}
```

### Thread Safety

- `EpochManager` is fully thread-safe
- All property accesses are protected by internal locking
- Multiple threads can safely call `AdvanceEpochAsync` concurrently
- Challenge derivation functions are stateless and thread-safe

### Anti-Replay Protection

Each challenge is unique to its epoch:
- Challenge = SHA256(previousBlockHash || epochNumber)
- Old challenges cannot be reused in new epochs
- Proofs from expired epochs are rejected

```csharp
// Challenge from epoch 5 is invalid for epoch 6
var epoch5Challenge = ChallengeDerivation.DeriveChallenge(blockHash, 5);
var isValidForEpoch6 = epochManager.ValidateChallengeForEpoch(
    epoch5Challenge, 6, blockHash);
// Returns false - prevents replay attacks
```

## Dependencies

- `Spacetime.Common` - Shared utilities
- `MerkleTree` - Merkle tree construction and verification (v1.0.0-beta.1)

## Thread Safety

All classes are thread-safe for reading after construction. The `BlockHeader.SetSignature()` and `Transaction.SetSignature()` methods are the only mutation operations and should only be called once before sharing the objects across threads.

The `BlockBuilder` itself is thread-safe and can be shared across multiple threads, but each block building operation should be independent.
