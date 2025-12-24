using Xunit;

namespace Spacetime.Common.Tests;

public class ByteFormattingTests
{
    [Theory]
    [InlineData(0, "0.00 B")]
    [InlineData(512, "512.00 B")]
    [InlineData(1024, "1.00 KB")]
    [InlineData(1048576, "1.00 MB")]
    [InlineData(1073741824, "1.00 GB")]
    [InlineData(1099511627776, "1.00 TB")]
    [InlineData(1536, "1.50 KB")]
    [InlineData(1572864, "1.50 MB")]
    public void FormatBytes_ValidSizes_ReturnsExpectedString(long bytes, string expected)
    {
        var result = ByteFormatting.FormatBytes(bytes);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1073741824, "1 GB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(2147483648, "2 GB")]
    [InlineData(2097152, "2 MB")]
    [InlineData(3221225472, "3.00 GB")]
    [InlineData(3145728, "3.00 MB")]
    [InlineData(123456789, "117.74 MB")]
    [InlineData(9876543210, "9.20 GB")]
    public void FormatSize_ValidSizes_ReturnsExpectedString(long bytes, string expected)
    {
        var result = ByteFormatting.FormatSize(bytes);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("200MB", 209715200)]
    [InlineData("2GB", 2147483648)]
    [InlineData("1.5GB", 1610612736)]
    [InlineData("1000", 1073741824000)]
    [InlineData("512MB", 536870912)]
    [InlineData("0.5GB", 536870912)]
    [InlineData("1GB", 1073741824)]
    public void TryParseSize_ValidInputs_ReturnsTrueAndExpectedBytes(string input, long expectedBytes)
    {
        var success = input.TryParseSize(out var bytes, out var error);
        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(expectedBytes, bytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-1GB")]
    [InlineData("abc")]
    [InlineData("0MB")]
    [InlineData("0GB")]
    public void TryParseSize_InvalidInputs_ReturnsFalse(string input)
    {
        var success = input.TryParseSize(out var bytes, out var error);
        Assert.False(success);
        Assert.NotNull(error);
    }
}
