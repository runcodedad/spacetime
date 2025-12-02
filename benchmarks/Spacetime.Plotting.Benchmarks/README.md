# Spacetime.Plotting.Benchmarks

Performance benchmarks for the Spacetime.Plotting library using BenchmarkDotNet.

## Running Benchmarks

To run all benchmarks:

```bash
cd benchmarks/Spacetime.Plotting.Benchmarks
dotnet run -c Release
```

To run specific benchmark classes:

```bash
# Run only single-plot benchmarks (8 benchmarks)
dotnet run -c Release -- --filter *SinglePlotProofGenerationBenchmarks*

# Run only multi-plot benchmarks (6 benchmarks)
dotnet run -c Release -- --filter *MultiPlotProofGenerationBenchmarks*

# Run specific benchmark categories
dotnet run -c Release -- --anyCategories WithCache    # All cached benchmarks
dotnet run -c Release -- --anyCategories NoCache      # All non-cached benchmarks

# Run specific methods by pattern
dotnet run -c Release -- --filter "*FullScan*"        # Full scan benchmarks only
dotnet run -c Release -- --filter "*Sampling1K*"      # 1K sampling benchmarks only
```

To list available benchmarks without running:

```bash
dotnet run -c Release -- --list flat
dotnet run -c Release -- --list tree
```

## Benchmarks

### SinglePlotProofGenerationBenchmarks

Compares proof generation performance for single plots with and without Merkle tree caching.

#### Benchmarks

**Without Cache** (`NoCache`):
- **Full Scan (no cache)** - Scans every leaf in a minimum-sized plot (100 MB)
- **Sampling 1K (no cache)** - Samples 1,000 leaves
- **Sampling 10K (no cache)** - Samples 10,000 leaves  
- **Sampling 100K (no cache)** - Samples 100,000 leaves

**With Cache** (`WithCache`):
- **Full Scan (with cache)** - Same as above, but with 5 levels of Merkle tree cached
- **Sampling 1K (with cache)** - Samples 1,000 leaves with cache
- **Sampling 10K (with cache)** - Samples 10,000 leaves with cache
- **Sampling 100K (with cache)** - Samples 100,000 leaves with cache

### MultiPlotProofGenerationBenchmarks

Tests parallel proof generation across 4 plots simultaneously using `GenerateProofFromMultiplePlotsAsync`.

#### Benchmarks

**Without Cache** (`NoCache`):
- **Multi-plot 4x full scan (no cache)** - Full scan across 4 plots
- **Multi-plot 4x sampling 1K (no cache)** - 1K sampling across 4 plots
- **Multi-plot 4x sampling 10K (no cache)** - 10K sampling across 4 plots

**With Cache** (`WithCache`):
- **Multi-plot 4x full scan (with cache)** - Full scan across 4 plots with cache
- **Multi-plot 4x sampling 1K (with cache)** - 1K sampling across 4 plots with cache
- **Multi-plot 4x sampling 10K (with cache)** - 10K sampling across 4 plots with cache

## What These Benchmarks Measure

These benchmarks help understand:
- Trade-offs between proof quality (full scan) and generation speed (sampling)
- Performance impact of Merkle tree caching during proof generation
- Storage vs. speed trade-offs (cache files add disk usage but may speed up proof generation)
- Scalability of parallel proof generation across multiple plots (important for miners managing multiple plots)

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
