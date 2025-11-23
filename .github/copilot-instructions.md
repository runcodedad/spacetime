# 🤖 Copilot Instructions — Spacetime Blockchain

Spacetime: Proof-of-Space-Time blockchain in C# / .NET 10. Energy-efficient, disk-based mining. Modular architecture.

## Project Structure
- Source: `src/` - Current: `Spacetime.Plotting`. Planned: `Spacetime.Blockchain`, `Spacetime.Consensus`, `Spacetime.Network`
- Tests: `tests/` - Follow `{ProjectName}.Tests` naming
- Use external MerkleTree NuGet package for tree operations

## Required Coding Patterns

### Naming
- Use PascalCase for classes, methods, properties
- Use _camelCase for private fields
- Use interfaces starting with `I` (e.g., `IHashFunction`)

### Nullable Types
- Always enable nullable reference types: `<Nullable>enable</Nullable>`
- Use `ArgumentNullException.ThrowIfNull(parameter)` for null checks
- Never use `!` null-forgiving operator

### Async/Await
- Always use async for I/O operations (file, network, database)
- Always suffix async methods with `Async`
- Always return `Task` or `Task<T>`
- Always accept `CancellationToken` as last parameter with default value
- Always check `cancellationToken.ThrowIfCancellationRequested()` in loops

### Dependency Injection
- Always use constructor injection
- Always inject interfaces, never concrete classes
- Always validate dependencies in constructor with `ArgumentNullException.ThrowIfNull`

### Immutability
- Always use `readonly` fields
- Always use records for data classes
- Always use `ReadOnlyMemory<byte>` for binary data (keys, seeds)
- Always use `ReadOnlySpan<T>` or `IReadOnlyList<T>` for exposing collections

### Error Handling
- Always use specific exceptions: `ArgumentNullException`, `ArgumentException`, `InvalidOperationException`, `IOException`
- Always validate input early
- Never throw generic `Exception`
- Never catch exceptions you can't handle

## Required Testing Patterns
- Use xUnit, NSubstitute for mocking
- Always follow Arrange-Act-Assert pattern
- Always name tests: `MethodName_Scenario_ExpectedBehavior`
- Always clean up resources (files, streams) with `try/finally` or `using`
- Always target 80%+ code coverage for business logic
- Always mock interfaces only, never concrete classes

## Blockchain-Specific Rules

### Cryptography
- Always use SHA-256 for hashing (via `IHashFunction` abstraction)
- Always use `RandomNumberGenerator.GetBytes()` for random data
- Never use `System.Random` for security operations
- Never implement custom crypto algorithms
- Always use ECDSA with secp256k1 curve
- Always verify signatures before trusting data

### Binary Data
- Always use binary serialization for plots, proofs, blocks
- Always use `BinaryWriter`/`BinaryReader` for simple structures
- Always specify little-endian explicitly
- Always version serialization formats

### Large Files (Plots)
- Always stream large files, never load into memory
- Always use 81920 byte (80 KB) or larger buffers
- Always use `MemoryMappedFile` for random access to plots
- Always report progress for long-running operations

### Thread Safety
- Always minimize shared mutable state
- Always use `Interlocked` for counters
- Always use `lock` for critical sections
- Always use `ConcurrentDictionary` or `ConcurrentQueue` for thread-safe collections

## Security Requirements
- Always validate all external inputs before processing
- Always check size limits, value ranges, format correctness
- Never log private keys, passwords, or sensitive data
- Never expose stack traces to external clients
- Never store private keys in plain text
- Always encrypt private keys with user passphrase

## Forbidden Patterns
- Never use blocking I/O: `File.ReadAllBytes`, `stream.Read` without async
- Never use `.Result` or `.Wait()` on async operations
- Never access file system directly without abstraction (use `IFileSystem`)
- Never hardcode configuration values (use config classes with `init` properties)
- Never use mutable static state
- Never use `async void` (except event handlers)
- Never ignore `CancellationToken` parameters
- Never optimize prematurely (profile first)

## Required Architecture
- Use Factory pattern for complex object creation
- Use Builder pattern for blocks and transactions
- Use Repository pattern with interfaces for storage
- Use Strategy pattern for plot scanning algorithms
- Use Observer pattern for event notifications
- Use Disposable pattern (`IAsyncDisposable`) for file handles and network connections

## XML Documentation
- Always document all public APIs with `<summary>`, `<param>`, `<returns>`, `<exception>`
- Always include usage examples for complex APIs with `<example>` and `<code>`
