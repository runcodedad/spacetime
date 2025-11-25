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

    // For multi-plot benchmarks
    private List<PlotLoader> _multiplePlotsNoCache = null!;
    private List<PlotLoader> _multiplePlotsWithCache = null!;
    private List<string> _multiplePlotPathsNoCache = null!;
    private List<string> _multiplePlotPathsWithCache = null!;
    private List<string> _multipleCachePaths = null!;
    private const int MultiPlotCount = 4;

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

        // Create multiple plots for multi-plot benchmarks
        _multiplePlotsNoCache = new List<PlotLoader>();
        _multiplePlotsWithCache = new List<PlotLoader>();
        _multiplePlotPathsNoCache = new List<string>();
        _multiplePlotPathsWithCache = new List<string>();
        _multipleCachePaths = new List<string>();

        for (int i = 0; i < MultiPlotCount; i++)
        {
            var multiPlotSeed = RandomNumberGenerator.GetBytes(32);
            
            // Plot without cache
            var pathNoCache = Path.Combine(Path.GetTempPath(), $"benchmark_multi_nocache_{i}_{Guid.NewGuid()}.plot");
            var configMultiNoCache = new PlotConfiguration(
                PlotConfiguration.MinPlotSize,
                minerKey,
                multiPlotSeed,
                pathNoCache,
                includeCache: false);
            
            await _plotCreator.CreatePlotAsync(configMultiNoCache);
            _multiplePlotsNoCache.Add(await PlotLoader.LoadAsync(pathNoCache, _hashFunction));
            _multiplePlotPathsNoCache.Add(pathNoCache);

            // Plot with cache
            var pathWithCache = Path.Combine(Path.GetTempPath(), $"benchmark_multi_cache_{i}_{Guid.NewGuid()}.plot");
            var configMultiWithCache = new PlotConfiguration(
                PlotConfiguration.MinPlotSize,
                minerKey,
                multiPlotSeed,
                pathWithCache,
                includeCache: true,
                cacheLevels: 5);
            
            var multiResult = await _plotCreator.CreatePlotAsync(configMultiWithCache);
            _multiplePlotsWithCache.Add(await PlotLoader.LoadAsync(pathWithCache, _hashFunction));
            _multiplePlotPathsWithCache.Add(pathWithCache);
            if (multiResult.CacheFilePath != null)
            {
                _multipleCachePaths.Add(multiResult.CacheFilePath);
            }
        }
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

        // Clean up multi-plot resources
        if (_multiplePlotsNoCache != null)
        {
            foreach (var loader in _multiplePlotsNoCache)
            {
                await loader.DisposeAsync();
            }
        }

        if (_multiplePlotsWithCache != null)
        {
            foreach (var loader in _multiplePlotsWithCache)
            {
                await loader.DisposeAsync();
            }
        }

        if (_multiplePlotPathsNoCache != null)
        {
            foreach (var path in _multiplePlotPathsNoCache)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        if (_multiplePlotPathsWithCache != null)
        {
            foreach (var path in _multiplePlotPathsWithCache)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        if (_multipleCachePaths != null)
        {
            foreach (var path in _multipleCachePaths)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }

    // ========== Benchmarks WITHOUT cache ==========

    [Benchmark(Description = "Full scan (no cache)")]
    [BenchmarkCategory("Single", "NoCache")]
    public async Task<Proof?> FullScan_NoCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderNoCache,
            _challenge,
            FullScanStrategy.Instance);
    }

    [Benchmark(Description = "Sampling 1K (no cache)")]
    [BenchmarkCategory("Single", "NoCache")]
    public async Task<Proof?> Sampling1K_NoCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderNoCache,
            _challenge,
            new SamplingScanStrategy(1_000));
    }

    [Benchmark(Description = "Sampling 10K (no cache)")]
    [BenchmarkCategory("Single", "NoCache")]
    public async Task<Proof?> Sampling10K_NoCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderNoCache,
            _challenge,
            new SamplingScanStrategy(10_000));
    }

    [Benchmark(Description = "Sampling 100K (no cache)")]
    [BenchmarkCategory("Single", "NoCache")]
    public async Task<Proof?> Sampling100K_NoCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderNoCache,
            _challenge,
            new SamplingScanStrategy(100_000));
    }

    // ========== Benchmarks WITH cache ==========

    [Benchmark(Description = "Full scan (with cache)")]
    [BenchmarkCategory("Single", "WithCache")]
    public async Task<Proof?> FullScan_WithCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderWithCache,
            _challenge,
            FullScanStrategy.Instance);
    }

    [Benchmark(Description = "Sampling 1K (with cache)")]
    [BenchmarkCategory("Single", "WithCache")]
    public async Task<Proof?> Sampling1K_WithCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderWithCache,
            _challenge,
            new SamplingScanStrategy(1_000));
    }

    [Benchmark(Description = "Sampling 10K (with cache)")]
    [BenchmarkCategory("Single", "WithCache")]
    public async Task<Proof?> Sampling10K_WithCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderWithCache,
            _challenge,
            new SamplingScanStrategy(10_000));
    }

    [Benchmark(Description = "Sampling 100K (with cache)")]
    [BenchmarkCategory("Single", "WithCache")]
    public async Task<Proof?> Sampling100K_WithCache()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoaderWithCache,
            _challenge,
            new SamplingScanStrategy(100_000));
    }

    // ========== Multi-plot benchmarks WITHOUT cache ==========

    [Benchmark(Description = "Multi-plot 4x full scan (no cache)")]
    [BenchmarkCategory("MultiPlot", "NoCache")]
    public async Task<Proof?> MultiPlot_FullScan_NoCache()
    {
        return await _proofGenerator.GenerateProofFromMultiplePlotsAsync(
            _multiplePlotsNoCache,
            _challenge,
            FullScanStrategy.Instance);
    }

    [Benchmark(Description = "Multi-plot 4x sampling 1K (no cache)")]
    [BenchmarkCategory("MultiPlot", "NoCache")]
    public async Task<Proof?> MultiPlot_Sampling1K_NoCache()
    {
        return await _proofGenerator.GenerateProofFromMultiplePlotsAsync(
            _multiplePlotsNoCache,
            _challenge,
            new SamplingScanStrategy(1_000));
    }

    [Benchmark(Description = "Multi-plot 4x sampling 10K (no cache)")]
    [BenchmarkCategory("MultiPlot", "NoCache")]
    public async Task<Proof?> MultiPlot_Sampling10K_NoCache()
    {
        return await _proofGenerator.GenerateProofFromMultiplePlotsAsync(
            _multiplePlotsNoCache,
            _challenge,
            new SamplingScanStrategy(10_000));
    }

    // ========== Multi-plot benchmarks WITH cache ==========

    [Benchmark(Description = "Multi-plot 4x full scan (with cache)")]
    [BenchmarkCategory("MultiPlot", "WithCache")]
    public async Task<Proof?> MultiPlot_FullScan_WithCache()
    {
        return await _proofGenerator.GenerateProofFromMultiplePlotsAsync(
            _multiplePlotsWithCache,
            _challenge,
            FullScanStrategy.Instance);
    }

    [Benchmark(Description = "Multi-plot 4x sampling 1K (with cache)")]
    [BenchmarkCategory("MultiPlot", "WithCache")]
    public async Task<Proof?> MultiPlot_Sampling1K_WithCache()
    {
        return await _proofGenerator.GenerateProofFromMultiplePlotsAsync(
            _multiplePlotsWithCache,
            _challenge,
            new SamplingScanStrategy(1_000));
    }

    [Benchmark(Description = "Multi-plot 4x sampling 10K (with cache)")]
    [BenchmarkCategory("MultiPlot", "WithCache")]
    public async Task<Proof?> MultiPlot_Sampling10K_WithCache()
    {
        return await _proofGenerator.GenerateProofFromMultiplePlotsAsync(
            _multiplePlotsWithCache,
            _challenge,
            new SamplingScanStrategy(10_000));
    }
}
