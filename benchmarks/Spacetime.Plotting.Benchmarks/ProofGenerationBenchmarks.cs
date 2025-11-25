using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MerkleTree.Hashing;

namespace Spacetime.Plotting.Benchmarks;

/// <summary>
/// Performance benchmarks for proof generation operations.
/// Compares performance with and without Merkle tree caching.
/// </summary>
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
[MarkdownExporter]
public class ProofGenerationBenchmarks
{
    private IHashFunction _hashFunction = null!;
    private PlotCreator _plotCreator = null!;
    private ProofGenerator _proofGenerator = null!;
    private PlotLoader _plotLoaderNoCache = null!;
    private PlotLoader _plotLoaderWithCache = null!;
    private byte[] _challenge = null!;
    private string _plotFilePathNoCache = null!;
    private string _plotFilePathWithCache = null!;
    private string? _cacheFilePath = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _hashFunction = new Sha256HashFunction();
        _plotCreator = new PlotCreator(_hashFunction);
        _proofGenerator = new ProofGenerator(_hashFunction);
        
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        _challenge = RandomNumberGenerator.GetBytes(32);
        _plotFilePathNoCache = Path.Combine(Path.GetTempPath(), $"benchmark_nocache_{Guid.NewGuid()}.plot");
        _plotFilePathWithCache = Path.Combine(Path.GetTempPath(), $"benchmark_cache_{Guid.NewGuid()}.plot");

        // Create plot without cache
        var configNoCache = new PlotConfiguration(
            PlotConfiguration.MinPlotSize, 
            minerKey, 
            plotSeed, 
            _plotFilePathNoCache,
            includeCache: false);
        
        await _plotCreator.CreatePlotAsync(configNoCache);
        _plotLoaderNoCache = await PlotLoader.LoadAsync(_plotFilePathNoCache, _hashFunction);

        // Create plot with cache (5 levels)
        var configWithCache = new PlotConfiguration(
            PlotConfiguration.MinPlotSize, 
            minerKey, 
            plotSeed, 
            _plotFilePathWithCache,
            includeCache: true,
            cacheLevels: 5);
        
        var result = await _plotCreator.CreatePlotAsync(configWithCache);
        _cacheFilePath = result.CacheFilePath;
        _plotLoaderWithCache = await PlotLoader.LoadAsync(_plotFilePathWithCache, _hashFunction);
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        if (_plotLoaderNoCache != null)
        {
            await _plotLoaderNoCache.DisposeAsync();
        }

        if (_plotLoaderWithCache != null)
        {
            await _plotLoaderWithCache.DisposeAsync();
        }

        if (File.Exists(_plotFilePathNoCache))
        {
            File.Delete(_plotFilePathNoCache);
        }

        if (File.Exists(_plotFilePathWithCache))
        {
            File.Delete(_plotFilePathWithCache);
        }

        if (_cacheFilePath != null && File.Exists(_cacheFilePath))
        {
            File.Delete(_cacheFilePath);
        }
    }

    // ========== Benchmarks WITHOUT cache ==========

    [Benchmark(Description = "Full scan (no cache)")]
    public async Task<Proof?> FullScan_NoCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderNoCache,
            _challenge,
            FullScanStrategy.Instance);
    }

    [Benchmark(Description = "Sampling 1K (no cache)")]
    public async Task<Proof?> Sampling1K_NoCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderNoCache,
            _challenge,
            new SamplingScanStrategy(1_000));
    }

    [Benchmark(Description = "Sampling 10K (no cache)")]
    public async Task<Proof?> Sampling10K_NoCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderNoCache,
            _challenge,
            new SamplingScanStrategy(10_000));
    }

    [Benchmark(Description = "Sampling 100K (no cache)")]
    public async Task<Proof?> Sampling100K_NoCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderNoCache,
            _challenge,
            new SamplingScanStrategy(100_000));
    }

    // ========== Benchmarks WITH cache ==========

    [Benchmark(Description = "Full scan (with cache)")]
    public async Task<Proof?> FullScan_WithCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderWithCache,
            _challenge,
            FullScanStrategy.Instance);
    }

    [Benchmark(Description = "Sampling 1K (with cache)")]
    public async Task<Proof?> Sampling1K_WithCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderWithCache,
            _challenge,
            new SamplingScanStrategy(1_000));
    }

    [Benchmark(Description = "Sampling 10K (with cache)")]
    public async Task<Proof?> Sampling10K_WithCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderWithCache,
            _challenge,
            new SamplingScanStrategy(10_000));
    }

    [Benchmark(Description = "Sampling 100K (with cache)")]
    public async Task<Proof?> Sampling100K_WithCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderWithCache,
            _challenge,
            new SamplingScanStrategy(100_000));
    }
}
