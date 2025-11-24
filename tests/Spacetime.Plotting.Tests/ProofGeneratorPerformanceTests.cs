using System.Diagnostics;
using System.Security.Cryptography;
using MerkleTree.Hashing;

namespace Spacetime.Plotting.Tests;

/// <summary>
/// Performance benchmarks for proof generation.
/// </summary>
public class ProofGeneratorPerformanceTests
{
    private readonly IHashFunction _hashFunction = new Sha256HashFunction();

    [Fact]
    public async Task Benchmark_FullScanPerformance_OnMinimumPlot()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create minimum sized plot
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            var leafCount = loader.LeafCount;

            // Act - measure proof generation time
            var stopwatch = Stopwatch.StartNew();
            var proof = await generator.GenerateProofAsync(
                loader,
                challenge,
                FullScanStrategy.Instance);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(proof);

            // Report performance metrics
            var leavesPerSecond = leafCount / stopwatch.Elapsed.TotalSeconds;
            var timePerLeaf = stopwatch.Elapsed.TotalMilliseconds / leafCount;

            // Log performance (will appear in test output)
            Console.WriteLine($"=== Full Scan Performance Benchmark ===");
            Console.WriteLine($"Plot size: {PlotConfiguration.MinPlotSize:N0} bytes ({PlotConfiguration.MinPlotSize / (1024.0 * 1024):F2} MB)");
            Console.WriteLine($"Leaf count: {leafCount:N0}");
            Console.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds:N0} ms");
            Console.WriteLine($"Throughput: {leavesPerSecond:N0} leaves/sec");
            Console.WriteLine($"Time per leaf: {timePerLeaf:F3} ms");

            // Sanity check - should be able to scan at least 10,000 leaves per second
            const int minExpectedThroughput = 10_000;
            Assert.True(leavesPerSecond > minExpectedThroughput, 
                $"Scanning performance too slow: {leavesPerSecond:N0} leaves/sec (expected > {minExpectedThroughput:N0}/sec)");
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task Benchmark_SamplingPerformance_OnMinimumPlot()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create minimum sized plot
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            const int sampleSize = 1000;
            var strategy = new SamplingScanStrategy(sampleSize);

            // Act - measure proof generation time with sampling
            var stopwatch = Stopwatch.StartNew();
            var proof = await generator.GenerateProofAsync(
                loader,
                challenge,
                strategy);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(proof);

            // Report performance metrics
            var leavesPerSecond = sampleSize / stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine($"=== Sampling Strategy Performance Benchmark ===");
            Console.WriteLine($"Plot size: {PlotConfiguration.MinPlotSize:N0} bytes ({PlotConfiguration.MinPlotSize / (1024.0 * 1024):F2} MB)");
            Console.WriteLine($"Total leaves: {loader.LeafCount:N0}");
            Console.WriteLine($"Sample size: {sampleSize:N0}");
            Console.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds:N0} ms");
            Console.WriteLine($"Throughput: {leavesPerSecond:N0} leaves/sec");

            // Sampling should be faster than full scan for large plots
            // For small plots, the overhead might make it similar or slower
            Assert.True(stopwatch.Elapsed.TotalSeconds < 10, 
                "Sampling should complete within reasonable time");
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task Benchmark_ParallelProofGeneration_OnMultiplePlots()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        
        var plot1Path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");
        var plot2Path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create two small plots
            var plotSeed1 = RandomNumberGenerator.GetBytes(32);
            var plotSeed2 = RandomNumberGenerator.GetBytes(32);
            
            var config1 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed1, plot1Path);
            var config2 = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed2, plot2Path);
            
            await creator.CreatePlotAsync(config1);
            await creator.CreatePlotAsync(config2);

            var loader1 = await PlotLoader.LoadAsync(plot1Path, _hashFunction);
            var loader2 = await PlotLoader.LoadAsync(plot2Path, _hashFunction);

            try
            {
                // Act - measure parallel proof generation
                var stopwatch = Stopwatch.StartNew();
                var proof = await generator.GenerateProofFromMultiplePlotsAsync(
                    new[] { loader1, loader2 },
                    challenge,
                    new SamplingScanStrategy(1000));
                stopwatch.Stop();

                // Assert
                Assert.NotNull(proof);

                Console.WriteLine($"=== Parallel Multi-Plot Performance Benchmark ===");
                Console.WriteLine($"Number of plots: 2");
                Console.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds:N0} ms");
                Console.WriteLine($"Strategy: Sampling (1000 leaves per plot)");

                // Parallel scanning should complete in reasonable time
                Assert.True(stopwatch.Elapsed.TotalSeconds < 30, 
                    "Parallel scanning of multiple plots should complete quickly");
            }
            finally
            {
                await loader1.DisposeAsync();
                await loader2.DisposeAsync();
            }
        }
        finally
        {
            if (File.Exists(plot1Path)) File.Delete(plot1Path);
            if (File.Exists(plot2Path)) File.Delete(plot2Path);
        }
    }

    [Fact]
    public async Task Benchmark_MerkleProofGeneration_Overhead()
    {
        // Arrange
        var creator = new PlotCreator(_hashFunction);
        var generator = new ProofGenerator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.plot");

        try
        {
            // Create minimum sized plot
            var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
            var header = await creator.CreatePlotAsync(config);

            await using var loader = await PlotLoader.LoadAsync(outputPath, _hashFunction);

            // Use sampling to isolate Merkle proof generation cost
            const int scanSampleSize = 100;
            var strategy = new SamplingScanStrategy(scanSampleSize);

            // Act - measure total proof generation time
            var totalStopwatch = Stopwatch.StartNew();
            var proof = await generator.GenerateProofAsync(
                loader,
                challenge,
                strategy);
            totalStopwatch.Stop();

            // Assert
            Assert.NotNull(proof);

            Console.WriteLine($"=== Merkle Proof Generation Overhead ===");
            Console.WriteLine($"Total leaves: {loader.LeafCount:N0}");
            Console.WriteLine($"Tree height: {header.TreeHeight}");
            Console.WriteLine($"Sample scanned: {scanSampleSize} leaves");
            Console.WriteLine($"Total time: {totalStopwatch.ElapsedMilliseconds:N0} ms");
            Console.WriteLine($"Proof sibling count: {proof!.SiblingHashes.Count}");

            // The Merkle proof generation requires reading all leaves, so it will be the dominant cost
            // But it should still complete in reasonable time
            Assert.True(totalStopwatch.Elapsed.TotalSeconds < 60, 
                "Merkle proof generation should complete within a minute for small plots");
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }
}
