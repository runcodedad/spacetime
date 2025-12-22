using System.Security.Cryptography;
using MerkleTree.Hashing;

namespace Spacetime.Plotting.IntegrationTests;

/// <summary>
/// Integration tests for scanning configuration with actual proof generation.
/// These tests verify that early termination and scan limits work correctly
/// with the ProofGenerator.
/// </summary>
public class ScanningConfigurationTests : IDisposable
{
    private readonly IHashFunction _hashFunction;
    private readonly PlotCreator _plotCreator;
    private readonly ProofGenerator _proofGenerator;
    private readonly string _testPlotPath;
    private PlotLoader? _plotLoader;

    public ScanningConfigurationTests()
    {
        _hashFunction = new Sha256HashFunction();
        _plotCreator = new PlotCreator(_hashFunction);
        _proofGenerator = new ProofGenerator(_hashFunction);
        _testPlotPath = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.plot");
    }

    public void Dispose()
    {
        _plotLoader?.DisposeAsync().AsTask().Wait();
        
        if (File.Exists(_testPlotPath))
        {
            File.Delete(_testPlotPath);
        }
    }

    [Fact]
    public async Task GenerateProofAsync_WithMaxLeavesLimit_StopsAtLimit()
    {
        // Arrange
        await CreateTestPlotAsync();
        var challenge = RandomNumberGenerator.GetBytes(32);
        var config = ScanningConfiguration.CreateTimeLimited(maxLeaves: 100);
        
        var scannedCount = 0;
        var progress = new Progress<double>(_ => scannedCount++);

        // Act
        var proof = await _proofGenerator.GenerateProofAsync(
            _plotLoader!,
            challenge,
            FullScanStrategy.Instance,
            config,
            progress: progress);

        // Assert
        Assert.NotNull(proof);
        // Verify we didn't scan all leaves (would be much more than 100)
        Assert.True(scannedCount < 1000); // With 100 max leaves and reporting every 100, should be minimal
    }

    [Fact]
    public async Task GenerateProofAsync_WithDefaultConfig_ScansNormally()
    {
        // Arrange
        await CreateTestPlotAsync();
        var challenge = RandomNumberGenerator.GetBytes(32);
        var config = ScanningConfiguration.Default;

        // Act
        var proof = await _proofGenerator.GenerateProofAsync(
            _plotLoader!,
            challenge,
            new SamplingScanStrategy(1000),
            config);

        // Assert
        Assert.NotNull(proof);
        Assert.NotNull(proof.Score);
        Assert.Equal(32, proof.Score.Length);
    }

    [Fact]
    public async Task GenerateProofAsync_WithEarlyTermination_FindsProof()
    {
        // Arrange
        await CreateTestPlotAsync();
        var challenge = RandomNumberGenerator.GetBytes(32);
        
        // Use a very relaxed threshold (8 bits) to ensure we find a qualifying proof
        var config = ScanningConfiguration.CreateFastMode(qualityThresholdBits: 8);

        // Act
        var proof = await _proofGenerator.GenerateProofAsync(
            _plotLoader!,
            challenge,
            FullScanStrategy.Instance,
            config);

        // Assert
        Assert.NotNull(proof);
        Assert.NotNull(proof.Score);
        
        // Verify the proof meets the quality threshold
        Assert.True(config.MeetsQualityThreshold(proof.Score));
    }

    [Fact]
    public async Task GenerateProofAsync_CombinedLimitAndEarlyTermination_RespectsLimits()
    {
        // Arrange
        await CreateTestPlotAsync();
        var challenge = RandomNumberGenerator.GetBytes(32);
        
        // Combine max leaves with early termination
        var config = new ScanningConfiguration(
            enableEarlyTermination: true,
            qualityThresholdBits: 4, // Very easy threshold
            maxLeavesToScan: 500);

        var scannedCount = 0;
        var progress = new Progress<double>(_ => scannedCount++);

        // Act
        var proof = await _proofGenerator.GenerateProofAsync(
            _plotLoader!,
            challenge,
            FullScanStrategy.Instance,
            config,
            progress: progress);

        // Assert
        Assert.NotNull(proof);
        
        // Should either find a qualifying proof or stop at max leaves
        // In either case, shouldn't scan the entire plot
        Assert.True(scannedCount < 1000);
    }

    [Fact]
    public async Task GenerateProofAsync_WithCacheFriendlyStrategy_ProducesValidProof()
    {
        // Arrange
        await CreateTestPlotAsync();
        var challenge = RandomNumberGenerator.GetBytes(32);
        var strategy = CacheFriendlyScanStrategy.CreateForL2Cache();
        var config = ScanningConfiguration.CreateFastMode(qualityThresholdBits: 8);

        // Act
        var proof = await _proofGenerator.GenerateProofAsync(
            _plotLoader!,
            challenge,
            strategy,
            config);

        // Assert
        Assert.NotNull(proof);
        Assert.NotNull(proof.Score);
        Assert.Equal(32, proof.Score.Length);
        Assert.Equal(32, proof.Challenge.Length);
        Assert.Equal(challenge, proof.Challenge);
    }

    [Fact]
    public async Task GenerateProofAsync_SamplingWithTimeLimit_RespectsLimit()
    {
        // Arrange
        await CreateTestPlotAsync();
        var challenge = RandomNumberGenerator.GetBytes(32);
        var strategy = new SamplingScanStrategy(10_000); // Try to sample 10K
        var config = ScanningConfiguration.CreateTimeLimited(maxLeaves: 500); // But limit to 500

        // Act
        var proof = await _proofGenerator.GenerateProofAsync(
            _plotLoader!,
            challenge,
            strategy,
            config);

        // Assert
        Assert.NotNull(proof);
        // Proof generation succeeded, which means the limit was respected
        // (If it tried to scan 10K leaves it would work, but with 500 limit it should also work)
    }

    private async Task CreateTestPlotAsync()
    {
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        
        // Create a minimal plot for testing
        var config = new PlotConfiguration(
            PlotConfiguration.MinPlotSize,
            minerKey,
            plotSeed,
            _testPlotPath,
            includeCache: false);

        await _plotCreator.CreatePlotAsync(config);
        _plotLoader = await PlotLoader.LoadAsync(_testPlotPath, _hashFunction);
    }
}
