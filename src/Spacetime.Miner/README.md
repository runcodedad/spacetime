# Spacetime.Miner

The miner node implementation and CLI for the Spacetime blockchain.

## Overview

`Spacetime.Miner` provides:
- **Command-line interface** for plot management and mining operations
- **Mining event loop** for automated proof generation and block building
- **Configuration management** with YAML files and environment variables

The miner:
- Loads and manages plot files
- Connects to full nodes or validators
- Listens for new challenges (via BlockAccepted messages)
- Generates proofs from plots in response to challenges
- Submits proofs to the network
- Builds and broadcasts blocks when winning

## Command-Line Interface

### Installation

```bash
dotnet build src/Spacetime.Miner
dotnet run --project src/Spacetime.Miner -- --help
```

### Commands

#### create-plot

Create a new plot file.

```bash
spacetime-miner create-plot [options]
```

**Options:**
- `--size, -s <size>` - Plot size in gigabytes (default: 1)
- `--output, -o <path>` - Output file path (optional)
- `--config, -c <path>` - Path to configuration file
- `--cache` - Include Merkle tree cache for faster proof generation
- `--cache-levels <levels>` - Number of Merkle tree levels to cache (default: 5)

**Examples:**

```bash
# Create a 1 GB plot
spacetime-miner create-plot

# Create a 10 GB plot with cache
spacetime-miner create-plot --size 10 --cache

# Create a plot with custom output path
spacetime-miner create-plot --size 5 --output my_plot.plot
```

#### list-plots

Show all registered plots.

```bash
spacetime-miner list-plots [options]
```

**Options:**
- `--config, -c <path>` - Path to configuration file
- `--verbose, -v` - Show detailed information

**Example:**

```bash
spacetime-miner list-plots --verbose
```

#### delete-plot

Remove a plot from the miner.

```bash
spacetime-miner delete-plot <plot-id> [options]
```

**Arguments:**
- `<plot-id>` - The ID of the plot to delete (GUID)

**Options:**
- `--config, -c <path>` - Path to configuration file
- `--delete-file` - Also delete the plot file from disk
- `--force, -f` - Skip confirmation prompt

**Examples:**

```bash
# Remove plot from manager (keeps file on disk)
spacetime-miner delete-plot 550e8400-e29b-41d4-a716-446655440000

# Remove plot and delete file
spacetime-miner delete-plot 550e8400-e29b-41d4-a716-446655440000 --delete-file
```

#### start

Start the mining process.

```bash
spacetime-miner start [options]
```

**Options:**
- `--config, -c <path>` - Path to configuration file
- `--daemon, -d` - Run as background daemon (not yet implemented)

**Example:**

```bash
spacetime-miner start
```

#### stop

Stop the mining process (not yet fully implemented).

```bash
spacetime-miner stop
```

#### status

Show mining status and statistics.

```bash
spacetime-miner status [options]
```

**Options:**
- `--config, -c <path>` - Path to configuration file

**Example:**

```bash
spacetime-miner status
```

## Configuration

The miner uses a YAML configuration file located at `~/.spacetime/miner.yaml` by default.

### Configuration File Format

```yaml
plotDirectory: /path/to/plots
plotMetadataPath: /path/to/plots_metadata.json
nodeAddress: 127.0.0.1
nodePort: 8333
privateKeyPath: /path/to/miner_key.dat
networkId: testnet
maxConcurrentProofs: 1
proofGenerationTimeoutSeconds: 60
connectionRetryIntervalSeconds: 5
maxConnectionRetries: 10
enablePerformanceMonitoring: true
```

### Configuration Options

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
| `plotDirectory` | `~/.spacetime/plots` | Directory containing plot files |
| `plotMetadataPath` | `~/.spacetime/plots_metadata.json` | Path to plot metadata JSON |
| `nodeAddress` | `127.0.0.1` | Address of full node |
| `nodePort` | `8333` | Port of full node |
| `privateKeyPath` | `~/.spacetime/miner_key.dat` | Path to miner's private key |
| `networkId` | `testnet` | Network identifier |
| `maxConcurrentProofs` | `1` | Max parallel proof generation tasks |
| `proofGenerationTimeoutSeconds` | `60` | Timeout for proof generation |
| `connectionRetryIntervalSeconds` | `5` | Retry interval for connections |
| `maxConnectionRetries` | `10` | Max connection retry attempts |
| `enablePerformanceMonitoring` | `true` | Enable detailed monitoring |

### Environment Variables

You can override configuration values using environment variables with the prefix `SPACETIME_MINER_`:

- `SPACETIME_MINER_PLOT_DIRECTORY`
- `SPACETIME_MINER_PLOT_METADATA_PATH`
- `SPACETIME_MINER_NODE_ADDRESS`
- `SPACETIME_MINER_NODE_PORT`
- `SPACETIME_MINER_PRIVATE_KEY_PATH`
- `SPACETIME_MINER_NETWORK_ID`
- `SPACETIME_MINER_MAX_CONCURRENT_PROOFS`
- `SPACETIME_MINER_PROOF_GENERATION_TIMEOUT_SECONDS`
- `SPACETIME_MINER_CONNECTION_RETRY_INTERVAL_SECONDS`
- `SPACETIME_MINER_MAX_CONNECTION_RETRIES`
- `SPACETIME_MINER_ENABLE_PERFORMANCE_MONITORING`

## Architecture

### Core Components

#### ConfigurationLoader
Loads configuration from YAML files and applies environment variable overrides.

#### MinerConfiguration
Configuration settings for the miner including plot directory, node connection, and performance parameters.

#### MinerEventLoop
Main event loop that orchestrates mining operations:
- **Boot Sequence**: Loads plots and connects to node
- **Challenge Handling**: Derives new challenges from block acceptances
- **Proof Generation**: Coordinates parallel proof generation across plots
- **Proof Submission**: Submits winning proofs to network
- **Block Building**: Builds blocks when proof wins
- **Error Recovery**: Handles connection failures and retries

#### CLI Commands
- `CreatePlotCommand` - Creates new plots
- `ListPlotsCommand` - Lists existing plots
- `DeletePlotCommand` - Removes plots
- `StartCommand` - Starts mining
- `StopCommand` - Stops mining
- `StatusCommand` - Shows status

## Programmatic Usage

### Basic Example

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
