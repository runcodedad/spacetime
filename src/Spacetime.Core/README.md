# Spacetime.Core

Core blockchain data structures for the Spacetime blockchain.

## Overview

This project provides the fundamental data structures for the Spacetime Proof-of-Space-Time blockchain:

- **Block** - Complete block combining header and body
- **BlockHeader** - Block metadata, consensus data, and authentication
- **BlockBody** - Transactions and PoST proof
- **BlockProof** - Proof-of-Space-Time proof data
- **BlockPlotMetadata** - Plot file metadata included in proofs

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
| transactions[] | byte[][] | List of transactions (length-prefixed) |
| proof | BlockProof | The winning PoST proof |

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

### Creating a New Block

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

## Design Decisions

1. **Immutability**: All fields are read-only after construction, except for the signature which can be set once.

2. **Validation**: Constructors validate all inputs immediately, throwing `ArgumentException` or `ArgumentNullException` for invalid data.

3. **Binary Serialization**: Uses `BinaryWriter`/`BinaryReader` for efficient binary serialization with explicit little-endian byte order.

4. **Defensive Copying**: All byte arrays are copied on construction to prevent external modification.

5. **Immutable Properties**: Hash fields are exposed as `byte[]` properties with private or no setters to prevent modification after construction.

## Dependencies

- `Spacetime.Common` - Shared utilities

## Thread Safety

All classes are thread-safe for reading after construction. The `BlockHeader.SetSignature()` method is the only mutation operation and should only be called once before sharing the header across threads.
