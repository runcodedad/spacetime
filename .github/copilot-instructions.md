# 🤖 Copilot Context — Spacetime Blockchain

## Project Overview
**Spacetime** is a learning-oriented blockchain project that implements a **Proof of Space-Time (PoST)** consensus mechanism.  
It is designed to be:
- **Energy-efficient** (disk-based mining, minimal CPU/GPU use)
- **Fair and mineable** (no privileged validators)
- **Educational and modular** (readable, incremental design)
- **Built in C# / .NET 10+**

This repository demonstrates how to build a decentralized blockchain network from scratch — including block structures, mining logic, P2P communication, and the novel PoST consensus mechanism.

---

## Core Design Principles
Copilot should align with these when suggesting code:

- **Determinism** — avoid randomness that isn’t reproducible across nodes.  
- **Simplicity first** — prefer clarity over micro-optimizations early on.  
- **Extensibility** — organize code to allow later addition of features like validator finality or proof upgrades.  
- **Energy Efficiency** — focus on lightweight disk I/O, avoid unnecessary hashing or loops.  
- **Fairness & Decentralization** — no central authority; all nodes derive challenges locally.

---

## High-Level Architecture

### Core Components
| Module | Purpose |
|---------|----------|
| `Blockchain` | Core block, transaction, and chainstate logic |
| `Consensus` | Proof of Space-Time, challenge generation, difficulty adjustment |
| `Plots` | Disk-based data creation and proof verification |
| `Mining` | Challenge polling and proof submission |
| `Network` | P2P synchronization, message exchange |
| `Wallet` | Key generation, transaction signing |
| `CLI` | User interface for running nodes, creating plots, sending txs |

---

## Key Concepts Copilot Should Understand

### Proof of Space (PoS)
- Uses **disk space** as the scarce resource.
- Miners pre-generate “plots” — deterministic files filled with pseudorandom entries.
- Each entry corresponds to a Merkle leaf for later proof verification.

### Proof of Space-Time (PoST)
- Extends PoS by requiring **continuous participation over epochs**.
- Every epoch, a **challenge** is derived deterministically from the previous block's hash.
- Miners scan their plots to find the best proof matching the challenge.
- The miner with the lowest score (best proof) wins the right to produce the next block.

### Challenge
- A deterministic seed computed from: `H(previous_block_hash || epoch_number)`
- Used to score each leaf: `score = H(challenge || leaf_data)`
- Lower scores are better.

### Plot File
- Binary file containing:
  - Header with metadata (magic bytes, version, plot seed, Merkle root, leaf count)
  - Deterministically generated leaves (fixed size, typically 32 bytes)
  - Optional Merkle tree cache for faster proof generation
- Files can be gigabytes in size (e.g., 1GB = ~33 million leaves)

### Epoch
- A time period (e.g., 10 seconds) during which miners compete with their proofs.
- At the end of each epoch, the best proof wins and a new block is created.

### Difficulty Adjustment
- Dynamically adjusts the acceptable maximum score based on actual block times.
- If blocks come too fast, difficulty increases (lower max score allowed).
- If blocks come too slow, difficulty decreases (higher max score allowed).

### Merkle Tree Library
- The project uses an external MerkleTree library (already implemented).
- Provides Merkle tree construction, proof generation, and verification.
- Supports streaming construction for large datasets.
- Can cache upper tree layers for performance.

---

## Technology Stack

### Language & Runtime
- **C# 14.0** with latest language features
- **.NET 10** (LTS) - ensures cross-platform support
- **Target Framework**: `net10.0`

### Project Structure
- **Solution**: `Spacetime.sln` - Visual Studio solution file
- **Source Projects**: Located in `src/` directory
  - `Spacetime.Plotting` - Plot file creation and management
  - Future: `Spacetime.Blockchain`, `Spacetime.Consensus`, `Spacetime.Network`, etc.
- **Test Projects**: Located in `tests/` directory
  - `Spacetime.Plotting.Tests` - Unit tests for plotting functionality
  - Tests follow the naming convention: `{ProjectName}.Tests`

### Key Dependencies
- **MerkleTree** (v1.0.0-beta.1) - Merkle tree construction and verification
- **System.Security.Cryptography** - SHA-256 hashing, ECDSA signing
- Future dependencies may include:
  - Serilog - Structured logging
  - MessagePack / Protobuf - Binary serialization
  - Custom P2P networking library

### Testing Framework
- **xUnit** (v2.9.3) - Primary testing framework
- **NSubstitute** (v5.3.0) - Mocking framework for interfaces
- **coverlet.collector** (v6.0.4) - Code coverage collection

### Build & Development Tools
- **EditorConfig** - Code formatting rules (`.editorconfig`)
- **Visual Studio 2022** / **JetBrains Rider** / **VS Code** - Recommended IDEs
- **dotnet CLI** - Command-line build and test tools

---

## Coding Standards and Conventions

### C# Conventions
Follow standard C# coding conventions with these specific guidelines:

#### Naming
- **PascalCase** for classes, methods, properties, constants
- **camelCase** for local variables and private fields
- **_camelCase** (underscore prefix) for private instance fields
- **UPPER_CASE** for constant values (when appropriate)
- Interfaces should start with `I` (e.g., `IHashFunction`, `IPlotScanner`)

#### Code Organization
- One class per file (exceptions: small nested types)
- File name matches the primary type name
- Order members: constants, fields, constructors, properties, methods
- Group related members together with `#region` sparingly (only for very large classes)

### Nullable Reference Types
- **Always enabled**: `<Nullable>enable</Nullable>` in all projects
- Use nullable annotations appropriately:
  - `string?` for potentially null strings
  - `ArgumentNullException.ThrowIfNull(parameter)` for null checks
- Avoid `!` (null-forgiving operator) unless absolutely necessary
- Design APIs to minimize nullability where possible

### XML Documentation
- **Required for all public APIs**: classes, methods, properties, events
- Document:
  - Purpose and behavior (`<summary>`)
  - Parameters (`<param name="paramName">`)
  - Return values (`<returns>`)
  - Exceptions that may be thrown (`<exception cref="ExceptionType">`)
  - Usage examples for complex APIs (`<example>`, `<code>`)
- Use `<remarks>` for additional implementation details
- Keep documentation concise but complete

**Example**:
```csharp
/// <summary>
/// Creates a plot file asynchronously with deterministic leaf values.
/// </summary>
/// <param name="config">The plot configuration containing size and seed parameters</param>
/// <param name="progress">Optional progress reporter (reports percentage 0-100)</param>
/// <param name="cancellationToken">Cancellation token to abort the operation</param>
/// <returns>The created plot header containing metadata and Merkle root</returns>
/// <exception cref="ArgumentNullException">Thrown when config is null</exception>
/// <exception cref="IOException">Thrown when file operations fail</exception>
public async Task<PlotHeader> CreatePlotAsync(
    PlotConfiguration config,
    IProgress<double>? progress = null,
    CancellationToken cancellationToken = default)
```

### Async/Await Patterns
- **Use async/await** for all I/O operations (file, network, database)
- Async methods should:
  - End with `Async` suffix (e.g., `CreatePlotAsync`, `ScanPlotAsync`)
  - Return `Task` or `Task<T>`
  - Accept `CancellationToken` as the last parameter (with default value)
- **Never block on async code**: Don't use `.Result` or `.Wait()`
- Use `ConfigureAwait(false)` in library code (not needed in .NET 6+)
- Prefer `ValueTask<T>` for high-frequency operations that may complete synchronously

### Error Handling
- Use **specific exceptions** rather than generic `Exception`
- Common exceptions:
  - `ArgumentNullException` - null parameter
  - `ArgumentException` / `ArgumentOutOfRangeException` - invalid parameter
  - `InvalidOperationException` - operation not valid in current state
  - `IOException` / `FileNotFoundException` - file operations
  - `OperationCanceledException` - cancellation requested
- Validate input early (fail-fast principle)
- Don't catch exceptions you can't handle
- When catching exceptions, log context before re-throwing
- Avoid using exceptions for control flow

### Logging Standards
*(Note: Serilog will be added in future modules)*
- Use structured logging with Serilog
- Log levels:
  - **Verbose**: Detailed trace information
  - **Debug**: Internal state information for debugging
  - **Information**: General application flow
  - **Warning**: Potentially harmful situations
  - **Error**: Error events that might still allow the app to continue
  - **Fatal**: Very severe errors causing termination
- Include relevant context properties in log statements
- Never log sensitive data (private keys, passwords, personal information)
- Use string interpolation syntax for structured logging: `Log.Information("Created plot {PlotId} with {LeafCount} leaves", plotId, leafCount)`

### SOLID Principles
Follow SOLID design principles:
- **Single Responsibility**: Each class should have one reason to change
- **Open/Closed**: Open for extension, closed for modification
- **Liskov Substitution**: Subtypes must be substitutable for their base types
- **Interface Segregation**: Many specific interfaces better than one general interface
- **Dependency Inversion**: Depend on abstractions, not concretions

---

## Architecture Guidelines

### Modular Design
- Organize code into **focused projects** with clear boundaries
- Each project should have a single, well-defined purpose
- Projects should be **loosely coupled** through interfaces
- Use **dependency injection** for cross-cutting concerns

### Separation of Concerns
Current project structure:
- **Spacetime.Plotting** - Plot file I/O and management (no consensus logic)
- Future projects:
  - **Spacetime.Blockchain** - Block, transaction, and chain state
  - **Spacetime.Consensus** - PoST logic, difficulty adjustment
  - **Spacetime.Cryptography** - Hashing, signing, key management
  - **Spacetime.Network** - P2P communication
  - **Spacetime.Mining** - Proof generation and submission
  - **Spacetime.Wallet** - Key storage, transaction creation
  - **Spacetime.CLI** - Command-line interface

### Dependency Injection
- Design for DI from the start (constructor injection preferred)
- Use **interfaces** for all dependencies
- Avoid service locator pattern
- Keep constructors simple (just assignment)
- Validate dependencies in constructor

**Example**:
```csharp
public sealed class PlotCreator
{
    private readonly IHashFunction _hashFunction;
    
    public PlotCreator(IHashFunction hashFunction)
    {
        _hashFunction = hashFunction ?? throw new ArgumentNullException(nameof(hashFunction));
    }
}
```

### Interface-Based Design
- Define interfaces for:
  - All external dependencies (file system, network, time)
  - Services that may have multiple implementations
  - Components that need mocking in tests
- Keep interfaces focused and cohesive
- Name interfaces based on capability (e.g., `IHashFunction`, `IPlotScanner`)

### Immutability
- Prefer **immutable types** where practical
- Use `readonly` fields whenever possible
- Use `init` properties or readonly properties for data classes
- Use records for simple data containers
- Make collections read-only when exposing them (`ReadOnlySpan<T>`, `IReadOnlyList<T>`)

**Example**:
```csharp
public sealed record PlotConfiguration(
    long PlotSize,
    ReadOnlyMemory<byte> MinerKey,
    ReadOnlyMemory<byte> PlotSeed,
    string OutputPath,
    bool IncludeCache = false,
    int CacheLevels = 0);
```

### Performance Considerations
- Profile before optimizing
- Use `Span<T>` and `Memory<T>` for efficient buffer handling
- Avoid unnecessary allocations in hot paths
- Use `ArrayPool<T>` for temporary buffers
- Stream large files rather than loading into memory
- Consider using `ValueTask<T>` for frequently-called async methods

---

## Testing Requirements

### Unit Test Coverage
- **Target**: 80%+ code coverage for business logic
- Focus on:
  - Public API surface
  - Complex algorithms (leaf generation, proof scoring)
  - Edge cases and error conditions
- Less coverage needed for:
  - Simple property getters/setters
  - Infrastructure code (will be covered by integration tests)

### Test Naming Convention
Use descriptive test names following the pattern:
```
MethodName_Scenario_ExpectedBehavior
```

**Examples**:
- `CreatePlotAsync_CreatesValidPlotFile`
- `GenerateLeaf_DifferentNonces_ProduceDifferentLeaves`
- `Constructor_PlotSizeTooSmall_ThrowsArgumentException`

### Test Structure: Arrange-Act-Assert
Every test should follow the AAA pattern:

```csharp
[Fact]
public async Task CreatePlotAsync_CreatesValidPlotFile()
{
    // Arrange
    var creator = new PlotCreator(new Sha256HashFunction());
    var config = new PlotConfiguration(/* ... */);
    
    // Act
    var header = await creator.CreatePlotAsync(config);
    
    // Assert
    Assert.NotNull(header);
    Assert.True(File.Exists(config.OutputPath));
    Assert.Equal(expectedLeafCount, header.LeafCount);
}
```

### Mock Usage Guidelines
- Use **NSubstitute** for mocking interfaces
- Only mock interfaces, never concrete classes
- Keep mocks simple and focused on the test scenario
- Prefer testing against real implementations when feasible (e.g., file system abstractions)
- Don't over-mock - if a dependency is simple, use the real implementation

**Example**:
```csharp
var mockHashFunction = Substitute.For<IHashFunction>();
mockHashFunction.HashSize.Returns(32);
mockHashFunction.ComputeHash(Arg.Any<ReadOnlySpan<byte>>()).Returns([/* test data */]);
```

### Integration Test Patterns
*(Will be implemented as project grows)*
- Test end-to-end workflows (create plot → scan plot → generate proof)
- Use real file system (with temp directories)
- Test performance with realistic data sizes
- Verify interoperability between modules

### Test Cleanup
- Always clean up resources in tests (files, directories)
- Use `try/finally` or `using` statements for cleanup
- Prefer `IAsyncDisposable` for async cleanup
- Use `[assembly: CollectionBehavior(DisableTestParallelization = true)]` only when necessary

---

## Blockchain-Specific Guidance

### Cryptographic Operations

#### Hashing
- **SHA-256** is the primary hash function
- Use `System.Security.Cryptography.SHA256` or abstracted `IHashFunction`
- Hash sizes are always 32 bytes (256 bits)
- Never implement custom cryptographic algorithms

**Example**:
```csharp
using var sha256 = SHA256.Create();
var hash = sha256.ComputeHash(data);
```

#### Signing and Verification
- Use **ECDSA** (Elliptic Curve Digital Signature Algorithm)
- Curve: **secp256k1** (Bitcoin/Ethereum standard)
- Public keys are 33 bytes (compressed) or 65 bytes (uncompressed)
- Signatures are 64 bytes (r, s values)
- Always verify signatures before trusting data

#### Random Data Generation
- Use `RandomNumberGenerator.GetBytes()` for cryptographic randomness
- Never use `System.Random` for anything security-related
- Seeds must be cryptographically random (32 bytes minimum)

### Binary Data Handling

#### Plots
- Plot files are **binary** (not text)
- Use `FileStream` with async operations
- Buffer size: 81920 bytes (80 KB) or larger for better performance
- Write deterministically (no random access during creation)
- Read with random access (memory-mapped files for large plots)

#### Proofs
- Proofs contain:
  - Leaf data (32 bytes)
  - Merkle path (32 bytes × tree height)
  - Metadata (plot ID, leaf index)
- Serialize compactly for network transmission
- Validate structure before processing

#### Blocks
- Block headers are fixed-size (simplifies validation)
- Transaction data is variable-size
- Use Merkle root to commit to transaction set
- Timestamp must be validated (not too far in future/past)

### Serialization
- **Binary serialization** for efficiency:
  - Use `BinaryWriter`/`BinaryReader` for simple structures
  - Consider MessagePack or Protobuf for complex objects
- **JSON** for configuration and human-readable data
- Always specify **endianness** explicitly (prefer little-endian)
- Version all serialization formats for future compatibility

**Example**:
```csharp
// Writing plot header
writer.Write(MagicBytes);  // 4 bytes
writer.Write(FormatVersion);  // 1 byte
writer.Write(PlotSeed);  // 32 bytes
writer.Write(LeafCount);  // 8 bytes (long)
// ... more fields
```

### Performance Considerations for Large Files

#### Streaming
- **Never load entire plots into memory**
- Use `Stream.ReadAsync()` / `Stream.WriteAsync()`
- Process data in chunks (e.g., 1 MB at a time)
- Report progress for long-running operations

#### Memory-Mapped Files
- Use `MemoryMappedFile` for random access to large plots
- Suitable for proof generation (need to access specific leaves)
- Be aware of address space limitations on 32-bit systems

#### Caching
- Cache upper Merkle tree layers (reduces I/O during proof generation)
- Cache file is separate from plot file (`.plot.cache` extension)
- Cache is optional but significantly improves performance

### Thread Safety

#### Concurrent Plot Access
- Multiple threads may scan the same plot simultaneously
- Use thread-safe file handles or synchronization
- Read-only operations are naturally thread-safe
- Write operations must be exclusive

#### Shared State
- Minimize shared mutable state
- Use `Interlocked` operations for counters
- Use `lock` statements for critical sections
- Prefer immutable data structures
- Consider `ConcurrentDictionary` and `ConcurrentQueue` for thread-safe collections

**Example**:
```csharp
private long _processedLeaves;

public void ReportProgress(long leafCount)
{
    var total = Interlocked.Add(ref _processedLeaves, leafCount);
    var percentage = (double)total / TotalLeaves * 100.0;
    _progress?.Report(percentage);
}
```

### Network Protocol Patterns
*(Future implementation)*
- Use **binary protocols** for efficiency
- Define message types with fixed headers
- Include version number in all messages
- Use length-prefixed encoding for variable-size data
- Implement timeout and retry logic
- Handle partial reads/writes
- Validate all incoming data before processing

---

## Security Considerations

### Input Validation
- **Validate all external inputs** before processing
- Check:
  - Size limits (e.g., plot size, block size)
  - Value ranges (e.g., timestamp, difficulty)
  - Format correctness (e.g., magic bytes, version)
  - Cryptographic validity (e.g., signature, Merkle proof)
- Reject invalid input early with clear error messages
- Never assume data from network/disk is trustworthy

**Example**:
```csharp
public PlotConfiguration(long plotSize, /* ... */)
{
    if (plotSize < MinPlotSize)
        throw new ArgumentException($"Plot size must be at least {MinPlotSize} bytes", nameof(plotSize));
    
    if (plotSize > MaxPlotSize)
        throw new ArgumentException($"Plot size cannot exceed {MaxPlotSize} bytes", nameof(plotSize));
    
    // ... more validation
}
```

### Cryptographic Best Practices
- Use **proven cryptographic libraries** (don't roll your own)
- Use secure defaults (e.g., SHA-256, ECDSA with secp256k1)
- Cryptographic keys must be:
  - Generated with cryptographically secure RNG
  - Stored securely (encrypted at rest)
  - Never logged or transmitted unencrypted
- Verify all signatures before trusting data
- Use constant-time comparison for security-sensitive comparisons

### Secure Key Handling
- Private keys must:
  - Never be logged
  - Never be stored in plain text
  - Be encrypted with a user-provided passphrase
  - Have restricted file permissions (0600 on Unix)
- Public keys can be freely shared
- Derivation paths should follow BIP32/BIP44 standards
- Consider hardware wallet integration for production

### DoS Protection Patterns
- **Rate limiting**: Limit requests per peer/IP
- **Resource limits**: 
  - Maximum block size
  - Maximum transaction size
  - Maximum Merkle proof size
  - Connection limits per peer
- **Validation before expensive operations**: 
  - Check signature before verifying proof
  - Check difficulty target before computing score
- **Timeout mechanisms**: Don't wait indefinitely for responses
- **Backpressure**: Slow down when under load

### Error Messages
- **Don't leak sensitive information** in error messages
- Safe: "Invalid signature"
- Unsafe: "Signature verification failed for key 0x1234..."
- Log detailed errors internally (with appropriate security)
- Return generic errors to external parties
- Never expose stack traces to external clients

---

## Common Patterns to Suggest

### Factory Pattern
Use for complex object creation with validation:

```csharp
public static class PlotConfigurationFactory
{
    public static PlotConfiguration CreateFromGB(double sizeGB, ReadOnlyMemory<byte> minerKey, ReadOnlyMemory<byte> plotSeed, string outputPath)
    {
        // Using binary units: 1 GiB = 1024³ bytes (not 1000³)
        const long gibibyte = 1024L * 1024L * 1024L;
        var sizeBytes = (long)(sizeGB * gibibyte);
        return new PlotConfiguration(sizeBytes, minerKey, plotSeed, outputPath);
    }
}
```

### Builder Pattern
Use for constructing blocks and transactions with many optional parameters:

```csharp
public class BlockBuilder
{
    private readonly List<Transaction> _transactions = new();
    private byte[]? _previousHash;
    private long _timestamp;
    
    public BlockBuilder WithPreviousHash(byte[] hash) { _previousHash = hash; return this; }
    public BlockBuilder WithTransaction(Transaction tx) { _transactions.Add(tx); return this; }
    public BlockBuilder WithTimestamp(long timestamp) { _timestamp = timestamp; return this; }
    
    public Block Build()
    {
        // Validation and construction
        return new Block(/* ... */);
    }
}
```

### Repository Pattern
Use for storage abstraction:

```csharp
public interface IBlockRepository
{
    Task<Block?> GetBlockAsync(byte[] blockHash, CancellationToken cancellationToken = default);
    Task SaveBlockAsync(Block block, CancellationToken cancellationToken = default);
    Task<Block?> GetLatestBlockAsync(CancellationToken cancellationToken = default);
}

public class FileBlockRepository : IBlockRepository
{
    // Implementation using file system
}
```

### Strategy Pattern
Use for different plot scanning algorithms:

```csharp
public interface IPlotScanningStrategy
{
    Task<ProofCandidate> FindBestProofAsync(byte[] challenge, PlotFile plot, CancellationToken cancellationToken);
}

public class FullScanStrategy : IPlotScanningStrategy { /* ... */ }
public class FastScanStrategy : IPlotScanningStrategy { /* ... */ }
```

### Observer Pattern
Use for event notifications (new blocks, transactions):

```csharp
public interface IBlockchainObserver
{
    void OnBlockAdded(Block block);
    void OnBlockRejected(Block block, string reason);
}

public class Blockchain
{
    private readonly List<IBlockchainObserver> _observers = new();
    
    public void RegisterObserver(IBlockchainObserver observer) => _observers.Add(observer);
    
    private void NotifyBlockAdded(Block block)
    {
        foreach (var observer in _observers)
            observer.OnBlockAdded(block);
    }
}
```

### Disposable Pattern
Use for managing unmanaged resources (file handles, network connections):

```csharp
public sealed class PlotFile : IAsyncDisposable
{
    private FileStream? _fileStream;
    
    public async ValueTask DisposeAsync()
    {
        if (_fileStream != null)
        {
            await _fileStream.DisposeAsync();
            _fileStream = null;
        }
    }
}
```

---

## What to Avoid

### Blocking I/O Operations
❌ **Don't**:
```csharp
var data = File.ReadAllBytes(path);  // Blocks thread
stream.Read(buffer, 0, buffer.Length);  // Blocks thread
```

✅ **Do**:
```csharp
// For small files only - use streaming for large files (see plot handling guidance)
var data = await File.ReadAllBytesAsync(path, cancellationToken);
await stream.ReadAsync(buffer, cancellationToken);

// For large files (plots), always use streaming:
await using var stream = File.OpenRead(path);
var buffer = new byte[81920];
int bytesRead;
while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
{
    // Process chunk
}
```

### Direct File System Access
❌ **Don't**:
```csharp
public class PlotScanner
{
    public void ScanPlot()
    {
        using var stream = File.OpenRead("plot.dat");  // Direct dependency
        // ...
    }
}
```

✅ **Do**:
```csharp
public interface IFileSystem
{
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);
}

public class PlotScanner
{
    private readonly IFileSystem _fileSystem;
    
    public PlotScanner(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    public async Task ScanPlotAsync(string path, CancellationToken cancellationToken = default)
    {
        using var stream = await _fileSystem.OpenReadAsync(path, cancellationToken);
        // ...
    }
}
```

### Hardcoded Configuration Values
❌ **Don't**:
```csharp
private const int MaxBlockSize = 1048576;  // Hardcoded
private const string DataDirectory = "/var/spacetime/data";  // Hardcoded
```

✅ **Do**:
```csharp
public class NetworkConfig
{
    public int MaxBlockSize { get; init; } = 1048576;
    public string DataDirectory { get; init; } = "data";
}
```

### Mutable Static State
❌ **Don't**:
```csharp
public static class GlobalState
{
    public static List<Block> Blocks = new();  // Mutable static
    public static Blockchain? CurrentChain;  // Mutable static
}
```

✅ **Do**:
```csharp
public class BlockchainService
{
    private readonly List<Block> _blocks = new();  // Instance state
    
    // Inject dependencies, manage state per instance
}
```

### Throwing Generic Exceptions
❌ **Don't**:
```csharp
if (leafCount == 0)
    throw new Exception("Invalid leaf count");  // Too generic
```

✅ **Do**:
```csharp
if (leafCount == 0)
    throw new ArgumentException("Leaf count must be greater than zero", nameof(leafCount));
```

### Ignoring Cancellation Tokens
❌ **Don't**:
```csharp
public async Task ProcessAsync(CancellationToken cancellationToken)
{
    for (int i = 0; i < 1000000; i++)
    {
        await ProcessItemAsync(i);  // Doesn't check cancellation or pass token
    }
}
```

✅ **Do**:
```csharp
public async Task ProcessAsync(CancellationToken cancellationToken)
{
    for (int i = 0; i < 1000000; i++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ProcessItemAsync(i, cancellationToken);
    }
}
```

### Using `async void`
❌ **Don't**:
```csharp
public async void ProcessData()  // async void (except for event handlers)
{
    await DoWorkAsync();
}
```

✅ **Do**:
```csharp
public async Task ProcessDataAsync()  // async Task
{
    await DoWorkAsync();
}
```

### Over-Engineering Early
❌ **Don't** create elaborate abstraction layers before you need them
❌ **Don't** optimize prematurely - profile first
❌ **Don't** add features "just in case" - follow YAGNI (You Aren't Gonna Need It)

✅ **Do** start simple and refactor as requirements become clear
✅ **Do** optimize bottlenecks identified through profiling
✅ **Do** add complexity only when justified by real requirements

---

## Additional Resources

### Internal Documentation
- [`README.md`](../README.md) - Project overview and getting started
- [`docs/requirements.md`](../docs/requirements.md) - Detailed implementation requirements
- [`docs/implementation-checklist.md`](../docs/implementation-checklist.md) - Development roadmap

### External References
- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [.NET API Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [xUnit Documentation](https://xunit.net/)
- [NSubstitute Documentation](https://nsubstitute.github.io/)

---

## Keeping This File Updated

This file should be updated as the project evolves:
- Add new patterns when they emerge and are validated
- Document new dependencies and their usage
- Update architecture guidelines as modules are added
- Refine coding standards based on code review feedback
- Add examples of preferred patterns specific to this codebase

Contributors: Please update this file when introducing new architectural patterns or coding conventions.
