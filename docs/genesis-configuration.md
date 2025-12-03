# Genesis Block Configuration Guide

This guide explains how to create and configure genesis blocks for Spacetime blockchain networks.

## Overview

The genesis block is the first block in a blockchain and defines the initial state and parameters for a network. Different networks (mainnet, testnet, devnet) have different genesis configurations.

## Genesis Configuration Parameters

A genesis configuration (`GenesisConfig`) includes the following parameters:

| Parameter | Type | Description |
|-----------|------|-------------|
| `NetworkId` | string | Unique identifier for the network (e.g., "spacetime-mainnet-v1") |
| `InitialTimestamp` | long | Unix epoch timestamp for the genesis block |
| `InitialDifficulty` | long | Starting difficulty target for proof validation |
| `InitialEpoch` | long | Starting epoch number (typically 0) |
| `EpochDurationSeconds` | int | Duration of each epoch in seconds |
| `TargetBlockTime` | int | Target time between blocks in seconds |
| `PreminedAllocations` | Dictionary<string, long> | Initial token allocations (public key → balance) |
| `Description` | string? | Optional description of the network |

## Predefined Network Configurations

### Mainnet

Production network with secure parameters:

```csharp
var config = GenesisConfigs.Mainnet;

// Network: spacetime-mainnet-v1
// Difficulty: 1,000,000
// Block Time: 30 seconds
// Epoch Duration: 30 seconds
// Premine: None
```

### Testnet

Test network with moderate parameters:

```csharp
var config = GenesisConfigs.Testnet;

// Network: spacetime-testnet-v1
// Difficulty: 10,000
// Block Time: 30 seconds
// Epoch Duration: 30 seconds
// Premine: None
```

### Development

Local development network with fast parameters:

```csharp
var config = GenesisConfigs.Development;

// Network: spacetime-devnet-local
// Difficulty: 100
// Block Time: 10 seconds
// Epoch Duration: 10 seconds
// Premine: None
```

## Creating a Custom Genesis Configuration

### Basic Custom Configuration

```csharp
using Spacetime.Core;

var config = GenesisConfigs.CreateCustom(
    networkId: "my-custom-network");
```

### Advanced Custom Configuration

```csharp
using Spacetime.Core;

// Create premine allocations (optional)
var premine = new Dictionary<string, long>
{
    ["03a1b2c3d4e5f6..."] = 1_000_000, // Public key (hex) → balance
    ["02f1e2d3c4b5a6..."] = 500_000
};

var config = GenesisConfigs.CreateCustom(
    networkId: "my-custom-network",
    timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    difficulty: 5000,
    epochDuration: 45,
    targetBlockTime: 45,
    preminedAllocations: premine,
    description: "Custom test network for experimentation");
```

## Generating a Genesis Block

### Step 1: Create Configuration

```csharp
var config = GenesisConfigs.Testnet;
```

### Step 2: Create Block Signer

```csharp
// Implementation depends on your key management system
IBlockSigner signer = new YourBlockSigner(privateKey);
```

### Step 3: Generate Genesis Block

```csharp
var generator = new GenesisBlockGenerator(signer);
var genesisBlock = await generator.GenerateGenesisBlockAsync(config);
```

### Step 4: Serialize for Storage or Distribution

```csharp
var blockBytes = genesisBlock.Serialize();
File.WriteAllBytes("genesis.block", blockBytes);
```

## Validating a Genesis Block

```csharp
var validator = new GenesisBlockValidator();
var config = GenesisConfigs.Testnet;

// Load or receive a genesis block
var block = Block.Deserialize(blockBytes);

// Validate against configuration
bool isValid = await validator.ValidateGenesisBlockAsync(block, config);

if (!isValid)
{
    throw new InvalidOperationException("Invalid genesis block");
}
```

## Genesis Block Properties

A valid genesis block has the following properties:

- **Height**: Always 0
- **Parent Hash**: All zeros (32 bytes)
- **Plot Root**: All zeros (32 bytes)
- **Proof Score**: All zeros (32 bytes)
- **Challenge**: SHA256 hash of the network ID
- **Timestamp**: Matches configuration
- **Difficulty**: Matches configuration
- **Epoch**: Matches configuration (typically 0)
- **Signature**: Valid ECDSA signature from genesis signer

## Security Considerations

### 1. Genesis Signer Private Key

- The genesis signer's private key should be generated securely
- For mainnet, use hardware security modules (HSM) or secure key generation
- For testnet/devnet, standard secure random generation is acceptable
- Never reuse mainnet keys for test networks

### 2. Network ID Selection

- Use descriptive, unique network IDs
- Include version numbers for future upgrades (e.g., "spacetime-mainnet-v1")
- Avoid names that could conflict with other networks

### 3. Initial Difficulty

- Mainnet: High difficulty for security (≥1,000,000)
- Testnet: Moderate difficulty for testing (≥10,000)
- Devnet: Low difficulty for development (≥100)

### 4. Premine Allocations

- Use premine sparingly and transparently
- Document all premined allocations
- Ensure public keys are properly formatted (33 bytes, hex-encoded)
- Validate total premine supply against economic model

### 5. Timestamp Selection

- Mainnet: Use a predetermined launch time
- Testnet: Use current time or near-future time
- Devnet: Use current time

## Best Practices

1. **Configuration Management**
   - Store genesis configurations in version control
   - Document the rationale for each parameter choice
   - Use configuration files (JSON) for deployment

2. **Testing**
   - Always test genesis generation in devnet first
   - Validate genesis blocks against expected properties
   - Test network startup with generated genesis

3. **Distribution**
   - Distribute genesis blocks along with configuration
   - Provide checksums (SHA256) for verification
   - Document the genesis block hash prominently

4. **Versioning**
   - Include version information in network IDs
   - Plan for potential network upgrades
   - Document breaking changes between versions

## Example: Complete Genesis Setup

```csharp
using Spacetime.Core;
using System.Security.Cryptography;

// 1. Create configuration
var config = new GenesisConfig
{
    NetworkId = "my-network-v1",
    InitialTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    InitialDifficulty = 10_000,
    InitialEpoch = 0,
    EpochDurationSeconds = 30,
    TargetBlockTime = 30,
    PreminedAllocations = new Dictionary<string, long>(),
    Description = "My custom Spacetime network"
};

// 2. Validate configuration
config.Validate();

// 3. Create signer (implementation-specific)
IBlockSigner signer = CreateBlockSigner();

// 4. Generate genesis block
var generator = new GenesisBlockGenerator(signer);
var genesisBlock = await generator.GenerateGenesisBlockAsync(config);

// 5. Validate genesis block
var validator = new GenesisBlockValidator();
bool isValid = await validator.ValidateGenesisBlockAsync(genesisBlock, config);

if (!isValid)
{
    throw new InvalidOperationException("Generated genesis block is invalid");
}

// 6. Compute and display genesis hash
var genesisHash = genesisBlock.ComputeHash();
Console.WriteLine($"Genesis Block Hash: {Convert.ToHexString(genesisHash)}");

// 7. Serialize and save
var blockBytes = genesisBlock.Serialize();
await File.WriteAllBytesAsync("genesis.block", blockBytes);

Console.WriteLine("Genesis block created successfully!");
```

## Troubleshooting

### Configuration Validation Fails

**Problem**: `config.Validate()` throws an exception

**Solutions**:
- Ensure NetworkId is not null or empty
- Verify all numeric values are positive
- Check that EpochDurationSeconds and TargetBlockTime are > 0
- Validate premine allocation keys are not empty

### Genesis Block Validation Fails

**Problem**: `ValidateGenesisBlockAsync` returns false

**Common Causes**:
- Block height is not 0
- Parent hash is not all zeros
- Challenge doesn't match network ID hash
- Timestamp doesn't match configuration
- Block is not signed

### Challenge Mismatch

**Problem**: Genesis challenge doesn't match expected value

**Solution**: The challenge is deterministic:
```csharp
var expectedChallenge = SHA256.HashData(
    System.Text.Encoding.UTF8.GetBytes(config.NetworkId));
```

## References

- [Spacetime Core README](../src/Spacetime.Core/README.md)
- [Requirements Document](./requirements.md)
- [Discovery Notes](./discovery-notes.md)
