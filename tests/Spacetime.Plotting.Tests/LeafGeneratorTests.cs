using System.Security.Cryptography;

namespace Spacetime.Plotting.Tests;

public class LeafGeneratorTests
{
    [Fact]
    public void GenerateLeaf_WithValidInputs_ReturnsCorrectSize()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        long nonce = 0;

        // Act
        var leaf = LeafGenerator.GenerateLeaf(minerKey, plotSeed, nonce);

        // Assert
        Assert.NotNull(leaf);
        Assert.Equal(LeafGenerator.LeafSize, leaf.Length);
    }

    [Fact]
    public void GenerateLeaf_IsDeterministic()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        long nonce = 42;

        // Act
        var leaf1 = LeafGenerator.GenerateLeaf(minerKey, plotSeed, nonce);
        var leaf2 = LeafGenerator.GenerateLeaf(minerKey, plotSeed, nonce);

        // Assert
        Assert.Equal(leaf1, leaf2);
    }

    [Fact]
    public void GenerateLeaf_DifferentNonces_ProduceDifferentLeaves()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);

        // Act
        var leaf1 = LeafGenerator.GenerateLeaf(minerKey, plotSeed, 0);
        var leaf2 = LeafGenerator.GenerateLeaf(minerKey, plotSeed, 1);

        // Assert
        Assert.NotEqual(leaf1, leaf2);
    }

    [Fact]
    public void GenerateLeaf_DifferentMinerKeys_ProduceDifferentLeaves()
    {
        // Arrange
        var minerKey1 = RandomNumberGenerator.GetBytes(32);
        var minerKey2 = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        long nonce = 0;

        // Act
        var leaf1 = LeafGenerator.GenerateLeaf(minerKey1, plotSeed, nonce);
        var leaf2 = LeafGenerator.GenerateLeaf(minerKey2, plotSeed, nonce);

        // Assert
        Assert.NotEqual(leaf1, leaf2);
    }

    [Fact]
    public void GenerateLeaf_DifferentPlotSeeds_ProduceDifferentLeaves()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed1 = RandomNumberGenerator.GetBytes(32);
        var plotSeed2 = RandomNumberGenerator.GetBytes(32);
        long nonce = 0;

        // Act
        var leaf1 = LeafGenerator.GenerateLeaf(minerKey, plotSeed1, nonce);
        var leaf2 = LeafGenerator.GenerateLeaf(minerKey, plotSeed2, nonce);

        // Assert
        Assert.NotEqual(leaf1, leaf2);
    }

    [Fact]
    public void GenerateLeaf_InvalidMinerKeySize_ThrowsArgumentException()
    {
        // Arrange
        var minerKey = new byte[16]; // Wrong size
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        long nonce = 0;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            LeafGenerator.GenerateLeaf(minerKey, plotSeed, nonce));
    }

    [Fact]
    public void GenerateLeaf_InvalidPlotSeedSize_ThrowsArgumentException()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = new byte[16]; // Wrong size
        long nonce = 0;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            LeafGenerator.GenerateLeaf(minerKey, plotSeed, nonce));
    }

    [Fact]
    public void GenerateLeaf_NegativeNonce_ThrowsArgumentException()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        long nonce = -1;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            LeafGenerator.GenerateLeaf(minerKey, plotSeed, nonce));
    }

    [Fact]
    public async Task GenerateLeavesAsync_GeneratesCorrectCount()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        const int count = 100;

        // Act
        var leaves = new List<byte[]>();
        await foreach (var leaf in LeafGenerator.GenerateLeavesAsync(minerKey, plotSeed, 0, count))
        {
            leaves.Add(leaf);
        }

        // Assert
        Assert.Equal(count, leaves.Count);
    }

    [Fact]
    public async Task GenerateLeavesAsync_LeavesAreDeterministic()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        const int count = 10;

        // Act - Generate twice
        var leaves1 = new List<byte[]>();
        await foreach (var leaf in LeafGenerator.GenerateLeavesAsync(minerKey, plotSeed, 0, count))
        {
            leaves1.Add(leaf);
        }

        var leaves2 = new List<byte[]>();
        await foreach (var leaf in LeafGenerator.GenerateLeavesAsync(minerKey, plotSeed, 0, count))
        {
            leaves2.Add(leaf);
        }

        // Assert - All leaves should match
        Assert.Equal(leaves1.Count, leaves2.Count);
        for (int i = 0; i < leaves1.Count; i++)
        {
            Assert.Equal(leaves1[i], leaves2[i]);
        }
    }

    [Fact]
    public async Task GenerateLeavesAsync_StartNonceWorks()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);

        // Act - Generate with different start nonces
        var leaf0 = LeafGenerator.GenerateLeaf(minerKey, plotSeed, 5);

        var leavesFrom5 = new List<byte[]>();
        await foreach (var leaf in LeafGenerator.GenerateLeavesAsync(minerKey, plotSeed, 5, 1))
        {
            leavesFrom5.Add(leaf);
        }

        // Assert - First leaf from startNonce=5 should match nonce=5
        Assert.Equal(leaf0, leavesFrom5[0]);
    }

    [Fact]
    public async Task GenerateLeavesAsync_CancellationWorks()
    {
        // Arrange
        var minerKey = RandomNumberGenerator.GetBytes(32);
        var plotSeed = RandomNumberGenerator.GetBytes(32);
        var cts = new CancellationTokenSource();

        // Act & Assert
        var leaves = new List<byte[]>();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var leaf in LeafGenerator.GenerateLeavesAsync(
                minerKey, plotSeed, 0, 10000, cts.Token))
            {
                leaves.Add(leaf);
                if (leaves.Count == 5)
                {
                    cts.Cancel();
                }
            }
        });

        // Should have stopped early
        Assert.True(leaves.Count < 10000);
    }
}
