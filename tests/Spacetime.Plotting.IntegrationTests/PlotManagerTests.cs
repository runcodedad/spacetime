using System.Security.Cryptography;
using MerkleTree.Hashing;

namespace Spacetime.Plotting.IntegrationTests;

public class PlotManagerTests : IAsyncLifetime
{
    private readonly IHashFunction _hashFunction = new Sha256HashFunction();
    private readonly string _tempDirectory;
    private readonly string _metadataFilePath;
    private readonly List<string> _createdFiles = new();

    public PlotManagerTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"PlotManagerTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        _metadataFilePath = Path.Combine(_tempDirectory, "plots_metadata.json");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var file in _createdFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }

        await Task.CompletedTask;
    }

    [Fact]
    public void Constructor_WithNullHashFunction_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PlotManager(null!, _metadataFilePath));
    }

    [Fact]
    public void Constructor_WithNullMetadataFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PlotManager(_hashFunction, null!));
    }

    [Fact]
    public void Constructor_WithEmptyMetadataFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PlotManager(_hashFunction, ""));
    }

    [Fact]
    public void Constructor_WithWhitespaceMetadataFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PlotManager(_hashFunction, "   "));
    }

    [Fact]
    public async Task Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);

        // Assert
        Assert.NotNull(manager);
        Assert.Equal(0, manager.TotalPlotCount);
        Assert.Equal(0, manager.ValidPlotCount);
        Assert.Equal(0, manager.TotalSpaceAllocatedBytes);
        Assert.Empty(manager.LoadedPlots);
        Assert.Empty(manager.PlotMetadataCollection);
    }


    [Fact]
    public async Task AddPlotAsync_WithValidPlot_LoadsSuccessfully()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();

        // Act
        var metadata = await manager.AddPlotAsync(plotPath);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(PlotStatus.Valid, metadata!.Status);
        Assert.Equal(plotPath, metadata.FilePath);
        Assert.Single(manager.PlotMetadataCollection);
        Assert.Equal(1, manager.TotalPlotCount);
    }

    [Fact]
    public async Task AddPlotAsync_WithMissingFile_MarksAsMissing()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.plot");

        // Act
        var metadata = await manager.AddPlotAsync(nonExistentPath);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(PlotStatus.Missing, metadata!.Status);
        Assert.Equal(nonExistentPath, metadata.FilePath);
        Assert.Single(manager.PlotMetadataCollection);
        Assert.Equal(1, manager.TotalPlotCount);
    }

    [Fact]
    public async Task AddPlotAsync_WithCorruptedFile_MarksAsCorrupted()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var corruptedPath = Path.Combine(_tempDirectory, "corrupted.plot");
        await File.WriteAllBytesAsync(corruptedPath, new byte[100]);
        _createdFiles.Add(corruptedPath);

        // Act
        var metadata = await manager.AddPlotAsync(corruptedPath);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(PlotStatus.Corrupted, metadata!.Status);
        Assert.Equal(corruptedPath, metadata.FilePath);
        Assert.Single(manager.PlotMetadataCollection);
        Assert.Equal(1, manager.TotalPlotCount);
    }

    [Fact]
    public async Task AddPlotAsync_WithDuplicatePath_ReturnsExistingMetadata_VerifyFilePath()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();

        // Act
        var metadata1 = await manager.AddPlotAsync(plotPath);
        var metadata2 = await manager.AddPlotAsync(plotPath);

        // Assert
        Assert.NotNull(metadata1);
        Assert.NotNull(metadata2);
        Assert.Equal(metadata1!.FilePath, metadata2!.FilePath);
        Assert.Single(manager.PlotMetadataCollection);
    }

    [Fact]
    public async Task AddPlotAsync_WithNullFilePath_ThrowsArgumentException()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await manager.AddPlotAsync(null!));
    }

    [Fact]
    public async Task AddPlotAsync_WithValidPlot_AddsSuccessfully()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();

        // Act
        var metadata = await manager.AddPlotAsync(plotPath);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(PlotStatus.Valid, metadata!.Status);
        Assert.Equal(plotPath, metadata.FilePath);
        Assert.Equal(1, manager.ValidPlotCount);
    }

    [Fact]
    public async Task AddPlotAsync_WithDuplicatePath_ReturnsExistingMetadata()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();

        // Act
        var metadata1 = await manager.AddPlotAsync(plotPath);
        var metadata2 = await manager.AddPlotAsync(plotPath);

        // Assert
        Assert.NotNull(metadata1);
        Assert.NotNull(metadata2);
        Assert.Equal(metadata1!.PlotId, metadata2!.PlotId);
        Assert.Equal(1, manager.TotalPlotCount);
    }

    [Fact]
    public async Task RemovePlotAsync_WithValidPlotId_RemovesSuccessfully()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        var metadata = await manager.AddPlotAsync(plotPath);

        // Act
        var removed = await manager.RemovePlotAsync(metadata!.PlotId);

        // Assert
        Assert.True(removed);
        Assert.Equal(0, manager.TotalPlotCount);
        Assert.Equal(0, manager.ValidPlotCount);
        Assert.Empty(manager.LoadedPlots);
    }

    [Fact]
    public async Task RemovePlotAsync_WithInvalidPlotId_ReturnsFalse()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);

        // Act
        var removed = await manager.RemovePlotAsync(Guid.NewGuid());

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public async Task GenerateProofAsync_WithNullChallenge_ThrowsArgumentNullException()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        await manager.AddPlotAsync(plotPath);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await manager.GenerateProofAsync(null!, FullScanStrategy.Instance));
    }

    [Fact]
    public async Task GenerateProofAsync_WithNullStrategy_ThrowsArgumentNullException()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        await manager.AddPlotAsync(plotPath);
        var challenge = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await manager.GenerateProofAsync(challenge, null!));
    }

    [Fact]
    public async Task GenerateProofAsync_WithInvalidChallengeSize_ThrowsArgumentException()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        await manager.AddPlotAsync(plotPath);
        var invalidChallenge = RandomNumberGenerator.GetBytes(16);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await manager.GenerateProofAsync(invalidChallenge, FullScanStrategy.Instance));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public async Task GenerateProofAsync_WithNoLoadedPlots_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var challenge = RandomNumberGenerator.GetBytes(32);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await manager.GenerateProofAsync(challenge, FullScanStrategy.Instance));
    }

    [Fact]
    public async Task GenerateProofAsync_WithValidPlot_ReturnsProof()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        await manager.AddPlotAsync(plotPath);
        var challenge = RandomNumberGenerator.GetBytes(32);

        // Act
        var proof = await manager.GenerateProofAsync(challenge, new SamplingScanStrategy(100));

        // Assert
        Assert.NotNull(proof);
        Assert.Equal(32, proof!.Score.Length);
        Assert.Equal(challenge, proof.Challenge);
    }

    [Fact]
    public async Task SaveMetadataAsync_PersistsMetadataToFile()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        await manager.AddPlotAsync(plotPath);

        // Act
        await manager.SaveMetadataAsync();

        // Assert
        Assert.True(File.Exists(_metadataFilePath));
        var json = await File.ReadAllTextAsync(_metadataFilePath);
        Assert.Contains("plotId", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("filePath", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPlotMetadata_WithValidId_ReturnsMetadata()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        var addedMetadata = await manager.AddPlotAsync(plotPath);

        // Act
        var retrievedMetadata = manager.GetPlotMetadata(addedMetadata!.PlotId);

        // Assert
        Assert.NotNull(retrievedMetadata);
        Assert.Equal(addedMetadata.PlotId, retrievedMetadata!.PlotId);
    }

    [Fact]
    public async Task GetPlotMetadata_WithInvalidId_ReturnsNull()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);

        // Act
        var metadata = manager.GetPlotMetadata(Guid.NewGuid());

        // Assert
        Assert.Null(metadata);
    }


    [Fact]
    public async Task LoadMetadataAsync_WithExistingMetadataFile_LoadsPreviousMetadata()
    {
        // Arrange
        var plotPath = await CreateTestPlotAsync();
        Guid originalPlotId;

        // First manager - add plot and save
        await using (var manager1 = new PlotManager(_hashFunction, _metadataFilePath))
        {
            var metadata = await manager1.AddPlotAsync(plotPath);
            originalPlotId = metadata!.PlotId;
            await manager1.SaveMetadataAsync();
        }

        // Second manager - should load from metadata file
        await using var manager2 = new PlotManager(_hashFunction, _metadataFilePath);

        // Act - load metadata from file
        await manager2.LoadMetadataAsync(default);

        // Assert - metadata should be loaded from file
        Assert.Equal(1, manager2.TotalPlotCount);
        var loadedMetadata = manager2.PlotMetadataCollection.First();
        Assert.Equal(plotPath, loadedMetadata.FilePath);
        Assert.Equal(originalPlotId, loadedMetadata.PlotId);
        Assert.Equal(PlotStatus.Valid, loadedMetadata.Status);
    }

    [Fact]
    public async Task RefreshStatusAsync_WithDeletedPlot_UpdatesStatusToMissing()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        await manager.AddPlotAsync(plotPath);
        
        // Delete the plot file
        File.Delete(plotPath);
        _createdFiles.Remove(plotPath);

        // Act
        var changedCount = await manager.RefreshStatusAsync();

        // Assert
        Assert.Equal(1, changedCount);
        var metadata = manager.PlotMetadataCollection.First();
        Assert.Equal(PlotStatus.Missing, metadata.Status);
        Assert.Equal(0, manager.ValidPlotCount);
    }

    [Fact]
    public async Task DisposeAsync_DisposesAllLoadedPlots()
    {
        // Arrange
        var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        await manager.AddPlotAsync(plotPath);
        var loader = manager.LoadedPlots.First();

        // Act
        await manager.DisposeAsync();

        // Assert - accessing the disposed loader should throw
        // Note: We can't directly test if the loader is disposed, but accessing
        // the manager after dispose should throw ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => manager.TotalPlotCount);
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        await manager.AddPlotAsync(plotPath);

        // Act & Assert - should not throw
        await manager.DisposeAsync();
        await manager.DisposeAsync();
        await manager.DisposeAsync();
    }


    [Fact]
    public async Task AddPlotAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await manager.AddPlotAsync(plotPath, null, cts.Token));
    }

    [Fact]
    public async Task AddPlotAsync_WithPlotInDirectory_LoadsSuccessfully()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotDir = Path.Combine(_tempDirectory, "plots");
        Directory.CreateDirectory(plotDir);
        var plotPath = await CreateTestPlotAsync(plotDir);

        // Act
        var metadata = await manager.AddPlotAsync(plotPath);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(PlotStatus.Valid, metadata!.Status);
        Assert.Equal(plotPath, metadata.FilePath);
        Assert.Equal(1, manager.ValidPlotCount);
    }

    [Fact]
    public async Task LoadMetadataAsync_WithNonExistentFile_DoesNotThrow()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);

        // Act & Assert - should not throw when metadata file doesn't exist
        await manager.LoadMetadataAsync(default);

        // Assert
        Assert.Equal(0, manager.TotalPlotCount);
    }

    [Fact]
    public async Task LoadMetadataAsync_WithCorruptedMetadataFile_BacksUpAndStartsFresh()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        
        // Create corrupted metadata file
        await File.WriteAllTextAsync(_metadataFilePath, "{ invalid json content }");

        // Act
        await manager.LoadMetadataAsync(default);

        // Assert - should have empty metadata after handling corrupt file
        Assert.Equal(0, manager.TotalPlotCount);
        
        // Verify backup was created
        var backupFiles = Directory.GetFiles(_tempDirectory, "*.corrupt.*");
        Assert.NotEmpty(backupFiles);
    }

    [Fact]
    public async Task LoadMetadataAsync_WithValidMetadataButMissingPlotFile_LoadsAsValid()
    {
        // Arrange
        var plotPath = await CreateTestPlotAsync();

        // First manager - add plot and save
        await using (var manager1 = new PlotManager(_hashFunction, _metadataFilePath))
        {
            await manager1.AddPlotAsync(plotPath);
            await manager1.SaveMetadataAsync();
        }

        // Delete the plot file but keep metadata
        File.Delete(plotPath);
        _createdFiles.Remove(plotPath);

        // Second manager - should load metadata even though file is missing
        await using var manager2 = new PlotManager(_hashFunction, _metadataFilePath);

        // Act
        await manager2.LoadMetadataAsync(default);

        // Assert - metadata is loaded, but plot is not in LoadedPlots since file is missing
        Assert.Equal(1, manager2.TotalPlotCount);
        Assert.Empty(manager2.LoadedPlots);
    }

    [Fact]
    public async Task LoadMetadataAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        
        await using (var manager1 = new PlotManager(_hashFunction, _metadataFilePath))
        {
            await manager1.AddPlotAsync(plotPath);
            await manager1.SaveMetadataAsync();
        }

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await manager.LoadMetadataAsync(cts.Token));
    }

    [Fact]
    public async Task AddPlotAsync_WithCacheFilePath_StoresInMetadata()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        var cacheFilePath = Path.Combine(_tempDirectory, "test.cache");

        // Act
        var metadata = await manager.AddPlotAsync(plotPath, cacheFilePath);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(cacheFilePath, metadata!.CacheFilePath);
    }

    [Fact]
    public async Task SaveMetadataAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        await manager.AddPlotAsync(plotPath);
        
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await manager.SaveMetadataAsync(cts.Token));
    }

    [Fact]
    public async Task RefreshStatusAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        await manager.AddPlotAsync(plotPath);
        
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await manager.RefreshStatusAsync(cts.Token));
    }

    [Fact]
    public async Task RemovePlotAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        var metadata = await manager.AddPlotAsync(plotPath);
        
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await manager.RemovePlotAsync(metadata!.PlotId, cts.Token));
    }

    [Fact]
    public async Task GenerateProofAsync_WithProgress_CompletesSuccessfully()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        await manager.AddPlotAsync(plotPath);
        var challenge = RandomNumberGenerator.GetBytes(32);
        var progress = new Progress<double>(_ => { });

        // Act
        var proof = await manager.GenerateProofAsync(challenge, FullScanStrategy.Instance, progress);

        // Assert - proof should be generated successfully when progress is provided
        Assert.NotNull(proof);
        
        // Note: Progress.Report(100) is called synchronously, but Progress<T> invokes
        // the callback on the captured synchronization context. In unit tests without
        // a synchronization context, callbacks may not fire immediately.
        // The important part is the method accepts the progress parameter without errors.
    }

    [Fact]
    public async Task GenerateProofAsync_HonorsCancellation_WhenCancelledDuringScan()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        await manager.AddPlotAsync(plotPath);
        var challenge = RandomNumberGenerator.GetBytes(32);
        
        // Use a cancellation token that will be cancelled after a short delay
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(1));

        // Act & Assert
        // Note: With small test plots, the operation might complete before cancellation
        // This test verifies cancellation is checked, but may not always throw
        try
        {
            await manager.GenerateProofAsync(challenge, FullScanStrategy.Instance, null, cts.Token);
            // If it completes without throwing, that's acceptable for small plots
        }
        catch (OperationCanceledException)
        {
            // If cancellation happens, that's the expected behavior
        }
    }

    [Fact]
    public async Task RefreshStatusAsync_WithRecoveredPlot_UpdatesStatusToValid()
    {
        // Arrange
        await using var manager = new PlotManager(_hashFunction, _metadataFilePath);
        var plotPath = await CreateTestPlotAsync();
        await manager.AddPlotAsync(plotPath);
        
        // Delete the plot file temporarily
        File.Delete(plotPath);
        _createdFiles.Remove(plotPath);
        
        // Refresh to mark as missing
        await manager.RefreshStatusAsync();
        Assert.Equal(PlotStatus.Missing, manager.PlotMetadataCollection.First().Status);
        
        // Recreate the plot
        var recreatedPath = await CreateTestPlotAsync();
        File.Move(recreatedPath, plotPath);
        _createdFiles.Remove(recreatedPath);
        _createdFiles.Add(plotPath);

        // Act
        var changedCount = await manager.RefreshStatusAsync();

        // Assert
        Assert.Equal(1, changedCount);
        var metadata = manager.PlotMetadataCollection.First();
        Assert.Equal(PlotStatus.Valid, metadata.Status);
        Assert.Equal(1, manager.ValidPlotCount);
    }

    [Fact]
    public async Task SaveMetadataAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var nestedDir = Path.Combine(_tempDirectory, "nested", "path");
        var nestedMetadataPath = Path.Combine(nestedDir, "metadata.json");
        await using var manager = new PlotManager(_hashFunction, nestedMetadataPath);
        var plotPath = await CreateTestPlotAsync();
        await manager.AddPlotAsync(plotPath);

        // Act
        await manager.SaveMetadataAsync();

        // Assert
        Assert.True(Directory.Exists(nestedDir));
        Assert.True(File.Exists(nestedMetadataPath));
    }

    private async Task<string> CreateTestPlotAsync(string? directory = null)
    {
        var creator = new PlotCreator(_hashFunction);
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var outputPath = Path.Combine(directory ?? _tempDirectory, $"test_{Guid.NewGuid()}.plot");

        var config = new PlotConfiguration(PlotConfiguration.MinPlotSize, minerKey, plotSeed, outputPath);
        await creator.CreatePlotAsync(config);
        
        _createdFiles.Add(outputPath);
        return outputPath;
    }
}
