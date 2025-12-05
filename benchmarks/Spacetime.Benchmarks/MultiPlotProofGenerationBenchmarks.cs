using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MerkleTree.Hashing;
using Spacetime.Plotting;

namespace Spacetime.Benchmarks;

/// <summary>
/// Performance benchmarks for multi-plot proof generation operations.
/// Compares performance with and without Merkle tree caching across multiple plots.
/// </summary>
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
[MarkdownExporter]
public class MultiPlotProofGenerationBenchmarks
{
    private IHashFunction _hashFunction = null!;
    private PlotCreator _plotCreator = null!;
    private ProofGenerator _proofGenerator = null!;
    private byte[] _challenge = null!;

    private List<PlotLoader> _multiplePlotsNoCache = null!;
    private List<PlotLoader> _multiplePlotsWithCache = null!;
    private List<string> _multiplePlotPathsNoCache = null!;
    private List<string> _multiplePlotPathsWithCache = null!;
    private List<string> _multipleCachePaths = null!;
    private List<ProofGenerationOptions> _multiPlotOptionsNoCache = null!;
    private List<ProofGenerationOptions> _multiPlotOptionsWithCache = null!;
    private const int _multiPlotCount = 4;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _hashFunction = new Sha256HashFunction();
        _plotCreator = new PlotCreator(_hashFunction);
        _proofGenerator = new ProofGenerator(_hashFunction);
        
        var minerKey = RandomNumberGenerator.GetBytes(32);
        _challenge = RandomNumberGenerator.GetBytes(32);

        // Create multiple plots for multi-plot benchmarks
        _multiplePlotsNoCache = new List<PlotLoader>();
        _multiplePlotsWithCache = new List<PlotLoader>();
        _multiplePlotPathsNoCache = new List<string>();
        _multiplePlotPathsWithCache = new List<string>();
        _multipleCachePaths = new List<string>();

        for (int i = 0; i < _multiPlotCount; i++)
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

        // Create ProofGenerationOptions lists
        _multiPlotOptionsNoCache = _multiplePlotsNoCache
            .Select(loader => new ProofGenerationOptions(loader, null))
            .ToList();
        
        _multiPlotOptionsWithCache = _multiplePlotsWithCache
            .Zip(_multipleCachePaths, (loader, cachePath) => new ProofGenerationOptions(loader, cachePath))
            .ToList();
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
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

    // ========== Multi-plot benchmarks WITHOUT cache ==========

    [Benchmark(Description = "Multi-plot 4x full scan (no cache)")]
    [BenchmarkCategory("NoCache")]
    public async Task<Proof?> MultiPlot_FullScan_NoCache()
    {
        return await _proofGenerator.GenerateProofFromMultiplePlotsAsync(
            _multiPlotOptionsNoCache,
            _challenge,
            FullScanStrategy.Instance);
    }

    [Benchmark(Description = "Multi-plot 4x sampling 1K (no cache)")]
    [BenchmarkCategory("NoCache")]
    public async Task<Proof?> MultiPlot_Sampling1K_NoCache()
    {
        return await _proofGenerator.GenerateProofFromMultiplePlotsAsync(
            _multiPlotOptionsNoCache,
            _challenge,
            new SamplingScanStrategy(1_000));
    }

    [Benchmark(Description = "Multi-plot 4x sampling 10K (no cache)")]
    [BenchmarkCategory("NoCache")]
    public async Task<Proof?> MultiPlot_Sampling10K_NoCache()
    {
        return await _proofGenerator.GenerateProofFromMultiplePlotsAsync(
            _multiPlotOptionsNoCache,
            _challenge,
            new SamplingScanStrategy(10_000));
    }

    // ========== Multi-plot benchmarks WITH cache ==========

    [Benchmark(Description = "Multi-plot 4x full scan (with cache)")]
    [BenchmarkCategory("WithCache")]
    public async Task<Proof?> MultiPlot_FullScan_WithCache()
    {
        return await _proofGenerator.GenerateProofFromMultiplePlotsAsync(
            _multiPlotOptionsWithCache,
            _challenge,
            FullScanStrategy.Instance);
    }

    [Benchmark(Description = "Multi-plot 4x sampling 1K (with cache)")]
    [BenchmarkCategory("WithCache")]
    public async Task<Proof?> MultiPlot_Sampling1K_WithCache()
    {
        return await _proofGenerator.GenerateProofFromMultiplePlotsAsync(
            _multiPlotOptionsWithCache,
            _challenge,
            new SamplingScanStrategy(1_000));
    }

    [Benchmark(Description = "Multi-plot 4x sampling 10K (with cache)")]
    [BenchmarkCategory("WithCache")]
    public async Task<Proof?> MultiPlot_Sampling10K_WithCache()
    {
        return await _proofGenerator.GenerateProofFromMultiplePlotsAsync(
            _multiPlotOptionsWithCache,
            _challenge,
            new SamplingScanStrategy(10_000));
    }
}
