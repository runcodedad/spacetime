namespace Spacetime.Common;

/// <summary>
/// Provides utility methods for formatting byte sizes to human-readable strings.
/// </summary>
public static class ByteFormatting
{
    /// <summary>
    /// Formats a byte count as a human-readable string (e.g., 1.23 MB).
    /// </summary>
    /// <param name="bytes">The number of bytes.</param>
    /// <returns>A formatted string representing the size.</returns>
    public static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        var order = 0;
        var size = (double)bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:F2} {sizes[order]}";
    }
}
