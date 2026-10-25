# Spacetime Miner User Guide

This guide covers installation, configuration, and operation of the Spacetime miner.

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Plot Management](#plot-management)
- [Mining Operations](#mining-operations)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)

## Installation

### Prerequisites

- .NET 10 SDK or later
- Sufficient disk space for plots (minimum 100 MB per plot, recommended 1+ GB)
- Fast storage (SSD recommended for optimal performance)

### Building from Source

```bash
git clone https://github.com/runcodedad/spacetime.git
cd spacetime
dotnet build src/Spacetime.Miner -c Release
```

### Running the Miner

```bash
# Run directly
dotnet run --project src/Spacetime.Miner -- [command] [options]

# Or build and run the executable
dotnet build src/Spacetime.Miner -c Release
cd src/Spacetime.Miner/bin/Release/net10.0
./Spacetime.Miner [command] [options]
```

## Quick Start

### 1. Check Status

This will create a default configuration file if one doesn't exist:

```bash
spacetime-miner status
```

### 2. Create Your First Plot

Create a 1 GB plot:

```bash
spacetime-miner create-plot --size 1
```

This will:
- Generate a plot file in the configured plot directory
- Create plot metadata
- Display the plot ID and Merkle root

### 3. Verify Your Plots

```bash
spacetime-miner list-plots
```

### 4. Start Mining

```bash
spacetime-miner start
```

Press Ctrl+C to stop.

## Configuration

### Configuration File Location

The default configuration file is located at:
- **Linux/macOS**: `~/.spacetime/miner.yaml`
- **Windows**: `%USERPROFILE%\.spacetime\miner.yaml`

You can specify a different location with the `--config` option:

```bash
spacetime-miner status --config /path/to/config.yaml
```

### Configuration File Format

See [docs/miner-configuration-sample.yaml](miner-configuration-sample.yaml) for a complete example.

Required fields:
```yaml
plotDirectory: /path/to/plots
plotMetadataPath: /path/to/plots_metadata.json
nodeAddress: 127.0.0.1
nodePort: 8333
privateKeyPath: /path/to/miner_key.dat
networkId: testnet
```

Optional fields with defaults:
```yaml
maxConcurrentProofs: 1
proofGenerationTimeoutSeconds: 60
connectionRetryIntervalSeconds: 5
maxConnectionRetries: 10
enablePerformanceMonitoring: true
```

### Environment Variables

Override configuration values with environment variables:

```bash
export SPACETIME_MINER_NODE_ADDRESS=192.168.1.100
export SPACETIME_MINER_NODE_PORT=9000
export SPACETIME_MINER_NETWORK_ID=mainnet
spacetime-miner start
```

Environment variables take precedence over the configuration file.

## Plot Management

### Creating Plots

**Basic plot creation:**
```bash
spacetime-miner create-plot --size 5
```

**With Merkle tree cache (faster proof generation):**
```bash
spacetime-miner create-plot --size 5 --cache --cache-levels 5
```

**Custom output path:**
```bash
spacetime-miner create-plot --size 5 --output /custom/path/my_plot.plot
```

### Plot Size Recommendations

| Use Case | Recommended Size | Notes |
|----------|------------------|-------|
| Testing | 1-5 GB | Quick to create, fast proof generation |
| Home Mining | 10-50 GB | Good balance of space and performance |
| Dedicated Mining | 100+ GB | Maximum mining power per plot |

**Tips:**
- Create multiple smaller plots rather than one large plot for better parallel performance
- Use `--cache` for plots you'll actively mine with
- SSD storage provides better proof generation performance than HDD

### Listing Plots

**Simple list:**
```bash
spacetime-miner list-plots
```

**Detailed information:**
```bash
spacetime-miner list-plots --verbose
```

Shows:
- Plot ID (GUID)
- Status (Valid, Invalid, Missing)
- File path
- Size
- Creation date
- Merkle root (in verbose mode)
- Cache file path (if applicable, in verbose mode)

### Deleting Plots

**Remove from manager (keeps file):**
```bash
spacetime-miner delete-plot <plot-id>
```

**Remove from manager and delete file:**
```bash
spacetime-miner delete-plot <plot-id> --delete-file
```

**Skip confirmation:**
```bash
spacetime-miner delete-plot <plot-id> --delete-file --force
```

## Mining Operations

### Starting the Miner

```bash
spacetime-miner start
```

What happens when you start:
1. Configuration is loaded
2. Plots are loaded from the plot directory
3. Connection is established to the full node
4. Miner listens for new block challenges
5. For each challenge:
   - Scans plots to find the best proof
   - Submits proof if it meets the difficulty threshold
   - Builds and broadcasts block if proof wins

### Monitoring Status

While mining is running (in another terminal):
```bash
spacetime-miner status
```

Shows:
- Configuration settings
- Number of plots (total and valid)
- Total space allocated
- Mining status (running/stopped)

### Stopping the Miner

Press **Ctrl+C** in the terminal where the miner is running for graceful shutdown.

## Troubleshooting

### Configuration file not found

**Problem:** Configuration file doesn't exist.

**Solution:** The miner will automatically create a default configuration file. Edit it to match your setup:

```bash
nano ~/.spacetime/miner.yaml
```

### Cannot connect to node

**Problem:** `Failed to connect to node at 127.0.0.1:8333`

**Solutions:**
1. Verify the full node is running
2. Check the `nodeAddress` and `nodePort` in configuration
3. Check firewall settings
4. Verify network connectivity

### Plot creation fails

**Problem:** Plot creation fails with disk space or permission error.

**Solutions:**
1. Verify sufficient disk space: `df -h`
2. Check plot directory exists: `mkdir -p /path/to/plots`
3. Verify write permissions: `ls -la /path/to/plots`
4. Ensure minimum plot size (100 MB)

### No plots found

**Problem:** `list-plots` shows no plots after creating them.

**Solutions:**
1. Verify plot metadata file exists and is readable
2. Check plot directory configuration
3. Try creating a new plot with `create-plot`

### Plot status shows "Invalid" or "Missing"

**Problem:** Plots show as invalid or missing in `list-plots`.

**Solutions:**
1. Verify plot files exist at the specified paths
2. Check file permissions
3. Verify plots weren't moved or deleted
4. Remove invalid plots and recreate: `delete-plot <id> && create-plot`

### Proof generation is slow

**Problem:** Proof generation takes too long.

**Solutions:**
1. Create plots with `--cache` option for faster lookups
2. Move plots to SSD storage
3. Reduce plot size or create multiple smaller plots
4. Increase `maxConcurrentProofs` in configuration (multi-core systems)

## Best Practices

### Plot Management

1. **Create multiple plots**: Better parallel performance than one large plot
2. **Use SSD storage**: Significant performance improvement over HDD
3. **Enable caching**: Use `--cache` for actively mined plots
4. **Regular backups**: Back up plot metadata file to avoid losing plot references
5. **Monitor disk space**: Ensure sufficient space for new plots

### Configuration

1. **Use environment variables for secrets**: Avoid storing keys in configuration
2. **Tune concurrent proofs**: Start with 1, increase if CPU cores are underutilized
3. **Adjust timeouts based on hardware**: Slower systems may need longer timeouts
4. **Enable performance monitoring**: Track success rates and optimize

### Mining

1. **Start with testnet**: Test your setup before joining mainnet
2. **Monitor node connection**: Ensure stable connection to full node
3. **Track success rate**: Monitor how often your proofs are accepted
4. **Upgrade storage as needed**: More space = more plots = higher mining power

### Security

1. **Protect private keys**: Store miner keys securely
2. **Use dedicated machine**: Avoid running miner on shared systems
3. **Regular updates**: Keep miner software up to date
4. **Firewall configuration**: Only allow necessary network connections

## Advanced Topics

### Multi-Node Setup

Run multiple miners with different plot sets:

```bash
# Miner 1
spacetime-miner start --config /path/to/miner1.yaml

# Miner 2
spacetime-miner start --config /path/to/miner2.yaml
```

Each miner can:
- Use different plot directories
- Connect to different nodes
- Use different key files

### Performance Tuning

**Maximize throughput:**
```yaml
maxConcurrentProofs: 4  # Match CPU core count
proofGenerationTimeoutSeconds: 30  # Shorter timeout
```

**Optimize for reliability:**
```yaml
maxConcurrentProofs: 1  # Sequential processing
proofGenerationTimeoutSeconds: 120  # Longer timeout
connectionRetryIntervalSeconds: 10
maxConnectionRetries: 20
```

### Automation

**Run as systemd service (Linux):**

Create `/etc/systemd/system/spacetime-miner.service`:

```ini
[Unit]
Description=Spacetime Miner
After=network.target

[Service]
Type=simple
User=miner
WorkingDirectory=/opt/spacetime
ExecStart=/usr/bin/dotnet /opt/spacetime/Spacetime.Miner.dll start
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl enable spacetime-miner
sudo systemctl start spacetime-miner
sudo systemctl status spacetime-miner
```

## Getting Help

- **Documentation**: See [src/Spacetime.Miner/README.md](../src/Spacetime.Miner/README.md)
- **Issues**: Report bugs at https://github.com/runcodedad/spacetime/issues
- **CLI Help**: Run `spacetime-miner --help` or `spacetime-miner <command> --help`

## Next Steps

- Learn about [Proof-of-Space-Time consensus](requirements.md)
- Understand [plot file internals](../src/Spacetime.Plotting/README.md)
- Explore [network protocol](network-protocol.md)
