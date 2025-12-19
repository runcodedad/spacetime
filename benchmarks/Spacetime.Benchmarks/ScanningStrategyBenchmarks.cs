using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MerkleTree.Hashing;
using Spacetime.Plotting;

namespace Spacetime.Benchmarks;

/// <summary>
/// Performance benchmarks comparing different scanning strategies.
/// Measures throughput and memory efficiency of various scanning approaches.
/// </summary>
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
[MarkdownExporter]
public class ScanningStrategyBenchmarks
{
    private IHashFunction _hashFunction = null!;
    private PlotCreator _plotCreator = null!;
    private ProofGenerator _proofGenerator = null!;
    private PlotLoader _plotLoader = null!;
    private byte[] _challenge = null!;
    private string _plotFilePath = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _hashFunction = new Sha256HashFunction();
        _plotCreator = new PlotCreator(_hashFunction);
        _proofGenerator = new ProofGenerator(_hashFunction);
        
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        _challenge = RandomNumberGenerator.GetBytes(32);
        _plotFilePath = Path.Combine(Path.GetTempPath(), $"benchmark_strategy_{Guid.NewGuid()}.plot");

        // Create plot without cache
        var config = new PlotConfiguration(
            PlotConfiguration.MinPlotSize, 
            minerKey, 
            plotSeed, 
            _plotFilePath,
            includeCache: false);
        
        await _plotCreator.CreatePlotAsync(config);
        _plotLoader = await PlotLoader.LoadAsync(_plotFilePath, _hashFunction);
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        if (_plotLoader != null)
        {
            await _plotLoader.DisposeAsync();
        }

        if (File.Exists(_plotFilePath))
        {
            File.Delete(_plotFilePath);
        }
    }

    // ========== Full Scan Strategy ==========

    [Benchmark(Baseline = true, Description = "Full scan (baseline)")]
    [BenchmarkCategory("Strategy")]
    public async Task<Proof?> FullScan()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            FullScanStrategy.Instance);
    }

    // ========== Sampling Strategy ==========

    [Benchmark(Description = "Sampling 1K")]
    [BenchmarkCategory("Strategy")]
    public async Task<Proof?> Sampling1K()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            new SamplingScanStrategy(1_000));
    }

    [Benchmark(Description = "Sampling 10K")]
    [BenchmarkCategory("Strategy")]
    public async Task<Proof?> Sampling10K()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            new SamplingScanStrategy(10_000));
    }

    [Benchmark(Description = "Sampling 100K")]
    [BenchmarkCategory("Strategy")]
    public async Task<Proof?> Sampling100K()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            new SamplingScanStrategy(100_000));
    }

    // ========== Cache-Friendly Strategy ==========

    [Benchmark(Description = "Cache-friendly L2 (full)")]
    [BenchmarkCategory("Strategy")]
    public async Task<Proof?> CacheFriendlyL2Full()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            CacheFriendlyScanStrategy.CreateForL2Cache());
    }

    [Benchmark(Description = "Cache-friendly L3 (full)")]
    [BenchmarkCategory("Strategy")]
    public async Task<Proof?> CacheFriendlyL3Full()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            CacheFriendlyScanStrategy.CreateForL3Cache());
    }

    [Benchmark(Description = "Cache-friendly sampling 1K")]
    [BenchmarkCategory("Strategy")]
    public async Task<Proof?> CacheFriendlySampling1K()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            CacheFriendlyScanStrategy.CreateSampling(samplesPerBlock: 1_000));
    }

    [Benchmark(Description = "Cache-friendly sampling 10K")]
    [BenchmarkCategory("Strategy")]
    public async Task<Proof?> CacheFriendlySampling10K()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            CacheFriendlyScanStrategy.CreateSampling(samplesPerBlock: 10_000));
    }

    // ========== Early Termination Benchmarks ==========

    [Benchmark(Description = "Full scan with early termination (16 bits)")]
    [BenchmarkCategory("EarlyTermination")]
    public async Task<Proof?> FullScan_EarlyTermination16Bits()
    {
        var config = ScanningConfiguration.CreateFastMode(qualityThresholdBits: 16);
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            FullScanStrategy.Instance,
            config);
    }

    [Benchmark(Description = "Full scan with early termination (24 bits)")]
    [BenchmarkCategory("EarlyTermination")]
    public async Task<Proof?> FullScan_EarlyTermination24Bits()
    {
        var config = ScanningConfiguration.CreateFastMode(qualityThresholdBits: 24);
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            FullScanStrategy.Instance,
            config);
    }

    [Benchmark(Description = "Sampling 10K with time limit (5K max)")]
    [BenchmarkCategory("EarlyTermination")]
    public async Task<Proof?> Sampling10K_TimeLimit5K()
    {
        var config = ScanningConfiguration.CreateTimeLimited(maxLeaves: 5_000);
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            new SamplingScanStrategy(10_000),
            config);
    }

    // ========== Combined Strategy Benchmarks ==========

    [Benchmark(Description = "Cache-friendly L2 with early termination")]
    [BenchmarkCategory("Combined")]
    public async Task<Proof?> CacheFriendlyL2_EarlyTermination()
    {
        var config = ScanningConfiguration.CreateFastMode(qualityThresholdBits: 16);
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            CacheFriendlyScanStrategy.CreateForL2Cache(),
            config);
    }
}
