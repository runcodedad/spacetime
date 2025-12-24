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

    /// <summary>
    /// Formats a byte size as MB or GB for display.
    /// </summary>
    public static string FormatSize(long bytes)
    {
        if (bytes % (1024 * 1024 * 1024) == 0)
        {
            return $"{bytes / (1024 * 1024 * 1024)} GB";
        }

        if (bytes % (1024 * 1024) == 0)
        {
            return $"{bytes / (1024 * 1024)} MB";
        }

        if (bytes > 1024 * 1024 * 1024)
        {
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }

        return $"{bytes / (1024.0 * 1024):F2} MB";
    }

    /// <summary>
    /// Parses a size string (e.g. "200MB", "2GB", "1000") into bytes.
    /// </summary>
    public static bool TryParseSize(this string input, out long bytes, out string? error)
    {
        bytes = 0;
        error = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Size is required.";
            return false;
        }
        var trimmed = input.Trim().ToUpperInvariant();
        var mb = trimmed.EndsWith("MB", StringComparison.InvariantCultureIgnoreCase);
        var gb = trimmed.EndsWith("GB", StringComparison.InvariantCultureIgnoreCase);
        var numberPart = trimmed;
        
        if (mb || gb)
        {
            numberPart = trimmed[..^2];
        }

        if (!double.TryParse(numberPart.Trim(), out var value) || value <= 0)
        {
            error = "Invalid size value.";
            return false;
        }
        
        if (mb)
        {
            bytes = (long)(value * 1024 * 1024);
        }
        else // GB or no suffix since default is GB
        {
            bytes = (long)(value * 1024 * 1024 * 1024); 
        }

        return true;
    }
}
