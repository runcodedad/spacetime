namespace Spacetime.Storage;

/// <summary>
/// Interface for atomic write batches.
/// </summary>
/// <remarks>
/// Write batches allow multiple operations to be grouped together
/// and committed atomically, ensuring consistency.
/// </remarks>
public interface IWriteBatch : IDisposable
{
    /// <summary>
    /// Adds a put operation to the batch.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="columnFamily">Optional column family name.</param>
    void Put(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, string? columnFamily = null);

    /// <summary>
    /// Adds a delete operation to the batch.
    /// </summary>
    /// <param name="key">The key to delete.</param>
    /// <param name="columnFamily">Optional column family name.</param>
    void Delete(ReadOnlySpan<byte> key, string? columnFamily = null);

    /// <summary>
    /// Clears all operations from the batch.
    /// </summary>
    void Clear();
}
