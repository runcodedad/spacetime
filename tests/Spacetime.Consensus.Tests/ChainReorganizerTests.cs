using NSubstitute;
using System.Security.Cryptography;
using Spacetime.Core;
using Spacetime.Storage;

namespace Spacetime.Consensus.Tests;

/// <summary>
/// Unit tests for ChainReorganizer.
/// </summary>
public class ChainReorganizerTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly RocksDbChainStorage _storage;
    private readonly IStateManager _stateManager;
    private readonly IMempool _mempool;
    private readonly ChainReorganizer _reorganizer;
    private readonly ReorgConfig _config;

    public ChainReorganizerTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"spacetime_test_{Guid.NewGuid():N}");
        _storage = RocksDbChainStorage.Open(_testDbPath);
        _stateManager = Substitute.For<IStateManager>();
        _mempool = Substitute.For<IMempool>();
        _config = new ReorgConfig { MaxReorgDepth = 100 };
        _reorganizer = new ChainReorganizer(_storage, _stateManager, _config, _mempool);

        // Setup default state manager behavior
        _stateManager.CreateSnapshot().Returns(1);
        _stateManager.ApplyBlockAsync(Arg.Any<Block>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new byte[32]));
    }

    public void Dispose()
    {
        _storage.Dispose();
        if (Directory.Exists(_testDbPath))
        {
            Directory.Delete(_testDbPath, recursive: true);
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullStorage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ChainReorganizer(null!, _stateManager, _config));
    }

    [Fact]
    public void Constructor_WithNullStateManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ChainReorganizer(_storage, null!, _config));
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ChainReorganizer(_storage, _stateManager, null!));
    }

    [Fact]
    public void Constructor_WithInvalidConfig_ThrowsArgumentException()
    {
        // Arrange
        var invalidConfig = new ReorgConfig { MaxReorgDepth = -1 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ChainReorganizer(_storage, _stateManager, invalidConfig));
    }

    #endregion

    #region GetCumulativeDifficultyAsync Tests

    [Fact]
    public async Task GetCumulativeDifficultyAsync_WithInvalidHash_ThrowsArgumentException()
    {
        // Arrange
        var invalidHash = new byte[16]; // Not 32 bytes

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _reorganizer.GetCumulativeDifficultyAsync(invalidHash));
    }

    [Fact]
    public async Task GetCumulativeDifficultyAsync_WithSingleBlock_ReturnsBlockDifficulty()
    {
        // Arrange
        var genesisBlock = CreateGenesisBlock(difficulty: 1000);
        var genesisHash = genesisBlock.Header.ComputeHash();
        _storage.Blocks.StoreBlock(genesisBlock);

        // Act
        var cumulativeDifficulty = await _reorganizer.GetCumulativeDifficultyAsync(genesisHash);

        // Assert
        Assert.Equal(1000, cumulativeDifficulty);
    }

    [Fact]
    public async Task GetCumulativeDifficultyAsync_WithChain_ReturnsSumOfDifficulties()
    {
        // Arrange
        var genesisBlock = CreateGenesisBlock(difficulty: 1000);
        var block1 = CreateBlock(genesisBlock, difficulty: 1200);
        var block2 = CreateBlock(block1, difficulty: 1500);

        _storage.Blocks.StoreBlock(genesisBlock);
        _storage.Blocks.StoreBlock(block1);
        _storage.Blocks.StoreBlock(block2);

        // Act
        var cumulativeDifficulty = await _reorganizer.GetCumulativeDifficultyAsync(
            block2.Header.ComputeHash());

        // Assert
        Assert.Equal(3700, cumulativeDifficulty); // 1000 + 1200 + 1500
    }

    [Fact]
    public async Task GetCumulativeDifficultyAsync_CachesCumulativeDifficulty()
    {
        // Arrange
        var genesisBlock = CreateGenesisBlock(difficulty: 1000);
        var block1 = CreateBlock(genesisBlock, difficulty: 1200);
        var genesisHash = genesisBlock.Header.ComputeHash();
        var block1Hash = block1.Header.ComputeHash();

        _storage.Blocks.StoreBlock(genesisBlock);
        _storage.Blocks.StoreBlock(block1);

        // Act
        await _reorganizer.GetCumulativeDifficultyAsync(block1Hash);

        // Assert - verify cumulative difficulty was cached
        var cachedGenesisDifficulty = _storage.Metadata.GetCumulativeDifficulty(genesisHash);
        var cachedBlock1Difficulty = _storage.Metadata.GetCumulativeDifficulty(block1Hash);

        Assert.NotNull(cachedGenesisDifficulty);
        Assert.NotNull(cachedBlock1Difficulty);
        Assert.Equal(2200, cachedBlock1Difficulty); // 1000 + 1200
        
        // Genesis difficulty should be stored when traversing backwards from block1
        Assert.True(cachedGenesisDifficulty > 0);
    }

    #endregion

    #region FindForkPointAsync Tests

    [Fact]
    public async Task FindForkPointAsync_WithNullBlocks_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _reorganizer.FindForkPointAsync(null!));
    }

    [Fact]
    public async Task FindForkPointAsync_WithEmptyList_ReturnsMinusOne()
    {
        // Arrange
        var emptyList = Array.Empty<Block>();

        // Act
        var forkPoint = await _reorganizer.FindForkPointAsync(emptyList);

        // Assert
        Assert.Equal(-1, forkPoint);
    }

    [Fact]
    public async Task FindForkPointAsync_WithCommonAncestor_ReturnsForkHeight()
    {
        // Arrange
        // Build main chain: genesis -> block1 -> block2
        var genesisBlock = CreateGenesisBlock(difficulty: 1000);
        var block1 = CreateBlock(genesisBlock, difficulty: 1100);
        var block2 = CreateBlock(block1, difficulty: 1200);

        _storage.Blocks.StoreBlock(genesisBlock);
        _storage.Blocks.StoreBlock(block1);
        _storage.Blocks.StoreBlock(block2);

        // Build alternative chain: block1 -> altBlock2
        var altBlock2 = CreateBlock(block1, difficulty: 1300);

        // Act
        var forkPoint = await _reorganizer.FindForkPointAsync(new[] { altBlock2 });

        // Assert
        Assert.Equal(1, forkPoint); // Fork at block1
    }

    [Fact]
    public async Task FindForkPointAsync_WithNoCommonAncestor_ReturnsMinusOne()
    {
        // Arrange
        // Build main chain
        var genesisBlock = CreateGenesisBlock(difficulty: 1000);
        _storage.Blocks.StoreBlock(genesisBlock);

        // Build completely different alternative chain
        var altGenesis = CreateGenesisBlock(difficulty: 2000);
        var altBlock1 = CreateBlock(altGenesis, difficulty: 2100);

        // Act
        var forkPoint = await _reorganizer.FindForkPointAsync(new[] { altBlock1 });

        // Assert
        Assert.Equal(-1, forkPoint);
    }

    #endregion

    #region TryReorganizeAsync Tests

    [Fact]
    public async Task TryReorganizeAsync_WithNullTip_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _reorganizer.TryReorganizeAsync(null!, Array.Empty<Block>()));
    }

    [Fact]
    public async Task TryReorganizeAsync_WithNullBlocks_ThrowsArgumentNullException()
    {
        // Arrange
        var block = CreateGenesisBlock(difficulty: 1000);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _reorganizer.TryReorganizeAsync(block, null!));
    }

    [Fact]
    public async Task TryReorganizeAsync_WithNoCurrentChain_ReturnsFalse()
    {
        // Arrange
        var altBlock = CreateGenesisBlock(difficulty: 1000);

        // Act
        var result = await _reorganizer.TryReorganizeAsync(altBlock, new[] { altBlock });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryReorganizeAsync_WithLowerDifficulty_ReturnsFalse()
    {
        // Arrange
        // Setup current chain with higher difficulty
        var genesisBlock = CreateGenesisBlock(difficulty: 1000);
        var block1 = CreateBlock(genesisBlock, difficulty: 2000);

        _storage.Blocks.StoreBlock(genesisBlock);
        _storage.Blocks.StoreBlock(block1);
        _storage.Metadata.SetBestBlockHash(block1.Header.ComputeHash());
        _storage.Metadata.SetChainHeight(1);
        _storage.Metadata.SetCumulativeDifficulty(genesisBlock.Header.ComputeHash(), 1000);
        _storage.Metadata.SetCumulativeDifficulty(block1.Header.ComputeHash(), 3000);

        // Alternative chain with lower cumulative difficulty
        var altBlock1 = CreateBlock(genesisBlock, difficulty: 1500);
        _storage.Blocks.StoreBlock(altBlock1);
        _storage.Metadata.SetCumulativeDifficulty(altBlock1.Header.ComputeHash(), 2500);

        // Act
        var result = await _reorganizer.TryReorganizeAsync(altBlock1, new[] { altBlock1 });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryReorganizeAsync_WithHigherDifficulty_PerformsReorg()
    {
        // Arrange
        // Setup current chain
        var genesisBlock = CreateGenesisBlock(difficulty: 1000);
        var block1 = CreateBlock(genesisBlock, difficulty: 1000);

        _storage.Blocks.StoreBlock(genesisBlock);
        _storage.Blocks.StoreBlock(block1);
        _storage.Metadata.SetBestBlockHash(block1.Header.ComputeHash());
        _storage.Metadata.SetChainHeight(1);
        _storage.Metadata.SetCumulativeDifficulty(genesisBlock.Header.ComputeHash(), 1000);
        _storage.Metadata.SetCumulativeDifficulty(block1.Header.ComputeHash(), 2000);

        // Alternative chain with higher cumulative difficulty
        var altBlock1 = CreateBlock(genesisBlock, difficulty: 1500);
        _storage.Blocks.StoreBlock(altBlock1);
        _storage.Metadata.SetCumulativeDifficulty(altBlock1.Header.ComputeHash(), 2500);

        // Act
        var result = await _reorganizer.TryReorganizeAsync(altBlock1, new[] { altBlock1 });

        // Assert
        Assert.True(result);

        // Verify chain tip was updated
        var newTip = _storage.Metadata.GetBestBlockHash();
        Assert.NotNull(newTip);
        Assert.Equal(altBlock1.Header.ComputeHash(), newTip.Value.ToArray());

        // Verify old block was marked as orphaned
        Assert.True(_storage.Blocks.IsOrphaned(block1.Header.ComputeHash()));
    }

    [Fact]
    public async Task TryReorganizeAsync_ExceedingMaxDepth_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ReorgConfig { MaxReorgDepth = 2 };
        var reorganizer = new ChainReorganizer(_storage, _stateManager, config);

        // Build a long current chain
        var genesisBlock = CreateGenesisBlock(difficulty: 1000);
        var block1 = CreateBlock(genesisBlock, difficulty: 1000);
        var block2 = CreateBlock(block1, difficulty: 1000);
        var block3 = CreateBlock(block2, difficulty: 1000);

        _storage.Blocks.StoreBlock(genesisBlock);
        _storage.Blocks.StoreBlock(block1);
        _storage.Blocks.StoreBlock(block2);
        _storage.Blocks.StoreBlock(block3);
        _storage.Metadata.SetBestBlockHash(block3.Header.ComputeHash());
        _storage.Metadata.SetChainHeight(3);
        _storage.Metadata.SetCumulativeDifficulty(genesisBlock.Header.ComputeHash(), 1000);
        _storage.Metadata.SetCumulativeDifficulty(block1.Header.ComputeHash(), 2000);
        _storage.Metadata.SetCumulativeDifficulty(block2.Header.ComputeHash(), 3000);
        _storage.Metadata.SetCumulativeDifficulty(block3.Header.ComputeHash(), 4000);

        // Alternative chain forking from genesis (depth = 3 > max = 2)
        var altBlock1 = CreateBlock(genesisBlock, difficulty: 2000);
        var altBlock2 = CreateBlock(altBlock1, difficulty: 2000);
        var altBlock3 = CreateBlock(altBlock2, difficulty: 2000);

        _storage.Blocks.StoreBlock(altBlock1);
        _storage.Metadata.SetCumulativeDifficulty(altBlock1.Header.ComputeHash(), 3000);
        _storage.Metadata.SetCumulativeDifficulty(altBlock3.Header.ComputeHash(), 7000);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await reorganizer.TryReorganizeAsync(
                altBlock3,
                new[] { altBlock1, altBlock2, altBlock3 }));
    }

    [Fact]
    public async Task TryReorganizeAsync_EmitsReorgEvent()
    {
        // Arrange
        ChainReorgEvent? capturedEvent = null;
        _reorganizer.ChainReorganized += (sender, e) => capturedEvent = e;

        // Setup current chain
        var genesisBlock = CreateGenesisBlock(difficulty: 1000);
        var block1 = CreateBlock(genesisBlock, difficulty: 1000);

        _storage.Blocks.StoreBlock(genesisBlock);
        _storage.Blocks.StoreBlock(block1);
        _storage.Metadata.SetBestBlockHash(block1.Header.ComputeHash());
        _storage.Metadata.SetChainHeight(1);
        _storage.Metadata.SetCumulativeDifficulty(genesisBlock.Header.ComputeHash(), 1000);
        _storage.Metadata.SetCumulativeDifficulty(block1.Header.ComputeHash(), 2000);

        // Alternative chain
        var altBlock1 = CreateBlock(genesisBlock, difficulty: 1500);
        _storage.Blocks.StoreBlock(altBlock1);
        _storage.Metadata.SetCumulativeDifficulty(altBlock1.Header.ComputeHash(), 2500);

        // Act
        await _reorganizer.TryReorganizeAsync(altBlock1, new[] { altBlock1 });

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(0, capturedEvent.ForkHeight);
        Assert.Equal(1, capturedEvent.OldTipHeight);
        Assert.Equal(1, capturedEvent.NewTipHeight);
        Assert.Equal(1, capturedEvent.RevertedBlockCount);
        Assert.Equal(1, capturedEvent.AppliedBlockCount);
    }

    #endregion

    #region Helper Methods

    private static Block CreateGenesisBlock(long difficulty = 1000)
    {
        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: new byte[32], // Genesis has no parent
            height: 0,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            difficulty: difficulty,
            epoch: 0,
            challenge: RandomNumberGenerator.GetBytes(32),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32),
            txRoot: new byte[32],
            minerId: RandomNumberGenerator.GetBytes(33),
            signature: new byte[64]);

        var plotMetadata = BlockPlotMetadata.Create(
            leafCount: 1024,
            plotId: RandomNumberGenerator.GetBytes(32),
            plotHeaderHash: RandomNumberGenerator.GetBytes(32),
            version: 1);

        var body = new BlockBody(
            Array.Empty<Transaction>(),
            new BlockProof(
                RandomNumberGenerator.GetBytes(32),
                0,
                Array.Empty<byte[]>(),
                Array.Empty<bool>(),
                plotMetadata));

        return new Block(header, body);
    }

    private static Block CreateBlock(Block parent, long difficulty = 1000)
    {
        var parentHash = parent.Header.ComputeHash();
        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: parentHash,
            height: parent.Header.Height + 1,
            timestamp: parent.Header.Timestamp + 600, // 10 minutes later
            difficulty: difficulty,
            epoch: parent.Header.Epoch,
            challenge: parent.Header.Challenge.ToArray(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32),
            txRoot: new byte[32],
            minerId: RandomNumberGenerator.GetBytes(33),
            signature: new byte[64]);

        var plotMetadata = BlockPlotMetadata.Create(
            leafCount: 1024,
            plotId: RandomNumberGenerator.GetBytes(32),
            plotHeaderHash: RandomNumberGenerator.GetBytes(32),
            version: 1);

        var body = new BlockBody(
            Array.Empty<Transaction>(),
            new BlockProof(
                RandomNumberGenerator.GetBytes(32),
                0,
                Array.Empty<byte[]>(),
                Array.Empty<bool>(),
                plotMetadata));

        return new Block(header, body);
    }

    #endregion
}
