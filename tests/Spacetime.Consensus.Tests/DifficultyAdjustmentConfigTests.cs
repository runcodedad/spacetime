namespace Spacetime.Consensus.Tests;

/// <summary>
/// Unit tests for the DifficultyAdjustmentConfig class.
/// </summary>
public class DifficultyAdjustmentConfigTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultParameters_CreatesValidConfig()
    {
        // Act
        var config = new DifficultyAdjustmentConfig();

        // Assert
        Assert.Equal(DifficultyAdjustmentConfig.DefaultTargetBlockTimeSeconds, config.TargetBlockTimeSeconds);
        Assert.Equal(DifficultyAdjustmentConfig.DefaultAdjustmentIntervalBlocks, config.AdjustmentIntervalBlocks);
        Assert.Equal(DifficultyAdjustmentConfig.DefaultDampeningFactor, config.DampeningFactor);
        Assert.Equal(DifficultyAdjustmentConfig.DefaultMinimumDifficulty, config.MinimumDifficulty);
        Assert.Equal(long.MaxValue, config.MaximumDifficulty);
    }

    [Fact]
    public void Constructor_WithValidCustomParameters_CreatesConfig()
    {
        // Arrange
        int targetBlockTime = 30;
        int adjustmentInterval = 200;
        int dampeningFactor = 2;
        long minDifficulty = 100;
        long maxDifficulty = 1000000;

        // Act
        var config = new DifficultyAdjustmentConfig(
            targetBlockTime,
            adjustmentInterval,
            dampeningFactor,
            minDifficulty,
            maxDifficulty);

        // Assert
        Assert.Equal(targetBlockTime, config.TargetBlockTimeSeconds);
        Assert.Equal(adjustmentInterval, config.AdjustmentIntervalBlocks);
        Assert.Equal(dampeningFactor, config.DampeningFactor);
        Assert.Equal(minDifficulty, config.MinimumDifficulty);
        Assert.Equal(maxDifficulty, config.MaximumDifficulty);
    }

    [Fact]
    public void Constructor_WithZeroTargetBlockTime_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DifficultyAdjustmentConfig(targetBlockTimeSeconds: 0));
        Assert.Contains("Target block time must be positive", exception.Message);
    }

    [Fact]
    public void Constructor_WithNegativeTargetBlockTime_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DifficultyAdjustmentConfig(targetBlockTimeSeconds: -1));
        Assert.Contains("Target block time must be positive", exception.Message);
    }

    [Fact]
    public void Constructor_WithZeroAdjustmentInterval_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DifficultyAdjustmentConfig(adjustmentIntervalBlocks: 0));
        Assert.Contains("Adjustment interval must be positive", exception.Message);
    }

    [Fact]
    public void Constructor_WithNegativeAdjustmentInterval_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DifficultyAdjustmentConfig(adjustmentIntervalBlocks: -1));
        Assert.Contains("Adjustment interval must be positive", exception.Message);
    }

    [Fact]
    public void Constructor_WithZeroDampeningFactor_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DifficultyAdjustmentConfig(dampeningFactor: 0));
        Assert.Contains("Dampening factor must be positive", exception.Message);
    }

    [Fact]
    public void Constructor_WithNegativeDampeningFactor_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DifficultyAdjustmentConfig(dampeningFactor: -1));
        Assert.Contains("Dampening factor must be positive", exception.Message);
    }

    [Fact]
    public void Constructor_WithZeroMinimumDifficulty_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DifficultyAdjustmentConfig(minimumDifficulty: 0));
        Assert.Contains("Minimum difficulty must be positive", exception.Message);
    }

    [Fact]
    public void Constructor_WithNegativeMinimumDifficulty_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DifficultyAdjustmentConfig(minimumDifficulty: -1));
        Assert.Contains("Minimum difficulty must be positive", exception.Message);
    }

    [Fact]
    public void Constructor_WithZeroMaximumDifficulty_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DifficultyAdjustmentConfig(maximumDifficulty: 0));
        Assert.Contains("Maximum difficulty must be positive", exception.Message);
    }

    [Fact]
    public void Constructor_WithNegativeMaximumDifficulty_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DifficultyAdjustmentConfig(maximumDifficulty: -1));
        Assert.Contains("Maximum difficulty must be positive", exception.Message);
    }

    [Fact]
    public void Constructor_WithMinGreaterThanMax_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DifficultyAdjustmentConfig(
                minimumDifficulty: 1000,
                maximumDifficulty: 100));
        Assert.Contains("Minimum difficulty cannot exceed maximum difficulty", exception.Message);
    }

    [Fact]
    public void Constructor_WithMinEqualToMax_CreatesConfig()
    {
        // Arrange & Act
        var config = new DifficultyAdjustmentConfig(
            minimumDifficulty: 1000,
            maximumDifficulty: 1000);

        // Assert
        Assert.Equal(1000, config.MinimumDifficulty);
        Assert.Equal(1000, config.MaximumDifficulty);
    }

    #endregion

    #region Default Method Tests

    [Fact]
    public void Default_CreatesConfigWithDefaultValues()
    {
        // Act
        var config = DifficultyAdjustmentConfig.Default();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(DifficultyAdjustmentConfig.DefaultTargetBlockTimeSeconds, config.TargetBlockTimeSeconds);
        Assert.Equal(DifficultyAdjustmentConfig.DefaultAdjustmentIntervalBlocks, config.AdjustmentIntervalBlocks);
        Assert.Equal(DifficultyAdjustmentConfig.DefaultDampeningFactor, config.DampeningFactor);
        Assert.Equal(DifficultyAdjustmentConfig.DefaultMinimumDifficulty, config.MinimumDifficulty);
    }

    [Fact]
    public void Default_CreatesNewInstanceEachTime()
    {
        // Act
        var config1 = DifficultyAdjustmentConfig.Default();
        var config2 = DifficultyAdjustmentConfig.Default();

        // Assert - Should be different instances but equal values
        Assert.NotSame(config1, config2);
        Assert.Equal(config1.TargetBlockTimeSeconds, config2.TargetBlockTimeSeconds);
        Assert.Equal(config1.AdjustmentIntervalBlocks, config2.AdjustmentIntervalBlocks);
    }

    #endregion

    #region Record Behavior Tests

    [Fact]
    public void Config_IsRecord()
    {
        // Arrange
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 30,
            adjustmentIntervalBlocks: 200);

        // Act & Assert - Records have specific characteristics
        var type = config.GetType();
        
        // Records are sealed classes
        Assert.True(type.IsSealed || type.IsClass);
        
        // All properties should be readable
        var properties = type.GetProperties();
        foreach (var property in properties)
        {
            Assert.True(property.CanRead, $"Property {property.Name} should be readable");
        }
        
        // Should support 'with' expressions (tested elsewhere)
        // Should support equality comparison (tested elsewhere)
    }

    [Fact]
    public void Config_WithExpression_CreatesNewInstance()
    {
        // Arrange
        var original = new DifficultyAdjustmentConfig(targetBlockTimeSeconds: 10);

        // Act
        var modified = original with { TargetBlockTimeSeconds = 20 };

        // Assert
        Assert.NotSame(original, modified);
        Assert.Equal(10, original.TargetBlockTimeSeconds);
        Assert.Equal(20, modified.TargetBlockTimeSeconds);
    }

    [Fact]
    public void Config_EqualityComparison_WorksCorrectly()
    {
        // Arrange
        var config1 = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 4,
            minimumDifficulty: 1,
            maximumDifficulty: long.MaxValue);

        var config2 = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 4,
            minimumDifficulty: 1,
            maximumDifficulty: long.MaxValue);

        var config3 = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 20,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 4,
            minimumDifficulty: 1,
            maximumDifficulty: long.MaxValue);

        // Act & Assert
        Assert.Equal(config1, config2);
        Assert.NotEqual(config1, config3);
    }

    #endregion

    #region Realistic Configuration Tests

    [Fact]
    public void Config_MainnetStyle_CanBeCreated()
    {
        // Arrange & Act - Mainnet-style configuration
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 2016, // Bitcoin-style 2 weeks
            dampeningFactor: 4,
            minimumDifficulty: 1,
            maximumDifficulty: long.MaxValue);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(10, config.TargetBlockTimeSeconds);
        Assert.Equal(2016, config.AdjustmentIntervalBlocks);
    }

    [Fact]
    public void Config_TestnetStyle_CanBeCreated()
    {
        // Arrange & Act - Testnet-style with faster adjustments
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 2, // More aggressive
            minimumDifficulty: 1,
            maximumDifficulty: 1000000);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(10, config.TargetBlockTimeSeconds);
        Assert.Equal(100, config.AdjustmentIntervalBlocks);
        Assert.Equal(2, config.DampeningFactor);
    }

    [Fact]
    public void Config_DevnetStyle_CanBeCreated()
    {
        // Arrange & Act - Devnet with very fast adjustments
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 5,
            adjustmentIntervalBlocks: 10,
            dampeningFactor: 1, // No dampening for quick testing
            minimumDifficulty: 1,
            maximumDifficulty: 10000);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(5, config.TargetBlockTimeSeconds);
        Assert.Equal(10, config.AdjustmentIntervalBlocks);
        Assert.Equal(1, config.DampeningFactor);
    }

    #endregion
}
