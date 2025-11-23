# Spacetime.Plotting

**Deterministic plot file generation for Proof-of-Space-Time consensus**

## Overview

`Spacetime.Plotting` is responsible for creating and managing plot files — large, disk-based data structures that miners use to participate in the Spacetime blockchain's Proof-of-Space-Time (PoST) consensus mechanism.

A plot file contains:
- Deterministic leaf values generated from a miner's public key and plot seed
- A Merkle tree structure built from those leaves
- Metadata including the Merkle root hash for proof verification
- Optional cached Merkle tree layers for faster proof generation

## Key Components

### PlotCreator

The main entry point for creating plot files. Coordinates leaf generation, file writing, and Merkle tree construction in a single streaming operation.

**Key Features:**
- Asynchronous streaming generation to minimize memory usage
- Progress reporting for long-running plot creation
- Configurable Merkle tree caching for performance
- Atomic file creation with header checksums

**Example:**
```csharp
using Spacetime.Plotting;
using MerkleTree.Hashing;

// Setup
var hashFunction = new Sha256HashFunction();
var plotCreator = new PlotCreator(hashFunction);

// Generate a 32-byte random plot seed
var plotSeed = new byte[32];
using (var rng = RandomNumberGenerator.Create())
{
    rng.GetBytes(plotSeed);
}

// Configure a 1 GB plot
var config = PlotConfiguration.CreateFromGB(
    sizeInGB: 1,
    minerPublicKey: yourPublicKey, // 32 bytes
    plotSeed: plotSeed,
    outputPath: "plots/my-plot.dat",
    includeCache: true,
    cacheLevels: 5
);

// Create the plot with progress reporting
var progress = new Progress<double>(p => Console.WriteLine($"Progress: {p:F1}%"));
var header = await plotCreator.CreatePlotAsync(config, progress, cancellationToken);

Console.WriteLine($"Plot created with Merkle root: {BitConverter.ToString(header.MerkleRoot)}");
```

### PlotConfiguration

Immutable configuration for plot creation with validation.

**Properties:**
- `PlotSizeBytes` — Total size of the plot file
- `LeafCount` — Number of 32-byte leaves (calculated from size)
- `MinerPublicKey` — 32-byte public key identifying the miner
- `PlotSeed` — 32-byte random seed for deterministic generation
- `OutputPath` — File system path for the plot file
- `IncludeCache` — Whether to generate Merkle cache layers
- `CacheLevels` — Number of top tree levels to cache (default: 5)

**Validation:**
- Minimum plot size: 100 MB
- Public key and seed must be exactly 32 bytes
- Cache levels must be non-negative

**Factory Methods:**
```csharp
// Create from bytes
var config = new PlotConfiguration(
    plotSizeBytes: 1_073_741_824L, // 1 GB
    minerPublicKey: publicKey,
    plotSeed: seed,
    outputPath: "my-plot.dat"
);

// Create from gigabytes (convenience method)
var config = PlotConfiguration.CreateFromGB(
    sizeInGB: 10,
    minerPublicKey: publicKey,
    plotSeed: seed,
    outputPath: "large-plot.dat"
);
```

### LeafGenerator

Static utility for deterministic leaf generation.

**Algorithm:**
```
leaf = SHA256(minerPublicKey || plotSeed || nonce)
```

This ensures:
- **Determinism** — Same inputs always produce the same leaf
- **Uniqueness** — Different miners or seeds produce different plots
- **Collision Resistance** — SHA-256 properties prevent practical collisions

**Usage:**
```csharp
// Generate a single leaf
byte[] leaf = LeafGenerator.GenerateLeaf(
    minerPublicKey: publicKey,
    plotSeed: seed,
    nonce: 12345
);

// Generate leaves asynchronously (streaming)
await foreach (var leaf in LeafGenerator.GenerateLeavesAsync(
    minerPublicKey: publicKey,
    plotSeed: seed,
    startNonce: 0,
    count: 1_000_000,
    onLeafGenerated: () => progressCounter++,
    cancellationToken: cancellationToken))
{
    // Process each leaf
}
```

### PlotLoader

Loads and validates plot files, providing efficient random access to leaves.

**Key Features:**
- Reads and validates plot file headers (including checksum verification)
- Memory-efficient random access to individual leaves
- Optional Merkle root verification for integrity checking
- Shared reading support (multiple loaders on same file)
- Proper async disposal for file handle management
- Clear error messages for corrupted or invalid files

**Usage:**
```csharp
using Spacetime.Plotting;
using MerkleTree.Hashing;

// Load a plot file
var hashFunction = new Sha256HashFunction();
await using var loader = await PlotLoader.LoadAsync("my-plot.dat", hashFunction);

// Access metadata
Console.WriteLine($"Leaf count: {loader.LeafCount:N0}");
Console.WriteLine($"Tree height: {loader.TreeHeight}");
Console.WriteLine($"Merkle root: {BitConverter.ToString(loader.MerkleRoot.ToArray())}");

// Read specific leaves
var leaf = await loader.ReadLeafAsync(42);
Console.WriteLine($"Leaf 42: {BitConverter.ToString(leaf)}");

// Read multiple consecutive leaves
var leaves = await loader.ReadLeavesAsync(startIndex: 0, count: 10);

// Optionally verify Merkle root (expensive - scans entire plot)
var progress = new Progress<double>(p => Console.WriteLine($"Verification: {p:F1}%"));
var isValid = await loader.VerifyMerkleRootAsync(progress);
if (!isValid)
{
    Console.WriteLine("WARNING: Plot file is corrupted!");
}
```

**Error Handling:**
```csharp
try
{
    await using var loader = await PlotLoader.LoadAsync("my-plot.dat", hashFunction);
    // Use loader...
}
catch (FileNotFoundException)
{
    Console.WriteLine("Plot file not found");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("checksum"))
{
    Console.WriteLine("Plot file has invalid checksum - file is corrupted");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("truncated"))
{
    Console.WriteLine("Plot file is incomplete or truncated");
}
```

### PlotHeader

Binary header structure stored at the beginning of each plot file.

**Structure:**
```
[4 bytes]  Magic bytes: "SPTP"
[1 byte]   Format version: 1
[32 bytes] Plot seed
[8 bytes]  Leaf count
[4 bytes]  Leaf size (always 32)
[8 bytes]  Merkle tree height
[32 bytes] Merkle root hash
[32 bytes] SHA-256 checksum (of above fields)
Total: 121 bytes
```

**Features:**
- Version byte for forward compatibility
- Checksum validation to detect corruption
- Serialization/deserialization for disk storage
- Integrity verification before plot usage

**Usage:**
```csharp
// Create a new header (typically done by PlotCreator)
var header = new PlotHeader(plotSeed, leafCount, leafSize, treeHeight, merkleRoot);
header.ComputeChecksum();

// Serialize to bytes
var headerBytes = header.Serialize();

// Deserialize from bytes
var loadedHeader = PlotHeader.Deserialize(headerBytes);

Console.WriteLine($"Plot contains {loadedHeader.LeafCount:N0} leaves");
Console.WriteLine($"Merkle tree height: {loadedHeader.TreeHeight}");
```

## Plot File Format

### Layout
```
[Header: 121 bytes]
[Leaf 0: 32 bytes]
[Leaf 1: 32 bytes]
...
[Leaf N-1: 32 bytes]
```

### Optional Cache File
If caching is enabled, a separate `.cache` file is created containing the top levels of the Merkle tree for faster proof generation.

### File Naming Convention
- Plot file: `{user-defined-name}.dat`
- Cache file: `{user-defined-name}.dat.cache`

## Performance Considerations

### Memory Usage
- Plot generation uses **streaming** to minimize memory footprint
- Memory usage is approximately constant regardless of plot size
- Typical memory: < 10 MB during generation

### Disk I/O
- Uses 80 KB buffers for efficient file writing
- Asynchronous I/O to avoid blocking
- Single-pass generation (leaves written as they're generated)

### Generation Time
Approximate times on modern hardware (4-core CPU, SSD):
- 1 GB plot: ~30 seconds
- 10 GB plot: ~5 minutes
- 100 GB plot: ~50 minutes

*Actual times vary based on CPU hash performance and disk speed.*

### Caching Strategy
- Caching top Merkle tree levels trades disk space for proof speed
- Default 5 levels caches approximately 1/32nd of tree nodes
- Cache size: `(2^cacheLevels - 1) × 32 bytes`
- Example: 5 levels = ~1 KB cache

## Thread Safety

- `LeafGenerator` methods are **thread-safe** (pure functions)
- `PlotCreator` instances are **NOT thread-safe** (use one per plot creation)
- `PlotConfiguration` is **immutable** and safe to share
- `PlotHeader` is **thread-safe** for reading after construction

## Dependencies

- **MerkleTree** (external NuGet) — Streaming Merkle tree construction
- **System.Security.Cryptography** — SHA-256 hashing
- **.NET 10+** — Async streams, spans, modern C# features

## Security Considerations

### Plot Seed Management
- Plot seeds should be **randomly generated** using `RandomNumberGenerator`
- Seeds should be **backed up** securely (required to regenerate plots)
- Different seeds produce different plots from the same public key

### File Integrity
- Always verify header checksums before using a plot
- Store plots on reliable storage with integrity checking (e.g., ZFS, Btrfs)
- Consider keeping backup copies of large plots

### Determinism
- Plots are fully deterministic from (publicKey, seed) pairs
- This allows plot regeneration if files are lost
- Miners should maintain secure records of their seeds

## Testing

Run the test suite:
```bash
dotnet test tests/Spacetime.Plotting.Tests/
```

Test coverage includes:
- Leaf generation determinism and correctness
- Configuration validation
- Header serialization and checksum verification
- Plot creation with various sizes
- Error handling and edge cases

## Future Enhancements

Planned improvements:
- Parallel leaf generation using multiple CPU cores
- GPU-accelerated hashing for faster plot creation
- Compression schemes for reduced disk usage
- Plot metadata indexing for multi-plot management

---

**See also:**
- [Main README](../../README.md) — Project overview
- [Requirements](../../docs/requirements.md) — Technical specifications
- [Copilot Instructions](../../.github/copilot-instructions.md) — Development guidelines
