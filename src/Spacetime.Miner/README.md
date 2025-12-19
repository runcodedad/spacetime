# Spacetime.Miner

The miner node implementation for the Spacetime blockchain.

## Overview

`Spacetime.Miner` implements the main event loop for miner nodes that:
- Load and manage plot files
- Connect to full nodes or validators
- Listen for new challenges (via BlockAccepted messages)
- Generate proofs from plots in response to challenges
- Submit proofs to the network
- Build and broadcast blocks when winning

## Architecture

### Core Components

#### MinerConfiguration
Configuration settings for the miner including:
- Plot directory and metadata paths
- Node connection settings
- Network ID
- Performance tuning parameters
- Monitoring settings

#### MinerEventLoop
Main event loop that orchestrates mining operations:
- **Boot Sequence**: Loads plots and connects to node
- **Challenge Handling**: Derives new challenges from block acceptances
- **Proof Generation**: Coordinates parallel proof generation across plots
- **Proof Submission**: Submits winning proofs to network
- **Block Building**: Builds blocks when proof wins
- **Error Recovery**: Handles connection failures and retries

## Usage

### Basic Example

```csharp
using MerkleTree.Hashing;
using Spacetime.Core;
using Spacetime.Miner;
using Spacetime.Network;
using Spacetime.Plotting;

// Configure miner
var config = new MinerConfiguration
{
    PlotDirectory = "./plots",
    PlotMetadataPath = "./plots_metadata.json",
    NodeAddress = "127.0.0.1",
    NodePort = 8333,
    PrivateKeyPath = "./miner_key.dat",
    NetworkId = "mainnet",
    MaxConcurrentProofs = 4,
    ProofGenerationTimeoutSeconds = 60
};

// Create dependencies
var hashFunction = new Sha256HashFunction();
var plotManager = new PlotManager(hashFunction, config.PlotMetadataPath);
var epochManager = new EpochManager();
// ... create other dependencies

// Create and start miner
var miner = new MinerEventLoop(
    config,
    plotManager,
    epochManager,
    connectionManager,
    messageRelay,
    blockSigner,
    blockValidator,
    mempool,
    hashFunction);

await miner.StartAsync();

// Miner is now running - it will:
// 1. Load plots from the plot directory
// 2. Connect to the specified node
// 3. Listen for block acceptances
// 4. Generate proofs for new challenges
// 5. Submit proofs when found
// 6. Build blocks when winning

// Stop gracefully
await miner.StopAsync();
await miner.DisposeAsync();
```

### Configuration Options

| Property | Default | Description |
|----------|---------|-------------|
| `PlotDirectory` | required | Directory containing plot files |
| `PlotMetadataPath` | required | Path to plot metadata JSON |
| `NodeAddress` | required | Address of full node |
| `NodePort` | required | Port of full node |
| `PrivateKeyPath` | required | Path to miner's private key |
| `NetworkId` | required | Network identifier |
| `MaxConcurrentProofs` | 1 | Max parallel proof generation tasks |
| `ProofGenerationTimeoutSeconds` | 60 | Timeout for proof generation |
| `ConnectionRetryIntervalSeconds` | 5 | Retry interval for connections |
| `MaxConnectionRetries` | 10 | Max connection retry attempts |
| `EnablePerformanceMonitoring` | true | Enable detailed monitoring |

## Mining Flow

1. **Initialization**
   - Load plot metadata from disk
   - Discover and load plot files
   - Validate plots are readable

2. **Connection**
   - Connect to full node/validator
   - Retry on failure with exponential backoff
   - Fail after max retries

3. **Event Loop**
   ```
   Loop:
     - Listen for BlockAccepted messages
     - On new block:
       * Derive challenge from block hash
       * Advance epoch
       * Generate proofs from all plots (parallel)
       * Track best proof
       * Submit proof if meets threshold
       * Build block if winning
       * Broadcast block
     - Handle errors and reconnect if needed
   ```

4. **Proof Generation**
   - Use sampling strategy for fast scanning
   - Scan plots in parallel
   - Compute score = H(challenge || leaf)
   - Generate Merkle proof for best leaf
   - Track best proof across all plots

5. **Block Building**
   - Collect transactions from mempool
   - Compute transaction Merkle root
   - Sign block header
   - Validate block
   - Broadcast to network

## Performance Monitoring

When `EnablePerformanceMonitoring` is true, the miner tracks:
- `TotalChallengesReceived`: Number of challenges received
- `TotalProofsGenerated`: Number of proofs generated
- `TotalProofsSubmitted`: Number of proofs submitted
- `TotalBlocksWon`: Number of blocks won and built

Progress is reported during proof generation with time estimates.

## Error Handling

The miner implements robust error handling:

### Connection Failures
- Automatic retry with configurable interval
- Max retry limit to prevent infinite loops
- Reconnection during event loop if connection lost

### Proof Generation Failures
- Timeout protection per proof attempt
- Continue with other plots if one fails
- Log errors but don't crash the miner

### Block Building Failures
- Validate block before broadcasting
- Log detailed error on failure
- Continue mining on next challenge

## Thread Safety

The `MinerEventLoop` is designed to be thread-safe:
- Concurrent proof generation is controlled by semaphore
- Best proof tracking uses locks
- Event loop runs on dedicated task
- Safe shutdown with cancellation tokens

## Testing

### Unit Tests
Located in `tests/Spacetime.Miner.Tests/`:
- Configuration validation tests
- Constructor validation tests
- State tracking tests
- Error handling tests

Run with: `dotnet test tests/Spacetime.Miner.Tests/`

### Integration Tests
Located in `tests/Spacetime.Miner.IntegrationTests/`:
- Boot sequence tests
- Connection retry tests
- Component interaction tests
- Graceful shutdown tests

Run with: `dotnet test tests/Spacetime.Miner.IntegrationTests/`

## Dependencies

- **Spacetime.Core**: Block structures, epoch management
- **Spacetime.Network**: P2P networking, message relay
- **Spacetime.Plotting**: Plot management, proof generation
- **Spacetime.Consensus**: Block validation, mempool
- **MerkleTree**: Merkle tree operations

## Future Enhancements

Potential improvements for future versions:
- Pool mining support
- GPU-accelerated proof search
- Adaptive scanning strategies based on network difficulty
- Plot quality metrics and auto-selection
- REST API for remote monitoring
- Prometheus metrics export
- Hot-reload of configuration
- Multi-node connection for redundancy
