namespace Spacetime.Core.Tests;

public class EpochConfigTests
{
    [Fact]
    public void Constructor_WithDefaultValue_CreatesConfig()
    {
        // Act
        var config = new EpochConfig();

        // Assert
        Assert.Equal(EpochConfig.DefaultEpochDurationSeconds, config.EpochDurationSeconds);
    }

    [Fact]
    public void Constructor_WithCustomValue_CreatesConfig()
    {
        // Arrange
        const int customDuration = 30;

        // Act
        var config = new EpochConfig(customDuration);

        // Assert
        Assert.Equal(customDuration, config.EpochDurationSeconds);
    }

    [Fact]
    public void Constructor_WithMinValue_CreatesConfig()
    {
        // Arrange
        const int minDuration = EpochConfig.MinEpochDurationSeconds;

        // Act
        var config = new EpochConfig(minDuration);

        // Assert
        Assert.Equal(minDuration, config.EpochDurationSeconds);
    }

    [Fact]
    public void Constructor_WithMaxValue_CreatesConfig()
    {
        // Arrange
        const int maxDuration = EpochConfig.MaxEpochDurationSeconds;

        // Act
        var config = new EpochConfig(maxDuration);

        // Assert
        Assert.Equal(maxDuration, config.EpochDurationSeconds);
    }

    [Fact]
    public void Constructor_WithValueBelowMin_ThrowsArgumentException()
    {
        // Arrange
        var invalidDuration = EpochConfig.MinEpochDurationSeconds - 1;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new EpochConfig(invalidDuration));
        Assert.Contains("must be between", exception.Message);
    }

    [Fact]
    public void Constructor_WithValueAboveMax_ThrowsArgumentException()
    {
        // Arrange
        var invalidDuration = EpochConfig.MaxEpochDurationSeconds + 1;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new EpochConfig(invalidDuration));
        Assert.Contains("must be between", exception.Message);
    }

    [Fact]
    public void Default_ReturnsConfigWithDefaultValues()
    {
        // Act
        var config = EpochConfig.Default();

        // Assert
        Assert.Equal(EpochConfig.DefaultEpochDurationSeconds, config.EpochDurationSeconds);
    }

    [Fact]
    public void Record_WithInit_AllowsPropertyInitialization()
    {
        // Act
        var config = new EpochConfig { EpochDurationSeconds = 20 };

        // Assert
        Assert.Equal(20, config.EpochDurationSeconds);
    }

    [Fact]
    public void Record_Equality_ComparesValues()
    {
        // Arrange
        var config1 = new EpochConfig(15);
        var config2 = new EpochConfig(15);
        var config3 = new EpochConfig(20);

        // Assert
        Assert.Equal(config1, config2);
        Assert.NotEqual(config1, config3);
    }
}
