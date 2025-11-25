# Spacetime.Plotting.Benchmarks

Performance benchmarks for the Spacetime.Plotting library using BenchmarkDotNet.

## Running Benchmarks

To run all benchmarks:

```bash
cd benchmarks/Spacetime.Plotting.Benchmarks
dotnet run -c Release
```

To run specific benchmarks:

```bash
# Run only cached benchmarks
dotnet run -c Release -- --filter "*WithCache*"

# Run only non-cached benchmarks
dotnet run -c Release -- --filter "*NoCache*"

# Run only full scan benchmarks
dotnet run -c Release -- --filter "*FullScan*"
```

## Benchmarks

### ProofGenerationBenchmarks

Compares proof generation performance with and without Merkle tree caching. Each benchmark is run in two configurations:

#### Without Cache
- **Full Scan (no cache)** - Scans every leaf in a minimum-sized plot (100 MB)
- **Sampling 1K (no cache)** - Samples 1,000 leaves
- **Sampling 10K (no cache)** - Samples 10,000 leaves  
- **Sampling 100K (no cache)** - Samples 100,000 leaves

#### With Cache
- **Full Scan (with cache)** - Same as above, but with 5 levels of Merkle tree cached
- **Sampling 1K (with cache)** - Samples 1,000 leaves with cache
- **Sampling 10K (with cache)** - Samples 10,000 leaves with cache
- **Sampling 100K (with cache)** - Samples 100,000 leaves with cache

These benchmarks help understand:
- Trade-offs between proof quality (full scan) and generation speed (sampling)
- Performance impact of Merkle tree caching during proof generation
- Storage vs. speed trade-offs (cache files add disk usage but may speed up proof generation)

## Results

Results are output to `BenchmarkDotNet.Artifacts/` including:
- Markdown reports
- CSV data
- Memory diagnostics
- Statistical analysis

## Notes

- Benchmarks use minimum-sized plots (100 MB) for consistency
- All benchmarks use SHA-256 hashing
- Memory diagnostics are enabled to track allocations
- Cache configuration uses 5 levels when enabled
- Results may vary based on CPU performance and disk I/O speed
- **Current limitation**: Cache files are created but not yet consumed by `ProofGenerator` (TODO in codebase)
