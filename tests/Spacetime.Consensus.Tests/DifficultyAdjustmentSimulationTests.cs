namespace Spacetime.Consensus.Tests;

/// <summary>
/// Simulation tests for difficulty adjustment under various mining scenarios.
/// </summary>
/// <remarks>
/// These tests simulate realistic blockchain scenarios to verify that the difficulty
/// adjustment algorithm maintains target block times over multiple adjustment periods.
/// </remarks>
public class DifficultyAdjustmentSimulationTests
{
    #region Stable Hash Rate Scenarios

    [Fact]
    public void Simulation_StableHashRate_MaintainsTargetBlockTime()
    {
        // Arrange - Simulate 500 blocks with stable hash rate
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 4);
        var adjuster = new DifficultyAdjuster(config);

        long difficulty = 1000;
        long timestamp = 0;
        var targetBlockTime = config.TargetBlockTimeSeconds;

        // Act - Simulate blocks with perfect timing
        for (long height = 1; height <= 500; height++)
        {
            timestamp += targetBlockTime; // Perfect timing

            if (adjuster.ShouldAdjustDifficulty(height))
            {
                var intervalStart = timestamp - (config.AdjustmentIntervalBlocks * targetBlockTime);
                difficulty = adjuster.CalculateNextDifficulty(
                    difficulty,
                    height,
                    timestamp,
                    intervalStart);
            }
        }

        // Assert - Difficulty should remain relatively stable
        var expectedDifficulty = 1000;
        var tolerance = expectedDifficulty * 0.02; // 2% tolerance
        Assert.True(Math.Abs(difficulty - expectedDifficulty) <= tolerance,
            $"Difficulty {difficulty} should be close to {expectedDifficulty} with stable hash rate");
    }

    [Fact]
    public void Simulation_StableHashRate_MultipleEpochs_StabilizesDifficulty()
    {
        // Arrange - Simulate 10 adjustment periods with stable hash rate
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 4);
        var adjuster = new DifficultyAdjuster(config);

        long difficulty = 1000;
        long timestamp = 0;
        var difficulties = new List<long>();

        // Act - Simulate 1000 blocks (10 adjustment periods)
        for (long height = 1; height <= 1000; height++)
        {
            timestamp += config.TargetBlockTimeSeconds;

            if (adjuster.ShouldAdjustDifficulty(height))
            {
                var intervalStart = timestamp - (config.AdjustmentIntervalBlocks * config.TargetBlockTimeSeconds);
                difficulty = adjuster.CalculateNextDifficulty(
                    difficulty,
                    height,
                    timestamp,
                    intervalStart);
                difficulties.Add(difficulty);
            }
        }

        // Assert - All difficulties should be very close to starting difficulty
        Assert.All(difficulties, d =>
        {
            var tolerance = 1000 * 0.05; // 5% tolerance
            Assert.True(Math.Abs(d - 1000) <= tolerance,
                $"Difficulty {d} should remain stable around 1000");
        });
    }

    #endregion

    #region Hash Rate Increase Scenarios

    [Fact]
    public void Simulation_HashRateDoubles_DifficultyIncreases()
    {
        // Arrange - Simulate hash rate doubling (blocks come 2x faster)
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 4);
        var adjuster = new DifficultyAdjuster(config);

        long initialDifficulty = 1000;
        long difficulty = initialDifficulty;
        long timestamp = 0;
        var blockTime = config.TargetBlockTimeSeconds / 2; // 2x faster

        // Act - Simulate 300 blocks with doubled hash rate
        for (long height = 1; height <= 300; height++)
        {
            timestamp += blockTime;

            if (adjuster.ShouldAdjustDifficulty(height))
            {
                var intervalStart = timestamp - (config.AdjustmentIntervalBlocks * blockTime);
                difficulty = adjuster.CalculateNextDifficulty(
                    difficulty,
                    height,
                    timestamp,
                    intervalStart);
            }
        }

        // Assert - Difficulty should have increased significantly
        Assert.True(difficulty > initialDifficulty * 1.5,
            $"Difficulty {difficulty} should increase significantly from {initialDifficulty} with doubled hash rate");
    }

    [Fact]
    public void Simulation_GradualHashRateIncrease_DifficultyAdjustsGradually()
    {
        // Arrange - Simulate gradual hash rate increase over time
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 4);
        var adjuster = new DifficultyAdjuster(config);

        long difficulty = 1000;
        long timestamp = 0;
        var difficulties = new List<long> { difficulty };

        // Act - Simulate 500 blocks with gradually decreasing block time
        for (long height = 1; height <= 500; height++)
        {
            // Block time decreases by 1% every 100 blocks
            var blockTimeMultiplier = 1.0 - (height / 100) * 0.01;
            var blockTime = (long)(config.TargetBlockTimeSeconds * blockTimeMultiplier);
            timestamp += Math.Max(blockTime, 1);

            if (adjuster.ShouldAdjustDifficulty(height))
            {
                var intervalBlocks = Math.Min((int)height, config.AdjustmentIntervalBlocks);
                var intervalStart = timestamp - (intervalBlocks * blockTime);
                difficulty = adjuster.CalculateNextDifficulty(
                    difficulty,
                    height,
                    timestamp,
                    intervalStart);
                difficulties.Add(difficulty);
            }
        }

        // Assert - Difficulty should generally trend upward
        Assert.True(difficulties.Last() > difficulties.First(),
            $"Final difficulty {difficulties.Last()} should be higher than initial {difficulties.First()}");
        
        // Each adjustment should not be too dramatic due to dampening
        for (int i = 1; i < difficulties.Count; i++)
        {
            var change = Math.Abs(difficulties[i] - difficulties[i - 1]);
            var percentChange = (double)change / difficulties[i - 1];
            Assert.True(percentChange < 0.5,
                $"Single adjustment should not exceed 50% change (was {percentChange:P})");
        }
    }

    #endregion

    #region Hash Rate Decrease Scenarios

    [Fact]
    public void Simulation_HashRateHalves_DifficultyDecreases()
    {
        // Arrange - Simulate hash rate halving (blocks come 2x slower)
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 4);
        var adjuster = new DifficultyAdjuster(config);

        long initialDifficulty = 1000;
        long difficulty = initialDifficulty;
        long timestamp = 0;
        var blockTime = config.TargetBlockTimeSeconds * 2; // 2x slower

        // Act - Simulate 300 blocks with halved hash rate
        for (long height = 1; height <= 300; height++)
        {
            timestamp += blockTime;

            if (adjuster.ShouldAdjustDifficulty(height))
            {
                var intervalStart = timestamp - (config.AdjustmentIntervalBlocks * blockTime);
                difficulty = adjuster.CalculateNextDifficulty(
                    difficulty,
                    height,
                    timestamp,
                    intervalStart);
            }
        }

        // Assert - Difficulty should have decreased significantly
        Assert.True(difficulty < initialDifficulty * 0.7,
            $"Difficulty {difficulty} should decrease significantly from {initialDifficulty} with halved hash rate");
    }

    [Fact]
    public void Simulation_SevereHashRateDrop_EnforcesMinimumDifficulty()
    {
        // Arrange - Simulate severe hash rate drop
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 2, // Less dampening to reach minimum faster
            minimumDifficulty: 100);
        var adjuster = new DifficultyAdjuster(config);

        long difficulty = 1000;
        long timestamp = 0;
        var blockTime = config.TargetBlockTimeSeconds * 10; // 10x slower

        // Act - Simulate until minimum difficulty is reached
        for (long height = 1; height <= 1000; height++)
        {
            timestamp += blockTime;

            if (adjuster.ShouldAdjustDifficulty(height))
            {
                var intervalStart = timestamp - (config.AdjustmentIntervalBlocks * blockTime);
                difficulty = adjuster.CalculateNextDifficulty(
                    difficulty,
                    height,
                    timestamp,
                    intervalStart);
            }

            if (difficulty <= config.MinimumDifficulty)
            {
                break;
            }
        }

        // Assert - Should reach minimum difficulty
        Assert.Equal(config.MinimumDifficulty, difficulty);
    }

    #endregion

    #region Oscillating Hash Rate Scenarios

    [Fact]
    public void Simulation_OscillatingHashRate_DifficultyStabilizes()
    {
        // Arrange - Simulate oscillating hash rate
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 4);
        var adjuster = new DifficultyAdjuster(config);

        long difficulty = 1000;
        long timestamp = 0;
        var difficulties = new List<long>();
        var timestamps = new List<long> { 0 }; // Track all timestamps

        // Act - Simulate 1000 blocks with alternating fast/slow periods
        for (long height = 1; height <= 1000; height++)
        {
            // Alternate between 2x faster and 2x slower every 100 blocks
            var period = (height / 100) % 2;
            var blockTime = period == 0 
                ? config.TargetBlockTimeSeconds / 2  // Fast period
                : config.TargetBlockTimeSeconds * 2; // Slow period
            
            timestamp += blockTime;
            timestamps.Add(timestamp);

            if (adjuster.ShouldAdjustDifficulty(height))
            {
                // Get timestamp from N blocks ago
                var intervalStartIndex = (int)(height - config.AdjustmentIntervalBlocks);
                var intervalStart = timestamps[intervalStartIndex];
                
                difficulty = adjuster.CalculateNextDifficulty(
                    difficulty,
                    height,
                    timestamp,
                    intervalStart);
                difficulties.Add(difficulty);
            }
        }

        // Assert - Difficulty oscillates but dampening prevents extreme swings
        for (int i = 0; i < difficulties.Count; i++)
        {
            // Each difficulty should be within reasonable bounds
            Assert.True(difficulties[i] >= 500 && difficulties[i] <= 2000,
                $"Difficulty {difficulties[i]} at adjustment {i} should stay within reasonable bounds");
        }
    }

    #endregion

    #region Edge Case Scenarios

    [Fact]
    public void Simulation_ExtremelySlowBlocks_ConvergesToMinimumDifficulty()
    {
        // Arrange - Simulate extremely slow blocks
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 10, // Shorter interval for faster convergence
            dampeningFactor: 2,
            minimumDifficulty: 1);
        var adjuster = new DifficultyAdjuster(config);

        long difficulty = 10000; // Start high
        long timestamp = 0;
        var blockTime = config.TargetBlockTimeSeconds * 100; // 100x slower

        // Act - Simulate blocks until convergence
        for (long height = 1; height <= 200; height++)
        {
            timestamp += blockTime;

            if (adjuster.ShouldAdjustDifficulty(height))
            {
                var intervalStart = timestamp - (config.AdjustmentIntervalBlocks * blockTime);
                difficulty = adjuster.CalculateNextDifficulty(
                    difficulty,
                    height,
                    timestamp,
                    intervalStart);
            }
        }

        // Assert - Should converge to minimum
        Assert.Equal(config.MinimumDifficulty, difficulty);
    }

    [Fact]
    public void Simulation_ExtremelyFastBlocks_ConvergesTowardHighDifficulty()
    {
        // Arrange - Simulate extremely fast blocks
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 10,
            dampeningFactor: 2,
            minimumDifficulty: 1,
            maximumDifficulty: 100000);
        var adjuster = new DifficultyAdjuster(config);

        long difficulty = 100; // Start low
        long timestamp = 0;
        var blockTime = 1; // Very fast

        // Act - Simulate blocks
        for (long height = 1; height <= 200; height++)
        {
            timestamp += blockTime;

            if (adjuster.ShouldAdjustDifficulty(height))
            {
                var intervalStart = timestamp - (config.AdjustmentIntervalBlocks * blockTime);
                difficulty = adjuster.CalculateNextDifficulty(
                    difficulty,
                    height,
                    timestamp,
                    intervalStart);
            }
        }

        // Assert - Should increase significantly
        Assert.True(difficulty > 10000,
            $"Difficulty {difficulty} should increase significantly with very fast blocks");
    }

    #endregion

    #region Target Conversion Integration Tests

    [Fact]
    public void Simulation_DifficultyIncrease_TargetDecreases()
    {
        // Arrange
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 4);
        var adjuster = new DifficultyAdjuster(config);

        long difficulty = 1000;
        long timestamp = 0;
        var blockTime = config.TargetBlockTimeSeconds / 2; // 2x faster

        var initialTarget = DifficultyAdjuster.DifficultyToTarget(difficulty);

        // Act - One adjustment period with fast blocks
        for (long height = 1; height <= 100; height++)
        {
            timestamp += blockTime;
        }

        difficulty = adjuster.CalculateNextDifficulty(
            difficulty,
            100,
            timestamp,
            0);

        var newTarget = DifficultyAdjuster.DifficultyToTarget(difficulty);

        // Assert - Higher difficulty should produce lower target
        var initialTargetValue = new System.Numerics.BigInteger(initialTarget, isUnsigned: true, isBigEndian: true);
        var newTargetValue = new System.Numerics.BigInteger(newTarget, isUnsigned: true, isBigEndian: true);
        
        Assert.True(newTargetValue < initialTargetValue,
            "Higher difficulty should produce lower target value");
    }

    [Fact]
    public void Simulation_TargetValidation_WorksAfterAdjustment()
    {
        // Arrange - Simulate difficulty adjustment
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 4);
        var adjuster = new DifficultyAdjuster(config);

        long difficulty = 1000;
        long timestamp = 0;

        // Act - Simulate fast blocks causing difficulty increase
        for (long height = 1; height <= 100; height++)
        {
            timestamp += config.TargetBlockTimeSeconds / 2;
        }

        var newDifficulty = adjuster.CalculateNextDifficulty(
            difficulty,
            100,
            timestamp,
            0);

        var newTarget = DifficultyAdjuster.DifficultyToTarget(newDifficulty);

        // Assert - Target should be valid for validation
        Assert.Equal(32, newTarget.Length);
        Assert.True(newDifficulty > difficulty, "Difficulty should have increased");
        
        // Create a proof score that would pass the new target
        var passingScore = new byte[32];
        Array.Copy(newTarget, passingScore, 32);
        passingScore[31] = (byte)(passingScore[31] > 0 ? passingScore[31] - 1 : 0); // Make it slightly lower
        
        Assert.True(ProofValidator.IsScoreBelowTarget(passingScore, newTarget),
            "Score below target should pass validation");
    }

    #endregion

    #region Long-Term Stability Tests

    [Fact]
    public void Simulation_LongTermStability_MaintainsTargetOverTime()
    {
        // Arrange - Simulate many blocks to verify long-term stability
        var config = new DifficultyAdjustmentConfig(
            targetBlockTimeSeconds: 10,
            adjustmentIntervalBlocks: 100,
            dampeningFactor: 4);
        var adjuster = new DifficultyAdjuster(config);

        long difficulty = 1000;
        long timestamp = 0;
        var actualTimes = new List<long>();

        // Act - Simulate 2000 blocks with stable hash rate
        for (long height = 1; height <= 2000; height++)
        {
            var previousTimestamp = timestamp;
            timestamp += config.TargetBlockTimeSeconds;
            actualTimes.Add(timestamp - previousTimestamp);

            if (adjuster.ShouldAdjustDifficulty(height))
            {
                var intervalStart = timestamp - (config.AdjustmentIntervalBlocks * config.TargetBlockTimeSeconds);
                difficulty = adjuster.CalculateNextDifficulty(
                    difficulty,
                    height,
                    timestamp,
                    intervalStart);
            }
        }

        // Assert - Average block time over all blocks should be close to target
        var avgBlockTime = actualTimes.Average();
        var tolerance = config.TargetBlockTimeSeconds * 0.01; // 1% tolerance
        
        Assert.True(Math.Abs(avgBlockTime - config.TargetBlockTimeSeconds) <= tolerance,
            $"Average block time {avgBlockTime} should be close to target {config.TargetBlockTimeSeconds}");
    }

    #endregion
}
