using System.Collections.Concurrent;
using System.Text.Json;
using MerkleTree.Hashing;

namespace Spacetime.Plotting;

/// <summary>
/// Manages multiple plot files, including loading, lifecycle management, and proof generation.
/// </summary>
/// <remarks>
/// <para>
/// PlotManager provides a centralized way to manage plot files for mining operations.
/// It handles loading multiple plots, tracking their metadata, and coordinating
/// parallel proof generation across all valid plots.
/// </para>
/// <para>
/// Thread safety: This class is thread-safe for concurrent operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create and configure the plot manager
/// var manager = new PlotManager(hashFunction, "plots_metadata.json");
/// 
/// // Load plots from a directory
/// await manager.LoadPlotsAsync("/path/to/plots");
/// 
/// // Generate proof from all plots
/// var proof = await manager.GenerateProofAsync(challenge, FullScanStrategy.Instance);
/// 
/// // Don't forget to dispose
/// await manager.DisposeAsync();
/// </code>
/// </example>
public sealed class PlotManager : IPlotManager
{
    private const string PlotFilePattern = "*.plot";
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IHashFunction _hashFunction;
    private readonly string _metadataFilePath;
    private readonly ProofGenerator _proofGenerator;
    private readonly ConcurrentDictionary<Guid, PlotLoader> _loadedPlots;
    private readonly ConcurrentDictionary<Guid, PlotMetadata> _plotMetadata;
    private readonly SemaphoreSlim _loadLock;
    private bool _disposed;

    /// <inheritdoc />
    public IReadOnlyList<PlotLoader> LoadedPlots
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _loadedPlots.Values.ToList();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<PlotMetadata> PlotMetadataCollection
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _plotMetadata.Values.ToList();
        }
    }

    /// <inheritdoc />
    public int TotalPlotCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _plotMetadata.Count;
        }
    }

    /// <inheritdoc />
    public int ValidPlotCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _plotMetadata.Values.Count(m => m.Status == PlotStatus.Valid);
        }
    }

    /// <inheritdoc />
    public long TotalSpaceAllocatedBytes
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _plotMetadata.Values.Sum(m => m.SpaceAllocatedBytes);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlotManager"/> class.
    /// </summary>
    /// <param name="hashFunction">Hash function for Merkle tree operations.</param>
    /// <param name="metadataFilePath">Path to the JSON file for persisting plot metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when hashFunction is null.</exception>
    /// <exception cref="ArgumentException">Thrown when metadataFilePath is null or whitespace.</exception>
    public PlotManager(IHashFunction hashFunction, string metadataFilePath)
    {
        ArgumentNullException.ThrowIfNull(hashFunction);
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataFilePath);

        _hashFunction = hashFunction;
        _metadataFilePath = metadataFilePath;
        _proofGenerator = new ProofGenerator(hashFunction);
        _loadedPlots = new ConcurrentDictionary<Guid, PlotLoader>();
        _plotMetadata = new ConcurrentDictionary<Guid, PlotMetadata>();
        _loadLock = new SemaphoreSlim(1, 1);
    }

    /// <inheritdoc />
    public async Task<int> LoadPlotsAsync(
        string? plotDirectory = null,
        IReadOnlyList<string>? additionalPaths = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var hasDirectory = !string.IsNullOrWhiteSpace(plotDirectory);
        var hasAdditionalPaths = additionalPaths != null && additionalPaths.Count > 0;

        if (!hasDirectory && !hasAdditionalPaths)
        {
            throw new ArgumentException("At least one of plotDirectory or additionalPaths must be provided");
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            // Load existing metadata if available
            await LoadMetadataFromFileAsync(cancellationToken);

            // Collect all plot file paths
            var plotPaths = new List<string>();

            if (hasDirectory && Directory.Exists(plotDirectory))
            {
                var directoryFiles = Directory.GetFiles(plotDirectory, PlotFilePattern);
                plotPaths.AddRange(directoryFiles);
            }

            if (hasAdditionalPaths)
            {
                plotPaths.AddRange(additionalPaths!);
            }

            // Remove duplicates
            plotPaths = plotPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (plotPaths.Count == 0)
            {
                progress?.Report(100);
                return 0;
            }

            var loadedCount = 0;
            var processedCount = 0;

            foreach (var path in plotPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var metadata = await LoadSinglePlotAsync(path, cancellationToken);
                if (metadata != null && metadata.Status == PlotStatus.Valid)
                {
                    loadedCount++;
                }

                processedCount++;
                progress?.Report((double)processedCount / plotPaths.Count * 100);
            }

            // Save updated metadata
            await SaveMetadataAsync(cancellationToken);

            return loadedCount;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<PlotMetadata?> AddPlotAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            // Check if already loaded by path
            var existingByPath = _plotMetadata.Values.FirstOrDefault(
                m => string.Equals(m.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (existingByPath != null)
            {
                return existingByPath;
            }

            var metadata = await LoadSinglePlotAsync(filePath, cancellationToken);
            if (metadata != null)
            {
                await SaveMetadataAsync(cancellationToken);
            }
            return metadata;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemovePlotAsync(
        Guid plotId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (!_plotMetadata.TryRemove(plotId, out _))
            {
                return false;
            }

            if (_loadedPlots.TryRemove(plotId, out var loader))
            {
                await loader.DisposeAsync();
            }

            await SaveMetadataAsync(cancellationToken);
            return true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Proof?> GenerateProofAsync(
        byte[] challenge,
        IScanningStrategy strategy,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        ArgumentNullException.ThrowIfNull(strategy);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (challenge.Length != 32)
        {
            throw new ArgumentException("Challenge must be 32 bytes", nameof(challenge));
        }

        var validPlots = _loadedPlots.Values.ToList();
        if (validPlots.Count == 0)
        {
            throw new InvalidOperationException("No valid plots are loaded for proof generation");
        }

        return await _proofGenerator.GenerateProofFromMultiplePlotsAsync(
            validPlots,
            challenge,
            strategy,
            progress,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveMetadataAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var metadataList = _plotMetadata.Values.ToList();
        var serializableList = metadataList.Select(m => new SerializablePlotMetadata
        {
            PlotId = m.PlotId,
            FilePath = m.FilePath,
            SpaceAllocatedBytes = m.SpaceAllocatedBytes,
            MerkleRoot = Convert.ToBase64String(m.MerkleRoot),
            CreatedAtUtc = m.CreatedAtUtc,
            Status = m.Status.ToString()
        }).ToList();

        var json = JsonSerializer.Serialize(serializableList, _jsonOptions);
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(_metadataFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(_metadataFilePath, json, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> RefreshStatusAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            var changedCount = 0;

            foreach (var kvp in _plotMetadata.ToArray())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var metadata = kvp.Value;
                var newStatus = await DetermineStatusAsync(metadata.FilePath, cancellationToken);

                if (newStatus != metadata.Status)
                {
                    var updatedMetadata = metadata.WithStatus(newStatus);
                    _plotMetadata[kvp.Key] = updatedMetadata;

                    // Update loader state
                    if (newStatus == PlotStatus.Valid && !_loadedPlots.ContainsKey(kvp.Key))
                    {
                        // Try to load the plot
                        await TryLoadPlotLoaderAsync(kvp.Key, metadata.FilePath, cancellationToken);
                    }
                    else if (newStatus != PlotStatus.Valid && _loadedPlots.TryRemove(kvp.Key, out var loader))
                    {
                        await loader.DisposeAsync();
                    }

                    changedCount++;
                }
            }

            if (changedCount > 0)
            {
                await SaveMetadataAsync(cancellationToken);
            }

            return changedCount;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc />
    public PlotMetadata? GetPlotMetadata(Guid plotId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _plotMetadata.TryGetValue(plotId, out var metadata) ? metadata : null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Dispose all loaded plot loaders
        foreach (var loader in _loadedPlots.Values)
        {
            await loader.DisposeAsync();
        }

        _loadedPlots.Clear();
        _loadLock.Dispose();
    }

    private async Task LoadMetadataFromFileAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_metadataFilePath))
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_metadataFilePath, cancellationToken);
            var serializableList = JsonSerializer.Deserialize<List<SerializablePlotMetadata>>(json, _jsonOptions);

            if (serializableList == null)
            {
                return;
            }

            foreach (var item in serializableList)
            {
                if (!Enum.TryParse<PlotStatus>(item.Status, out var status))
                {
                    status = PlotStatus.Missing;
                }

                var metadata = new PlotMetadata(
                    PlotId: item.PlotId,
                    FilePath: item.FilePath,
                    SpaceAllocatedBytes: item.SpaceAllocatedBytes,
                    MerkleRoot: Convert.FromBase64String(item.MerkleRoot),
                    CreatedAtUtc: item.CreatedAtUtc,
                    Status: status);

                _plotMetadata.TryAdd(metadata.PlotId, metadata);

                // Try to load valid plots
                if (status == PlotStatus.Valid)
                {
                    await TryLoadPlotLoaderAsync(metadata.PlotId, metadata.FilePath, cancellationToken);
                }
            }
        }
        catch (JsonException)
        {
            // Corrupted metadata file - start fresh
        }
    }

    private async Task<PlotMetadata?> LoadSinglePlotAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var status = await DetermineStatusAsync(filePath, cancellationToken);

        if (status == PlotStatus.Missing)
        {
            // Create metadata for missing file
            var missingMetadata = new PlotMetadata(
                PlotId: Guid.NewGuid(),
                FilePath: filePath,
                SpaceAllocatedBytes: 0,
                MerkleRoot: Array.Empty<byte>(),
                CreatedAtUtc: DateTime.UtcNow,
                Status: PlotStatus.Missing);

            _plotMetadata.TryAdd(missingMetadata.PlotId, missingMetadata);
            return missingMetadata;
        }

        try
        {
            var loader = await PlotLoader.LoadAsync(filePath, _hashFunction, cancellationToken);
            var plotId = Guid.NewGuid();

            var metadata = PlotMetadata.FromPlotLoader(loader, plotId);
            _plotMetadata.TryAdd(plotId, metadata);
            _loadedPlots.TryAdd(plotId, loader);

            return metadata;
        }
        catch (Exception)
        {
            // Failed to load - mark as corrupted
            var fileInfo = new FileInfo(filePath);
            var corruptedMetadata = new PlotMetadata(
                PlotId: Guid.NewGuid(),
                FilePath: filePath,
                SpaceAllocatedBytes: fileInfo.Exists ? fileInfo.Length : 0,
                MerkleRoot: Array.Empty<byte>(),
                CreatedAtUtc: DateTime.UtcNow,
                Status: PlotStatus.Corrupted);

            _plotMetadata.TryAdd(corruptedMetadata.PlotId, corruptedMetadata);
            return corruptedMetadata;
        }
    }

    private async Task<PlotStatus> DetermineStatusAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return PlotStatus.Missing;
        }

        try
        {
            // Try to load and validate the plot
            await using var loader = await PlotLoader.LoadAsync(filePath, _hashFunction, cancellationToken);
            return PlotStatus.Valid;
        }
        catch (Exception)
        {
            return PlotStatus.Corrupted;
        }
    }

    private async Task TryLoadPlotLoaderAsync(
        Guid plotId,
        string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            var loader = await PlotLoader.LoadAsync(filePath, _hashFunction, cancellationToken);
            _loadedPlots.TryAdd(plotId, loader);
        }
        catch (Exception)
        {
            // Update status to corrupted if load fails
            if (_plotMetadata.TryGetValue(plotId, out var metadata))
            {
                _plotMetadata[plotId] = metadata.WithStatus(PlotStatus.Corrupted);
            }
        }
    }

    /// <summary>
    /// Serializable version of PlotMetadata for JSON persistence.
    /// </summary>
    private sealed class SerializablePlotMetadata
    {
        public Guid PlotId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public long SpaceAllocatedBytes { get; set; }
        public string MerkleRoot { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
