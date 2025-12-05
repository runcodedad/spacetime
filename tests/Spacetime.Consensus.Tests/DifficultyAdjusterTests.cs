using System.Numerics;

namespace Spacetime.Consensus.Tests;

/// <summary>
/// Unit tests for the DifficultyAdjuster class.
/// </summary>
public class DifficultyAdjusterTests
{
    private readonly DifficultyAdjustmentConfig _defaultConfig;
    private readonly DifficultyAdjuster _adjuster;

    public DifficultyAdjusterTests()
    {
        _defaultConfig = DifficultyAdjustmentConfig.Default();
        _adjuster = new DifficultyAdjuster(_defaultConfig);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidConfig_CreatesInstance()
    {
        // Arrange & Act
        var adjuster = new DifficultyAdjuster(_defaultConfig);

        // Assert
        Assert.NotNull(adjuster);
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DifficultyAdjuster(null!));
    }

    #endregion

    #region DifficultyToTarget Tests

    [Fact]
    public void DifficultyToTarget_WithDifficultyOne_ReturnsMaxTarget()
    {
        // Arrange
        long difficulty = 1;

        // Act
        var target = DifficultyAdjuster.DifficultyToTarget(difficulty);

        // Assert
        Assert.NotNull(target);
        Assert.Equal(32, target.Length);
        
        // Should be all 0xFF (maximum value)
        Assert.True(target.All(b => b == 0xFF));
    }

    [Fact]
    public void DifficultyToTarget_WithHighDifficulty_ReturnsLowTarget()
    {
        // Arrange
        long difficulty = 1000000;

        // Act
        var target = DifficultyAdjuster.DifficultyToTarget(difficulty);

        // Assert
        Assert.NotNull(target);
        Assert.Equal(32, target.Length);
        
        // Convert back to verify relationship
        var targetValue = new BigInteger(target, isUnsigned: true, isBigEndian: true);
        Assert.True(targetValue > 0);
        
        // Higher difficulty should result in lower target than difficulty=1
        var maxTarget = DifficultyAdjuster.DifficultyToTarget(1);
        var maxTargetValue = new BigInteger(maxTarget, isUnsigned: true, isBigEndian: true);
        Assert.True(targetValue < maxTargetValue);
    }

    [Fact]
    public void DifficultyToTarget_WithZeroDifficulty_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => DifficultyAdjuster.DifficultyToTarget(0));
    }

    [Fact]
    public void DifficultyToTarget_WithNegativeDifficulty_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => DifficultyAdjuster.DifficultyToTarget(-1));
    }

    [Fact]
    public void DifficultyToTarget_IsDeterministic()
    {
        // Arrange
        long difficulty = 12345;

        // Act
        var target1 = DifficultyAdjuster.DifficultyToTarget(difficulty);
        var target2 = DifficultyAdjuster.DifficultyToTarget(difficulty);

        // Assert
        Assert.Equal(target1, target2);
    }

    [Fact]
    public void DifficultyToTarget_HigherDifficulty_ProducesLowerTarget()
    {
        // Arrange
        long lowDifficulty = 1000;
        long highDifficulty = 10000;

        // Act
        var lowTarget = DifficultyAdjuster.DifficultyToTarget(lowDifficulty);
        var highTarget = DifficultyAdjuster.DifficultyToTarget(highDifficulty);

        // Assert - Compare as big-endian byte arrays
        var lowTargetValue = new BigInteger(lowTarget, isUnsigned: true, isBigEndian: true);
        var highTargetValue = new BigInteger(highTarget, isUnsigned: true, isBigEndian: true);
        
        Assert.True(highTargetValue < lowTargetValue, 
            "Higher difficulty should produce lower target value");
    }

    [Fact]
    public void DifficultyToTarget_Always32Bytes()
    {
        // Arrange
        var difficulties = new[] { 1L, 10L, 100L, 1000L, 10000L, 100000L, 1000000L };

        // Act & Assert
        foreach (var difficulty in difficulties)
        {
            var target = DifficultyAdjuster.DifficultyToTarget(difficulty);
            Assert.Equal(32, target.Length);
        }
    }

    [Fact]
    public void DifficultyToTarget_WithMaxLongValue_ReturnsSmallTarget()
    {
        // Arrange
        long difficulty = long.MaxValue;

        // Act
        var target = DifficultyAdjuster.DifficultyToTarget(difficulty);

        // Assert
        Assert.NotNull(target);
        Assert.Equal(32, target.Length);
        
        // Should be a very small value
        var targetValue = new BigInteger(target, isUnsigned: true, isBigEndian: true);
        Assert.True(targetValue > 0);
        
        // Should be much smaller than max target
        var maxTarget = DifficultyAdjuster.DifficultyToTarget(1);
        var maxTargetValue = new BigInteger(maxTarget, isUnsigned: true, isBigEndian: true);
        Assert.True(targetValue < maxTargetValue / 1000000); // Should be at least a million times smaller
    }

    #endregion

    #region TargetToDifficulty Tests

    [Fact]
    public void TargetToDifficulty_WithMaxTarget_ReturnsDifficultyOne()
    {
        // Arrange - All 0xFF bytes (max target)
        var maxTarget = Enumerable.Repeat((byte)0xFF, 32).ToArray();

        // Act
        var difficulty = DifficultyAdjuster.TargetToDifficulty(maxTarget);

        // Assert
        Assert.Equal(1, difficulty);
    }

    [Fact]
    public void TargetToDifficulty_WithNullTarget_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => DifficultyAdjuster.TargetToDifficulty(null!));
    }

    [Fact]
    public void TargetToDifficulty_WithInvalidSize_ThrowsArgumentException()
    {
        // Arrange
        var invalidTarget = new byte[16];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DifficultyAdjuster.TargetToDifficulty(invalidTarget));
    }

    [Fact]
    public void TargetToDifficulty_WithZeroTarget_ReturnsMaxDifficulty()
    {
        // Arrange
        var zeroTarget = new byte[32];

        // Act
        var difficulty = DifficultyAdjuster.TargetToDifficulty(zeroTarget);

        // Assert
        Assert.Equal(long.MaxValue, difficulty);
    }

    [Fact]
    public void TargetToDifficulty_RoundTrip_PreservesDifficulty()
    {
        // Arrange
        var difficulties = new[] { 1L, 10L, 100L, 1000L, 10000L, 100000L };

        // Act & Assert
        foreach (var originalDifficulty in difficulties)
        {
            var target = DifficultyAdjuster.DifficultyToTarget(originalDifficulty);
            var roundTripDifficulty = DifficultyAdjuster.TargetToDifficulty(target);
            
            // Allow small rounding error due to integer division
            Assert.True(Math.Abs(roundTripDifficulty - originalDifficulty) <= 1,
                $"Round-trip failed for difficulty {originalDifficulty}: got {roundTripDifficulty}");
        }
    }

    #endregion

    #region ShouldAdjustDifficulty Tests

    [Fact]
    public void ShouldAdjustDifficulty_AtGenesisHeight_ReturnsFalse()
    {
        // Act
        var shouldAdjust = _adjuster.ShouldAdjustDifficulty(0);

        // Assert
        Assert.False(shouldAdjust);
    }

    [Fact]
    public void ShouldAdjustDifficulty_AtAdjustmentInterval_ReturnsTrue()
    {
        // Arrange
        var adjustmentHeight = _defaultConfig.AdjustmentIntervalBlocks;

        // Act
        var shouldAdjust = _adjuster.ShouldAdjustDifficulty(adjustmentHeight);

        // Assert
        Assert.True(shouldAdjust);
    }

    [Fact]
    public void ShouldAdjustDifficulty_BetweenIntervals_ReturnsFalse()
    {
        // Arrange
        var height = _defaultConfig.AdjustmentIntervalBlocks / 2;

        // Act
        var shouldAdjust = _adjuster.ShouldAdjustDifficulty(height);

        // Assert
        Assert.False(shouldAdjust);
    }

    [Fact]
    public void ShouldAdjustDifficulty_AtMultipleIntervals_ReturnsTrue()
    {
        // Arrange
        var height = _defaultConfig.AdjustmentIntervalBlocks * 3;

        // Act
        var shouldAdjust = _adjuster.ShouldAdjustDifficulty(height);

        // Assert
        Assert.True(shouldAdjust);
    }

    [Fact]
    public void ShouldAdjustDifficulty_WithNegativeHeight_ReturnsFalse()
    {
        // Act
        var shouldAdjust = _adjuster.ShouldAdjustDifficulty(-1);

        // Assert
        Assert.False(shouldAdjust);
    }

    #endregion

    #region CalculateNextDifficulty Tests

    [Fact]
    public void CalculateNextDifficulty_WithFasterBlocks_IncreasesDifficulty()
    {
        // Arrange - Blocks are coming 2x faster than target
        long currentDifficulty = 1000;
        long currentHeight = 100;
        long targetTime = _defaultConfig.AdjustmentIntervalBlocks * _defaultConfig.TargetBlockTimeSeconds;
        long actualTime = targetTime / 2; // Half the expected time
        long currentTimestamp = actualTime;
        long intervalStartTimestamp = 0;

        // Act
        var newDifficulty = _adjuster.CalculateNextDifficulty(
            currentDifficulty,
            currentHeight,
            currentTimestamp,
            intervalStartTimestamp);

        // Assert
        Assert.True(newDifficulty > currentDifficulty, 
            $"Difficulty should increase when blocks are faster. Was {currentDifficulty}, now {newDifficulty}");
    }

    [Fact]
    public void CalculateNextDifficulty_WithSlowerBlocks_DecreasesDifficulty()
    {
        // Arrange - Blocks are coming 2x slower than target
        long currentDifficulty = 1000;
        long currentHeight = 100;
        long targetTime = _defaultConfig.AdjustmentIntervalBlocks * _defaultConfig.TargetBlockTimeSeconds;
        long actualTime = targetTime * 2; // Double the expected time
        long currentTimestamp = actualTime;
        long intervalStartTimestamp = 0;

        // Act
        var newDifficulty = _adjuster.CalculateNextDifficulty(
            currentDifficulty,
            currentHeight,
            currentTimestamp,
            intervalStartTimestamp);

        // Assert
        Assert.True(newDifficulty < currentDifficulty,
            $"Difficulty should decrease when blocks are slower. Was {currentDifficulty}, now {newDifficulty}");
    }

    [Fact]
    public void CalculateNextDifficulty_WithPerfectTiming_MaintainsDifficulty()
    {
        // Arrange - Blocks exactly on target
        long currentDifficulty = 1000;
        long currentHeight = 100;
        long targetTime = _defaultConfig.AdjustmentIntervalBlocks * _defaultConfig.TargetBlockTimeSeconds;
        long currentTimestamp = targetTime;
        long intervalStartTimestamp = 0;

        // Act
        var newDifficulty = _adjuster.CalculateNextDifficulty(
            currentDifficulty,
            currentHeight,
            currentTimestamp,
            intervalStartTimestamp);

        // Assert - Should be approximately the same (may have small rounding difference)
        var difference = Math.Abs(newDifficulty - currentDifficulty);
        Assert.True(difference <= 1,
            $"Difficulty should remain stable with perfect timing. Was {currentDifficulty}, now {newDifficulty}");
    }

    [Fact]
    public void CalculateNextDifficulty_EnforcesMinimumDifficulty()
    {
        // Arrange - Very slow blocks that would push difficulty below minimum
        long currentDifficulty = 10;
        long currentHeight = 100;
        long targetTime = _defaultConfig.AdjustmentIntervalBlocks * _defaultConfig.TargetBlockTimeSeconds;
        long actualTime = targetTime * 1000; // Much slower
        long currentTimestamp = actualTime;
        long intervalStartTimestamp = 0;

        // Act
        var newDifficulty = _adjuster.CalculateNextDifficulty(
            currentDifficulty,
            currentHeight,
            currentTimestamp,
            intervalStartTimestamp);

        // Assert
        Assert.True(newDifficulty >= _defaultConfig.MinimumDifficulty,
            $"Difficulty should not fall below minimum. Got {newDifficulty}, minimum is {_defaultConfig.MinimumDifficulty}");
    }

    [Fact]
    public void CalculateNextDifficulty_EnforcesMaximumDifficulty()
    {
        // Arrange - Very fast blocks that would push difficulty above maximum
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 1, // No dampening for easier testing
            minimumDifficulty: 1,
            maximumDifficulty: 10000);
        var adjuster = new DifficultyAdjuster(config);

        long currentDifficulty = 9000;
        long currentHeight = 100;
        long targetTime = config.AdjustmentIntervalBlocks * config.TargetBlockTimeSeconds;
        long actualTime = 1; // Extremely fast
        long currentTimestamp = actualTime;
        long intervalStartTimestamp = 0;

        // Act
        var newDifficulty = adjuster.CalculateNextDifficulty(
            currentDifficulty,
            currentHeight,
            currentTimestamp,
            intervalStartTimestamp);

        // Assert
        Assert.True(newDifficulty <= config.MaximumDifficulty,
            $"Difficulty should not exceed maximum. Got {newDifficulty}, maximum is {config.MaximumDifficulty}");
    }

    [Fact]
    public void CalculateNextDifficulty_WithDampening_SmoothsAdjustment()
    {
        // Arrange - Compare dampened vs non-dampened adjustment
        var noDampeningConfig = new DifficultyAdjustmentConfig(
            dampeningFactor: 1); // No dampening
        var dampenedConfig = new DifficultyAdjustmentConfig(
            dampeningFactor: 4); // Standard dampening

        var noDampeningAdjuster = new DifficultyAdjuster(noDampeningConfig);
        var dampenedAdjuster = new DifficultyAdjuster(dampenedConfig);

        long currentDifficulty = 1000;
        long currentHeight = 100;
        long actualTime = 500; // 2x faster
        long currentTimestamp = actualTime;
        long intervalStartTimestamp = 0;

        // Act
        var noDampeningDiff = noDampeningAdjuster.CalculateNextDifficulty(
            currentDifficulty, currentHeight, currentTimestamp, intervalStartTimestamp);
        var dampenedDiff = dampenedAdjuster.CalculateNextDifficulty(
            currentDifficulty, currentHeight, currentTimestamp, intervalStartTimestamp);

        // Assert
        var noDampeningChange = Math.Abs(noDampeningDiff - currentDifficulty);
        var dampenedChange = Math.Abs(dampenedDiff - currentDifficulty);

        Assert.True(dampenedChange < noDampeningChange,
            $"Dampened adjustment should be smaller. No dampening change: {noDampeningChange}, dampened change: {dampenedChange}");
    }

    [Fact]
    public void CalculateNextDifficulty_WithZeroDifficulty_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _adjuster.CalculateNextDifficulty(
            currentDifficulty: 0,
            currentHeight: 100,
            currentTimestamp: 1000,
            intervalStartTimestamp: 0));
    }

    [Fact]
    public void CalculateNextDifficulty_WithNegativeDifficulty_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _adjuster.CalculateNextDifficulty(
            currentDifficulty: -1,
            currentHeight: 100,
            currentTimestamp: 1000,
            intervalStartTimestamp: 0));
    }

    [Fact]
    public void CalculateNextDifficulty_WithNegativeHeight_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _adjuster.CalculateNextDifficulty(
            currentDifficulty: 1000,
            currentHeight: -1,
            currentTimestamp: 1000,
            intervalStartTimestamp: 0));
    }

    [Fact]
    public void CalculateNextDifficulty_WithNegativeTimestamp_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _adjuster.CalculateNextDifficulty(
            currentDifficulty: 1000,
            currentHeight: 100,
            currentTimestamp: -1,
            intervalStartTimestamp: 0));
    }

    [Fact]
    public void CalculateNextDifficulty_WithCurrentTimestampBeforeStart_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _adjuster.CalculateNextDifficulty(
            currentDifficulty: 1000,
            currentHeight: 100,
            currentTimestamp: 100,
            intervalStartTimestamp: 200));
    }

    [Fact]
    public void CalculateNextDifficulty_WithZeroActualTime_HandlesSafely()
    {
        // Arrange - Same timestamp (zero time elapsed)
        long currentDifficulty = 1000;
        long currentHeight = 100;
        long timestamp = 1000;

        // Act - Should not throw and should handle gracefully
        var newDifficulty = _adjuster.CalculateNextDifficulty(
            currentDifficulty,
            currentHeight,
            timestamp,
            timestamp);

        // Assert
        Assert.True(newDifficulty > 0);
        Assert.True(newDifficulty >= _defaultConfig.MinimumDifficulty);
    }

    #endregion
}
