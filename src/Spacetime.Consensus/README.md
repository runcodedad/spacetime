# Spacetime.Consensus

Consensus logic, proof validation, and difficulty adjustment for the Spacetime blockchain.

## Overview

This project contains the core consensus mechanisms for the Spacetime Proof-of-Space-Time blockchain, including:

- **Proof Validation**: Cryptographic verification of miner proofs
- **Score Calculation**: Computing proof scores from challenges and plot leaves
- **Difficulty Target Management**: Validation against difficulty thresholds
- **Merkle Path Verification**: Integration with MerkleTree library for proof paths

## Architecture

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

## Future Development

- **Difficulty Adjustment Algorithm**: Implement conversion from integer difficulty to 32-byte target
- **Dynamic Difficulty**: Adjust difficulty based on block time and network hash rate
- **Consensus Rules**: Additional validation rules for block acceptance
- **Fork Choice**: Logic for selecting the canonical chain

## Dependencies

- **Spacetime.Common** - Shared utilities
- **Spacetime.Plotting** - Proof data structures
- **MerkleTree** - Merkle tree proof verification

## Testing

- **Unit Tests**: `tests/Spacetime.Consensus.Tests/` - 34 tests covering score calculation, target comparison, validation logic
- **Integration Tests**: `tests/Spacetime.Consensus.IntegrationTests/` - 9 tests with real plot generation and end-to-end validation
