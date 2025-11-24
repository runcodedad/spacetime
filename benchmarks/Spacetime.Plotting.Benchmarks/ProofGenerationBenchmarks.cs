using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MerkleTree.Hashing;

namespace Spacetime.Plotting.Benchmarks;

/// <summary>
/// Performance benchmarks for proof generation operations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
[MarkdownExporter]
public class ProofGenerationBenchmarks
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
        _plotFilePath = Path.Combine(Path.GetTempPath(), $"benchmark_{Guid.NewGuid()}.plot");

        // Create a minimum sized plot for benchmarking
        var config = new PlotConfiguration(
            PlotConfiguration.MinPlotSize, 
            minerKey, 
            plotSeed, 
            _plotFilePath);
        
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

    [Benchmark(Description = "Full scan proof generation")]
    public async Task<Proof?> FullScanProofGeneration()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            FullScanStrategy.Instance);
    }

    [Benchmark(Description = "Sampling (1K) proof generation")]
    public async Task<Proof?> SamplingScanProofGeneration_1K()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            new SamplingScanStrategy(1_000));
    }

    [Benchmark(Description = "Sampling (10K) proof generation")]
    public async Task<Proof?> SamplingScanProofGeneration_10K()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            new SamplingScanStrategy(10_000));
    }

    [Benchmark(Description = "Sampling (100K) proof generation")]
    public async Task<Proof?> SamplingScanProofGeneration_100K()
    {
        return await _proofGenerator.GenerateProofAsync(
            _plotLoader,
            _challenge,
            new SamplingScanStrategy(100_000));
    }
}
