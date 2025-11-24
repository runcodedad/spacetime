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
dotnet run -c Release -- --filter "*FullScan*"
```

## Benchmarks

### ProofGenerationBenchmarks

Measures the performance of proof generation operations:

- **Full Scan Proof Generation** - Scans every leaf in a minimum-sized plot (100 MB)
- **Sampling (1K) Proof Generation** - Samples 1,000 leaves
- **Sampling (10K) Proof Generation** - Samples 10,000 leaves  
- **Sampling (100K) Proof Generation** - Samples 100,000 leaves

These benchmarks help understand the trade-offs between proof quality (full scan) and generation speed (sampling).

## Results

Results are output to `BenchmarkDotNet.Artifacts/` including:
- Markdown reports
- CSV data
- Memory diagnostics
- Statistical analysis

## Notes

- Benchmarks use a minimum-sized plot (100 MB) for consistency
- All benchmarks use SHA-256 hashing
- Memory diagnostics are enabled to track allocations
- Results may vary based on CPU performance and disk I/O speed
