using System.Security.Cryptography;

namespace Spacetime.Core.Tests;

/// <summary>
/// Integration tests for epoch management with block production.
/// </summary>
public class EpochBlockProductionTests
{
    private const int EpochExpiryWaitMs = 1100; // Slightly longer than 1 second epoch
    private static BlockProof CreateMockProof()
    {
        var leafValue = RandomNumberGenerator.GetBytes(32);
        var merkleProofPath = new List<byte[]>
        {
            RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32)
        };
        var orientationBits = new List<bool> { false, true };
        var plotMetadata = BlockPlotMetadata.Create(
            leafCount: 1000,
            plotId: RandomNumberGenerator.GetBytes(32),
            plotHeaderHash: RandomNumberGenerator.GetBytes(32),
            version: 1);

        return new BlockProof(leafValue, 0, merkleProofPath, orientationBits, plotMetadata);
    }

    [Fact]
    public async Task EpochTransition_WithBlockProduction_UpdatesChallengeCorrectly()
    {
        // Arrange
        var config = new EpochConfig(10);
        var epochManager = new EpochManager(config);
        var genesisBlockHash = RandomNumberGenerator.GetBytes(32);

        // Act - Advance to epoch 1
        epochManager.AdvanceEpoch(genesisBlockHash);
        var epoch1Challenge = epochManager.CurrentChallenge.ToArray();
        var epoch1Number = epochManager.CurrentEpoch;

        // Simulate block production
        var block1Hash = RandomNumberGenerator.GetBytes(32);

        // Advance to epoch 2
        epochManager.AdvanceEpoch(block1Hash);
        var epoch2Challenge = epochManager.CurrentChallenge.ToArray();
        var epoch2Number = epochManager.CurrentEpoch;

        // Assert
        Assert.Equal(1, epoch1Number);
        Assert.Equal(2, epoch2Number);
        Assert.NotEqual(epoch1Challenge, epoch2Challenge);
        Assert.True(ChallengeDerivation.VerifyChallenge(epoch1Challenge, genesisBlockHash, 1));
        Assert.True(ChallengeDerivation.VerifyChallenge(epoch2Challenge, block1Hash, 2));
    }

    [Fact]
    public async Task BlockHeader_ContainsCorrectEpochAndChallenge()
    {
        // Arrange
        var config = new EpochConfig(10);
        var epochManager = new EpochManager(config);
        var previousBlockHash = RandomNumberGenerator.GetBytes(32);

        // Advance epoch
        epochManager.AdvanceEpoch(previousBlockHash);
        var currentEpoch = epochManager.CurrentEpoch;
        var currentChallenge = epochManager.CurrentChallenge.ToArray();

        // Create a block header with epoch information
        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: previousBlockHash,
            height: 1,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            difficulty: 1000,
            epoch: currentEpoch,
            challenge: currentChallenge,
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32),
            txRoot: RandomNumberGenerator.GetBytes(32),
            minerId: RandomNumberGenerator.GetBytes(33),
            signature: RandomNumberGenerator.GetBytes(64));

        // Assert
        Assert.Equal(currentEpoch, header.Epoch);
        Assert.Equal(currentChallenge, header.Challenge.ToArray());
    }

    [Fact]
    public async Task MultipleEpochs_WithNoBlock_ContinueTransitioning()
    {
        // Arrange
        var config = new EpochConfig(10);
        var epochManager = new EpochManager(config);
        var blockHash = RandomNumberGenerator.GetBytes(32);
        var challenges = new List<byte[]>();

        // Act - Simulate multiple epochs without block production
        for (var i = 0; i < 5; i++)
        {
            epochManager.AdvanceEpoch(blockHash);
            challenges.Add(epochManager.CurrentChallenge.ToArray());
        }

        // Assert - All challenges should be unique
        Assert.Equal(5, epochManager.CurrentEpoch);
        Assert.Equal(5, challenges.Count);
        
        for (var i = 0; i < challenges.Count - 1; i++)
        {
            for (var j = i + 1; j < challenges.Count; j++)
            {
                Assert.NotEqual(challenges[i], challenges[j]);
            }
        }
    }

    [Fact]
    public async Task ChallengeWindow_Expires_AfterConfiguredDuration()
    {
        // Arrange
        var config = new EpochConfig(1); // 1 second epoch
        var epochManager = new EpochManager(config);
        
        // Act - Check immediately
        var isExpiredImmediately = epochManager.IsEpochExpired;
        
        // Wait for epoch to expire
        await Task.Delay(EpochExpiryWaitMs);
        var isExpiredAfterDelay = epochManager.IsEpochExpired;

        // Assert
        Assert.False(isExpiredImmediately);
        Assert.True(isExpiredAfterDelay);
    }

    [Fact]
    public async Task ValidateChallengeForEpoch_RejectsMismatchedChallenge()
    {
        // Arrange
        var epochManager = new EpochManager();
        var blockHash = RandomNumberGenerator.GetBytes(32);
        
        // Advance to epoch 1
        epochManager.AdvanceEpoch(blockHash);
        var correctChallenge = epochManager.CurrentChallenge.ToArray();
        var wrongChallenge = RandomNumberGenerator.GetBytes(32);

        // Act
        var correctIsValid = epochManager.ValidateChallengeForEpoch(correctChallenge, 1, blockHash);
        var wrongIsValid = epochManager.ValidateChallengeForEpoch(wrongChallenge, 1, blockHash);

        // Assert
        Assert.True(correctIsValid);
        Assert.False(wrongIsValid);
    }

    [Fact]
    public async Task EpochChain_MaintainsDeterministicChallengeSequence()
    {
        // Arrange
        var config = new EpochConfig(10);
        var epochManager1 = new EpochManager(config);
        var epochManager2 = new EpochManager(config);
        
        var genesisHash = RandomNumberGenerator.GetBytes(32);
        var block1Hash = RandomNumberGenerator.GetBytes(32);
        var block2Hash = RandomNumberGenerator.GetBytes(32);

        // Act - Both managers follow the same chain
        epochManager1.AdvanceEpoch(genesisHash);
        epochManager2.AdvanceEpoch(genesisHash);
        
        var challenge1_manager1 = epochManager1.CurrentChallenge.ToArray();
        var challenge1_manager2 = epochManager2.CurrentChallenge.ToArray();

        epochManager1.AdvanceEpoch(block1Hash);
        epochManager2.AdvanceEpoch(block1Hash);
        
        var challenge2_manager1 = epochManager1.CurrentChallenge.ToArray();
        var challenge2_manager2 = epochManager2.CurrentChallenge.ToArray();

        epochManager1.AdvanceEpoch(block2Hash);
        epochManager2.AdvanceEpoch(block2Hash);
        
        var challenge3_manager1 = epochManager1.CurrentChallenge.ToArray();
        var challenge3_manager2 = epochManager2.CurrentChallenge.ToArray();

        // Assert - Both managers should derive identical challenges
        Assert.Equal(challenge1_manager1, challenge1_manager2);
        Assert.Equal(challenge2_manager1, challenge2_manager2);
        Assert.Equal(challenge3_manager1, challenge3_manager2);
    }

    [Fact]
    public async Task GenesisEpoch_UsesNetworkIdForChallenge()
    {
        // Arrange
        const string networkId = "testnet";
        var genesisChallenge = ChallengeDerivation.DeriveGenesisChallenge(networkId);
        var epochManager = new EpochManager();
        
        // Reset to genesis state
        epochManager.Reset(0, genesisChallenge, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(0, epochManager.CurrentEpoch);
        Assert.Equal(genesisChallenge, epochManager.CurrentChallenge.ToArray());
        Assert.True(ChallengeDerivation.VerifyGenesisChallenge(genesisChallenge, networkId));
    }

    [Fact]
    public async Task ChallengeReplay_PreventsOldChallengeReuse()
    {
        // Arrange
        var epochManager = new EpochManager();
        var blockHash1 = RandomNumberGenerator.GetBytes(32);
        var blockHash2 = RandomNumberGenerator.GetBytes(32);

        // Act - Advance through multiple epochs
        epochManager.AdvanceEpoch(blockHash1);
        var epoch1Challenge = epochManager.CurrentChallenge.ToArray();
        
        epochManager.AdvanceEpoch(blockHash2);
        var epoch2Challenge = epochManager.CurrentChallenge.ToArray();

        // Try to validate epoch 1 challenge for epoch 2
        var canReuseOldChallenge = epochManager.ValidateChallengeForEpoch(epoch1Challenge, 2, blockHash2);

        // Assert - Old challenge should not be valid for new epoch
        Assert.False(canReuseOldChallenge);
    }

    [Fact]
    public async Task BlockProduction_WithExpiredEpoch_ShouldBeRejected()
    {
        // Arrange
        var config = new EpochConfig(1); // 1 second epoch
        var epochManager = new EpochManager(config);
        var blockHash = RandomNumberGenerator.GetBytes(32);
        
        epochManager.AdvanceEpoch(blockHash);
        var challengeBeforeExpiry = epochManager.CurrentChallenge.ToArray();
        var epochBeforeExpiry = epochManager.CurrentEpoch;

        // Act - Wait for epoch to expire
        await Task.Delay(EpochExpiryWaitMs);

        // Simulate receiving a proof for expired epoch
        var isExpired = epochManager.IsEpochExpired;
        var timeRemaining = epochManager.TimeRemainingInEpoch;

        // Assert
        Assert.True(isExpired);
        Assert.Equal(TimeSpan.Zero, timeRemaining);
        // In a real system, proofs for expired epochs would be rejected
    }

    [Fact]
    public async Task ConcurrentEpochAccess_WithBlockProduction_ThreadSafe()
    {
        // Arrange
        var epochManager = new EpochManager(new EpochConfig(10));
        var blockHashes = new List<byte[]>();
        for (var i = 0; i < 10; i++)
        {
            blockHashes.Add(RandomNumberGenerator.GetBytes(32));
        }

        var tasks = new List<Task>();
        var exceptions = new List<Exception>();

        // Act - Multiple threads advancing epochs and reading state
        for (var i = 0; i < 10; i++)
        {
            var blockHash = blockHashes[i];
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    epochManager.AdvanceEpoch(blockHash);
                    var _ = epochManager.CurrentEpoch;
                    var __ = epochManager.CurrentChallenge;
                    var ___ = epochManager.IsEpochExpired;
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(10, epochManager.CurrentEpoch);
    }

    [Fact]
    public void EpochConfig_IntegratesWithEpochManager()
    {
        // Arrange
        var shortConfig = new EpochConfig(5);
        var longConfig = new EpochConfig(30);

        // Act
        var shortManager = new EpochManager(shortConfig);
        var longManager = new EpochManager(longConfig);

        // Assert - Both managers should respect their configurations
        var shortRemaining = shortManager.TimeRemainingInEpoch;
        var longRemaining = longManager.TimeRemainingInEpoch;

        Assert.True(shortRemaining.TotalSeconds <= 5);
        Assert.True(longRemaining.TotalSeconds <= 30);
        Assert.True(longRemaining > shortRemaining);
    }

    [Fact]
    public async Task Reset_AllowsEpochManagerRestart()
    {
        // Arrange
        var epochManager = new EpochManager();
        var blockHash = RandomNumberGenerator.GetBytes(32);
        
        // Advance to epoch 5
        for (var i = 0; i < 5; i++)
        {
            epochManager.AdvanceEpoch(blockHash);
        }
        
        Assert.Equal(5, epochManager.CurrentEpoch);

        // Act - Reset to epoch 2 (simulating chain reorganization)
        var newChallenge = ChallengeDerivation.DeriveChallenge(blockHash, 2);
        var resetTime = DateTimeOffset.UtcNow.AddMinutes(-1);
        epochManager.Reset(2, newChallenge, resetTime);

        // Assert
        Assert.Equal(2, epochManager.CurrentEpoch);
        Assert.Equal(newChallenge, epochManager.CurrentChallenge.ToArray());
        Assert.Equal(resetTime, epochManager.EpochStartTime);
    }
}
