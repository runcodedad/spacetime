using System.IO.MemoryMappedFiles;
using MerkleTree.Core;
using MerkleTree.Hashing;

namespace Spacetime.Plotting;

/// <summary>
/// Loads and validates plot files, providing efficient random access to leaves.
/// </summary>
/// <remarks>
/// PlotLoader reads plot files created by PlotCreator, validates their integrity,
/// and provides memory-efficient access to individual leaves without loading
/// the entire plot into RAM.
/// </remarks>
/// <example>
/// <code>
/// // Load a plot file
/// using var loader = await PlotLoader.LoadAsync("my-plot.dat", hashFunction);
/// 
/// // Access metadata
/// Console.WriteLine($"Leaf count: {loader.LeafCount}");
/// Console.WriteLine($"Merkle root: {BitConverter.ToString(loader.MerkleRoot.ToArray())}");
/// 
/// // Read specific leaves
/// var leaf = await loader.ReadLeafAsync(42);
/// 
/// // Optionally verify Merkle root (expensive operation)
/// var isValid = await loader.VerifyMerkleRootAsync();
/// </code>
/// </example>
public sealed class PlotLoader : IAsyncDisposable
{
    private readonly FileStream _fileStream;
    private readonly IHashFunction _hashFunction;
    private readonly PlotHeader _header;
    private readonly string _filePath;
    private bool _disposed;

    /// <summary>
    /// Gets the plot header containing metadata.
    /// </summary>
    public PlotHeader Header => _header;

    /// <summary>
    /// Gets the total number of leaves in the plot.
    /// </summary>
    public long LeafCount => _header.LeafCount;

    /// <summary>
    /// Gets the size of each leaf entry in bytes.
    /// </summary>
    public int LeafSize => _header.LeafSize;

    /// <summary>
    /// Gets the Merkle root hash.
    /// </summary>
    public ReadOnlySpan<byte> MerkleRoot => _header.MerkleRoot;

    /// <summary>
    /// Gets the Merkle tree height.
    /// </summary>
    public long TreeHeight => _header.TreeHeight;

    /// <summary>
    /// Gets the plot seed used for deterministic generation.
    /// </summary>
    public ReadOnlySpan<byte> PlotSeed => _header.PlotSeed;

    /// <summary>
    /// Gets the file path of the loaded plot.
    /// </summary>
    public string FilePath => _filePath;

    private PlotLoader(FileStream fileStream, PlotHeader header, IHashFunction hashFunction, string filePath)
    {
        _fileStream = fileStream;
        _header = header;
        _hashFunction = hashFunction;
        _filePath = filePath;
    }

    /// <summary>
    /// Loads a plot file asynchronously and validates its header.
    /// </summary>
    /// <param name="filePath">Path to the plot file</param>
    /// <param name="hashFunction">Hash function for Merkle tree validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A PlotLoader instance for accessing the plot</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath or hashFunction is null</exception>
    /// <exception cref="FileNotFoundException">Thrown when the plot file doesn't exist</exception>
    /// <exception cref="InvalidOperationException">Thrown when the plot file is corrupted or invalid</exception>
    public static async Task<PlotLoader> LoadAsync(
        string filePath,
        IHashFunction hashFunction,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(hashFunction);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Plot file not found: {filePath}", filePath);
        }

        FileStream? fileStream = null;
        try
        {
            // Open file stream with read access and shared reading
            fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);

            // Read header
            var headerBytes = new byte[PlotHeader.TotalHeaderSize];
            var bytesRead = await fileStream.ReadAsync(headerBytes, cancellationToken);

            if (bytesRead < PlotHeader.TotalHeaderSize)
            {
                throw new InvalidOperationException(
                    $"Plot file is too small. Expected at least {PlotHeader.TotalHeaderSize} bytes for header, got {bytesRead} bytes");
            }

            // Deserialize and validate header (this also verifies checksum)
            PlotHeader header;
            try
            {
                header = PlotHeader.Deserialize(headerBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse plot header: {ex.Message}", ex);
            }

            // Verify file size matches expected size
            var expectedFileSize = PlotHeader.TotalHeaderSize + (header.LeafCount * header.LeafSize);
            var actualFileSize = fileStream.Length;

            if (actualFileSize < expectedFileSize)
            {
                throw new InvalidOperationException(
                    $"Plot file is truncated. Expected {expectedFileSize:N0} bytes, got {actualFileSize:N0} bytes");
            }

            return new PlotLoader(fileStream, header, hashFunction, filePath);
        }
        catch
        {
            // Clean up on error
            fileStream?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Reads a specific leaf by its index.
    /// </summary>
    /// <param name="leafIndex">Zero-based index of the leaf to read</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The leaf data as a byte array</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when leafIndex is out of range</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the loader has been disposed</exception>
    public async Task<byte[]> ReadLeafAsync(long leafIndex, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (leafIndex < 0 || leafIndex >= _header.LeafCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leafIndex),
                leafIndex,
                $"Leaf index must be between 0 and {_header.LeafCount - 1}");
        }

        // Calculate file position: header + (leafIndex * leafSize)
        var position = PlotHeader.TotalHeaderSize + (leafIndex * _header.LeafSize);

        var buffer = new byte[_header.LeafSize];
        
        // Seek and read
        _fileStream.Seek(position, SeekOrigin.Begin);
        var bytesRead = await _fileStream.ReadAsync(buffer, cancellationToken);

        if (bytesRead < _header.LeafSize)
        {
            throw new InvalidOperationException(
                $"Failed to read complete leaf at index {leafIndex}. Expected {_header.LeafSize} bytes, got {bytesRead} bytes");
        }

        return buffer;
    }

    /// <summary>
    /// Reads multiple consecutive leaves starting from the specified index.
    /// </summary>
    /// <param name="startIndex">Zero-based index of the first leaf to read</param>
    /// <param name="count">Number of leaves to read</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An array of leaf data</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when indices are out of range</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the loader has been disposed</exception>
    public async Task<byte[][]> ReadLeavesAsync(long startIndex, int count, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (startIndex < 0 || startIndex >= _header.LeafCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startIndex),
                startIndex,
                $"Start index must be between 0 and {_header.LeafCount - 1}");
        }

        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be positive");
        }

        if (startIndex + count > _header.LeafCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                $"Range [{startIndex}, {startIndex + count}) exceeds leaf count {_header.LeafCount}");
        }

        var leaves = new byte[count][];
        var position = PlotHeader.TotalHeaderSize + (startIndex * _header.LeafSize);
        
        _fileStream.Seek(position, SeekOrigin.Begin);

        for (int i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var buffer = new byte[_header.LeafSize];
            var bytesRead = await _fileStream.ReadAsync(buffer, cancellationToken);

            if (bytesRead < _header.LeafSize)
            {
                throw new InvalidOperationException(
                    $"Failed to read complete leaf at index {startIndex + i}. Expected {_header.LeafSize} bytes, got {bytesRead} bytes");
            }

            leaves[i] = buffer;
        }

        return leaves;
    }

    /// <summary>
    /// Verifies that the Merkle root in the header matches the computed root from all leaves.
    /// </summary>
    /// <param name="progress">Optional progress reporter (reports percentage 0-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the Merkle root is valid; otherwise, false</returns>
    /// <remarks>
    /// This is an expensive operation that reads and hashes all leaves in the plot.
    /// Use this method sparingly, typically only when there's suspicion of corruption.
    /// </remarks>
    public async Task<bool> VerifyMerkleRootAsync(
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            // Build Merkle tree from all leaves and compare root
            var merkleTreeStream = new MerkleTreeStream(_hashFunction);
            
            // Stream leaves asynchronously
            var leaves = ReadAllLeavesAsync(progress, cancellationToken);
            var metadata = await merkleTreeStream.BuildAsync(leaves, null, cancellationToken);

            // Compare computed root with header root
            return _header.MerkleRoot.SequenceEqual(metadata.RootHash);
        }
        catch (Exception)
        {
            // Any error during verification means the plot is invalid
            return false;
        }
    }

    /// <summary>
    /// Reads all leaves from the plot as an async enumerable.
    /// </summary>
    private async IAsyncEnumerable<byte[]> ReadAllLeavesAsync(
        IProgress<double>? progress,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Reset to start of leaves
        _fileStream.Seek(PlotHeader.TotalHeaderSize, SeekOrigin.Begin);

        var progressTracker = progress != null ? new ProgressReporter(_header.LeafCount, progress) : null;

        for (long i = 0; i < _header.LeafCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var buffer = new byte[_header.LeafSize];
            var bytesRead = await _fileStream.ReadAsync(buffer, cancellationToken);

            if (bytesRead < _header.LeafSize)
            {
                throw new InvalidOperationException(
                    $"Failed to read complete leaf at index {i}. Expected {_header.LeafSize} bytes, got {bytesRead} bytes");
            }

            progressTracker?.ReportLeafProcessed();
            yield return buffer;
        }
    }

    /// <summary>
    /// Helper class for reporting progress.
    /// </summary>
    private sealed class ProgressReporter(long totalLeaves, IProgress<double> progress)
    {
        private long _processedLeaves;
        private int _lastReportedPercentage = -1;

        public void ReportLeafProcessed()
        {
            var processed = Interlocked.Increment(ref _processedLeaves);
            var percentage = (int)(processed * 100.0 / totalLeaves);

            // Only report when percentage changes to reduce overhead
            if (percentage != _lastReportedPercentage)
            {
                _lastReportedPercentage = percentage;
                progress.Report(percentage);
            }
        }
    }

    /// <summary>
    /// Disposes the plot loader and releases file handles.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _fileStream.DisposeAsync();
    }
}
