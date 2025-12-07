namespace Spacetime.Consensus;

/// <summary>
/// Provides efficient byte array comparison for use as dictionary keys.
/// </summary>
/// <remarks>
/// This comparer is optimized for high-throughput scenarios where byte arrays
/// are used as dictionary keys, avoiding the overhead of Base64 string conversion.
/// </remarks>
internal sealed class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
{
    /// <summary>
    /// Gets the singleton instance of the comparer.
    /// </summary>
    public static ByteArrayEqualityComparer Instance { get; } = new ByteArrayEqualityComparer();

    private ByteArrayEqualityComparer()
    {
    }

    /// <summary>
    /// Determines whether two byte arrays are equal.
    /// </summary>
    public bool Equals(byte[]? x, byte[]? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.AsSpan().SequenceEqual(y);
    }

    /// <summary>
    /// Gets a hash code for the specified byte array.
    /// </summary>
    public int GetHashCode(byte[] obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        // Use a simple hash combining algorithm
        // For better distribution, we combine multiple bytes
        unchecked
        {
            var hash = 17;
            var len = Math.Min(obj.Length, 8); // Use first 8 bytes for hash
            for (var i = 0; i < len; i++)
            {
                hash = hash * 31 + obj[i];
            }
            return hash;
        }
    }
}
