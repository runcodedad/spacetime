namespace Spacetime.Plotting.Tests;

public class ScanningConfigurationTests
{
    [Fact]
    public void DefaultConfiguration_HasExpectedValues()
    {
        // Arrange & Act
        var config = ScanningConfiguration.Default;

        // Assert
        Assert.False(config.EnableEarlyTermination);
        Assert.Equal(16, config.QualityThresholdBits);
        Assert.Equal(0, config.MaxLeavesToScan);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesConfiguration()
    {
        // Arrange & Act
        var config = new ScanningConfiguration(
            enableEarlyTermination: true,
            qualityThresholdBits: 24,
            maxLeavesToScan: 10000);

        // Assert
        Assert.True(config.EnableEarlyTermination);
        Assert.Equal(24, config.QualityThresholdBits);
        Assert.Equal(10000, config.MaxLeavesToScan);
    }

    [Fact]
    public void Constructor_WithNegativeQualityThreshold_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ScanningConfiguration(qualityThresholdBits: -1));
    }

    [Fact]
    public void Constructor_WithQualityThresholdOver256_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ScanningConfiguration(qualityThresholdBits: 257));
    }

    [Fact]
    public void Constructor_WithNegativeMaxLeaves_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ScanningConfiguration(maxLeavesToScan: -1));
    }

    [Fact]
    public void CreateFastMode_ReturnsConfigurationWithEarlyTermination()
    {
        // Arrange & Act
        var config = ScanningConfiguration.CreateFastMode(qualityThresholdBits: 20);

        // Assert
        Assert.True(config.EnableEarlyTermination);
        Assert.Equal(20, config.QualityThresholdBits);
        Assert.Equal(0, config.MaxLeavesToScan);
    }

    [Fact]
    public void CreateTimeLimited_ReturnsConfigurationWithMaxLeaves()
    {
        // Arrange & Act
        var config = ScanningConfiguration.CreateTimeLimited(maxLeaves: 50000);

        // Assert
        Assert.False(config.EnableEarlyTermination);
        Assert.Equal(50000, config.MaxLeavesToScan);
    }

    [Fact]
    public void CreateTimeLimited_WithZeroMaxLeaves_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScanningConfiguration.CreateTimeLimited(0));
    }

    [Fact]
    public void MeetsQualityThreshold_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var config = new ScanningConfiguration(enableEarlyTermination: false);
        var score = new byte[32]; // All zeros

        // Act
        var result = config.MeetsQualityThreshold(score);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void MeetsQualityThreshold_WithAllZeros_ReturnsTrue()
    {
        // Arrange
        var config = new ScanningConfiguration(
            enableEarlyTermination: true,
            qualityThresholdBits: 8);
        var score = new byte[32]; // All zeros = 256 leading zero bits

        // Act
        var result = config.MeetsQualityThreshold(score);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MeetsQualityThreshold_WithExactlyEnoughZeroBits_ReturnsTrue()
    {
        // Arrange
        var config = new ScanningConfiguration(
            enableEarlyTermination: true,
            qualityThresholdBits: 12); // Need 12 leading zero bits

        // One zero byte (8 bits) + 4 leading zero bits in next byte = 12 bits
        var score = new byte[32];
        score[0] = 0x00; // 8 zero bits
        score[1] = 0x0F; // 0000 1111 = 4 leading zero bits
        for (var i = 2; i < 32; i++)
        {
            score[i] = 0xFF;
        }

        // Act
        var result = config.MeetsQualityThreshold(score);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MeetsQualityThreshold_WithInsufficientZeroBits_ReturnsFalse()
    {
        // Arrange
        var config = new ScanningConfiguration(
            enableEarlyTermination: true,
            qualityThresholdBits: 16); // Need 16 leading zero bits

        // Only 8 leading zero bits
        var score = new byte[32];
        score[0] = 0x00; // 8 zero bits
        score[1] = 0xFF; // No leading zero bits
        for (var i = 2; i < 32; i++)
        {
            score[i] = 0xFF;
        }

        // Act
        var result = config.MeetsQualityThreshold(score);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void MeetsQualityThreshold_WithMixedBytes_CountsCorrectly()
    {
        // Arrange
        var config = new ScanningConfiguration(
            enableEarlyTermination: true,
            qualityThresholdBits: 17); // Need 17 leading zero bits

        // Two zero bytes (16 bits) + 1 leading zero bit = 17 bits
        var score = new byte[32];
        score[0] = 0x00; // 8 zero bits
        score[1] = 0x00; // 8 zero bits
        score[2] = 0x7F; // 0111 1111 = 1 leading zero bit
        for (var i = 3; i < 32; i++)
        {
            score[i] = 0xFF;
        }

        // Act
        var result = config.MeetsQualityThreshold(score);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MeetsQualityThreshold_WithNoLeadingZeros_ReturnsFalse()
    {
        // Arrange
        var config = new ScanningConfiguration(
            enableEarlyTermination: true,
            qualityThresholdBits: 1);

        // First byte starts with 1
        var score = new byte[32];
        score[0] = 0x80; // 1000 0000 = no leading zero bits
        for (var i = 1; i < 32; i++)
        {
            score[i] = 0x00;
        }

        // Act
        var result = config.MeetsQualityThreshold(score);

        // Assert
        Assert.False(result);
    }
}
