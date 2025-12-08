using NSubstitute;
using System.Security.Cryptography;
using Spacetime.Core;
using Spacetime.Storage;

namespace Spacetime.Consensus.IntegrationTests;

/// <summary>
/// Integration tests for ChainReorganizer with full chain scenarios.
/// </summary>
public class ChainReorganizerIntegrationTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly RocksDbChainStorage _storage;
    private readonly ISignatureVerifier _signatureVerifier;
    private readonly ChainStateManager _stateManager;
    private readonly ChainReorganizer _reorganizer;
    private readonly ReorgConfig _config;

    public ChainReorganizerIntegrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"spacetime_test_{Guid.NewGuid():N}");
        _storage = RocksDbChainStorage.Open(_testDbPath);
        _signatureVerifier = Substitute.For<ISignatureVerifier>();
        _stateManager = new ChainStateManager(_storage, _signatureVerifier);
        _config = new ReorgConfig { MaxReorgDepth = 100 };
        _reorganizer = new ChainReorganizer(_storage, _stateManager, _config);

        // Setup default signature verification to succeed
        _signatureVerifier.VerifySignature(
            Arg.Any<byte[]>(),
            Arg.Any<byte[]>(),
            Arg.Any<byte[]>())
            .ReturnsForAnyArgs(true);
    }

    public void Dispose()
    {
        _storage.Dispose();
        if (Directory.Exists(_testDbPath))
        {
            Directory.Delete(_testDbPath, recursive: true);
        }
    }

    [Fact]
    public async Task ChainReorg_WithSimpleFork_SuccessfullyReorganizes()
    {
        // Arrange
        // Build main chain: genesis -> block1 -> block2
        var genesisBlock = CreateGenesisBlock(difficulty: 1000);
        var mainBlock1 = CreateBlock(genesisBlock, difficulty: 1000);
        var mainBlock2 = CreateBlock(mainBlock1, difficulty: 1000);

        // Store and set as best chain
        _storage.Blocks.StoreBlock(genesisBlock);
        _storage.Blocks.StoreBlock(mainBlock1);
        _storage.Blocks.StoreBlock(mainBlock2);
        _storage.Metadata.SetBestBlockHash(mainBlock2.Header.ComputeHash());
        _storage.Metadata.SetChainHeight(2);

        // Build alternative chain with higher difficulty: genesis -> block1 -> altBlock2
        var altBlock2 = CreateBlock(mainBlock1, difficulty: 1500);
        _storage.Blocks.StoreBlock(altBlock2);

        // Set up cumulative difficulties
        _storage.Metadata.SetCumulativeDifficulty(genesisBlock.Header.ComputeHash(), 1000);
        _storage.Metadata.SetCumulativeDifficulty(mainBlock1.Header.ComputeHash(), 2000);
        _storage.Metadata.SetCumulativeDifficulty(mainBlock2.Header.ComputeHash(), 3000);
        _storage.Metadata.SetCumulativeDifficulty(altBlock2.Header.ComputeHash(), 3500);

        // Act
        var result = await _reorganizer.TryReorganizeAsync(
            altBlock2,
            new[] { altBlock2 });

        // Assert
        Assert.True(result);

        // Verify new chain tip
        var newTip = _storage.Metadata.GetBestBlockHash();
        Assert.NotNull(newTip);
        Assert.Equal(altBlock2.Header.ComputeHash(), newTip.Value.ToArray());

        // Verify old block is orphaned
        Assert.True(_storage.Blocks.IsOrphaned(mainBlock2.Header.ComputeHash()));
        Assert.False(_storage.Blocks.IsOrphaned(mainBlock1.Header.ComputeHash()));
    }

    [Fact]
    public async Task ChainReorg_WithDeepFork_SuccessfullyReorganizes()
    {
        // Arrange
        // Build main chain: genesis -> block1 -> block2 -> block3
        var genesisBlock = CreateGenesisBlock(difficulty: 1000);
        var mainBlock1 = CreateBlock(genesisBlock, difficulty: 1000);
        var mainBlock2 = CreateBlock(mainBlock1, difficulty: 1000);
        var mainBlock3 = CreateBlock(mainBlock2, difficulty: 1000);

        _storage.Blocks.StoreBlock(genesisBlock);
        _storage.Blocks.StoreBlock(mainBlock1);
        _storage.Blocks.StoreBlock(mainBlock2);
        _storage.Blocks.StoreBlock(mainBlock3);
        _storage.Metadata.SetBestBlockHash(mainBlock3.Header.ComputeHash());
        _storage.Metadata.SetChainHeight(3);

        // Build alternative chain forking from genesis: genesis -> altBlock1 -> altBlock2 -> altBlock3
        var altBlock1 = CreateBlock(genesisBlock, difficulty: 1200);
        var altBlock2 = CreateBlock(altBlock1, difficulty: 1300);
        var altBlock3 = CreateBlock(altBlock2, difficulty: 1400);

        _storage.Blocks.StoreBlock(altBlock1);
        _storage.Blocks.StoreBlock(altBlock2);
        _storage.Blocks.StoreBlock(altBlock3);

        // Set up cumulative difficulties
        _storage.Metadata.SetCumulativeDifficulty(genesisBlock.Header.ComputeHash(), 1000);
        _storage.Metadata.SetCumulativeDifficulty(mainBlock1.Header.ComputeHash(), 2000);
        _storage.Metadata.SetCumulativeDifficulty(mainBlock2.Header.ComputeHash(), 3000);
        _storage.Metadata.SetCumulativeDifficulty(mainBlock3.Header.ComputeHash(), 4000);
        _storage.Metadata.SetCumulativeDifficulty(altBlock1.Header.ComputeHash(), 2200);
        _storage.Metadata.SetCumulativeDifficulty(altBlock2.Header.ComputeHash(), 3500);
        _storage.Metadata.SetCumulativeDifficulty(altBlock3.Header.ComputeHash(), 4900);

        // Act
        var result = await _reorganizer.TryReorganizeAsync(
            altBlock3,
            new[] { altBlock1, altBlock2, altBlock3 });

        // Assert
        Assert.True(result);

        // Verify all old blocks are orphaned
        Assert.True(_storage.Blocks.IsOrphaned(mainBlock1.Header.ComputeHash()));
        Assert.True(_storage.Blocks.IsOrphaned(mainBlock2.Header.ComputeHash()));
        Assert.True(_storage.Blocks.IsOrphaned(mainBlock3.Header.ComputeHash()));

        // Verify new chain tip
        var newTip = _storage.Metadata.GetBestBlockHash();
        Assert.NotNull(newTip);
        Assert.Equal(altBlock3.Header.ComputeHash(), newTip.Value.ToArray());
        Assert.Equal(3, _storage.Metadata.GetChainHeight());
    }

    [Fact]
    public async Task ChainReorg_WithTransactions_MarksOrphanedBlocks()
    {
        // Arrange
        // Note: Full state revert and reapply requires proper snapshot implementation
        // This test focuses on structural verification

        // Build main chain with empty transactions
        var genesisBlock = CreateGenesisBlock(difficulty: 1000);
        var mainBlock1 = CreateBlock(genesisBlock, difficulty: 1000);

        _storage.Blocks.StoreBlock(genesisBlock);
        _storage.Blocks.StoreBlock(mainBlock1);
        _storage.Metadata.SetBestBlockHash(mainBlock1.Header.ComputeHash());
        _storage.Metadata.SetChainHeight(1);

        // Build alternative chain with higher difficulty
        var altBlock1 = CreateBlock(genesisBlock, difficulty: 1500);
        _storage.Blocks.StoreBlock(altBlock1);

        // Set up cumulative difficulties
        _storage.Metadata.SetCumulativeDifficulty(genesisBlock.Header.ComputeHash(), 1000);
        _storage.Metadata.SetCumulativeDifficulty(mainBlock1.Header.ComputeHash(), 2000);
        _storage.Metadata.SetCumulativeDifficulty(altBlock1.Header.ComputeHash(), 2500);

        // Act - Perform reorg
        var result = await _reorganizer.TryReorganizeAsync(
            altBlock1,
            new[] { altBlock1 });

        // Assert
        Assert.True(result);

        // Verify old block is marked as orphaned
        Assert.True(_storage.Blocks.IsOrphaned(mainBlock1.Header.ComputeHash()));
        
        // Verify new chain tip
        var newTip = _storage.Metadata.GetBestBlockHash();
        Assert.NotNull(newTip);
        Assert.Equal(altBlock1.Header.ComputeHash(), newTip.Value.ToArray());
    }

    [Fact]
    public async Task ChainReorg_MultipleSequentialReorgs_HandlesCorrectly()
    {
        // Arrange & Act - Test multiple reorgs in sequence
        var genesisBlock = CreateGenesisBlock(difficulty: 1000);
        _storage.Blocks.StoreBlock(genesisBlock);
        _storage.Metadata.SetBestBlockHash(genesisBlock.Header.ComputeHash());
        _storage.Metadata.SetChainHeight(0);
        _storage.Metadata.SetCumulativeDifficulty(genesisBlock.Header.ComputeHash(), 1000);

        // First chain
        var block1a = CreateBlock(genesisBlock, difficulty: 1000);
        _storage.Blocks.StoreBlock(block1a);
        _storage.Metadata.SetBestBlockHash(block1a.Header.ComputeHash());
        _storage.Metadata.SetChainHeight(1);
        _storage.Metadata.SetCumulativeDifficulty(block1a.Header.ComputeHash(), 2000);

        // First reorg - replace block1a with block1b (higher difficulty)
        var block1b = CreateBlock(genesisBlock, difficulty: 1500);
        _storage.Blocks.StoreBlock(block1b);
        _storage.Metadata.SetCumulativeDifficulty(block1b.Header.ComputeHash(), 2500);

        var result1 = await _reorganizer.TryReorganizeAsync(block1b, new[] { block1b });
        Assert.True(result1);
        Assert.True(_storage.Blocks.IsOrphaned(block1a.Header.ComputeHash()));

        // Second reorg - extend the current chain (block1b)
        var block2b = CreateBlock(block1b, difficulty: 1000);
        _storage.Blocks.StoreBlock(block2b);
        
        // Update chain metadata for current state
        _storage.Metadata.SetBestBlockHash(block1b.Header.ComputeHash());
        _storage.Metadata.SetChainHeight(1);
        _storage.Metadata.SetCumulativeDifficulty(block2b.Header.ComputeHash(), 3500);

        // Create an even better alternative
        var block2c = CreateBlock(block1b, difficulty: 1200);
        _storage.Blocks.StoreBlock(block2c);
        _storage.Metadata.SetCumulativeDifficulty(block2c.Header.ComputeHash(), 3700);

        var result2 = await _reorganizer.TryReorganizeAsync(block2c, new[] { block2c });
        Assert.True(result2);

        // Verify final chain tip
        var finalTip = _storage.Metadata.GetBestBlockHash();
        Assert.NotNull(finalTip);
        Assert.Equal(block2c.Header.ComputeHash(), finalTip.Value.ToArray());
    }

    [Fact]
    public async Task ChainReorg_EventEmission_NotifiesListeners()
    {
        // Arrange
        ChainReorgEvent? capturedEvent = null;
        _reorganizer.ChainReorganized += (sender, e) => capturedEvent = e;

        // Build initial chain
        var genesisBlock = CreateGenesisBlock(difficulty: 1000);
        var block1 = CreateBlock(genesisBlock, difficulty: 1000);

        _storage.Blocks.StoreBlock(genesisBlock);
        _storage.Blocks.StoreBlock(block1);
        _storage.Metadata.SetBestBlockHash(block1.Header.ComputeHash());
        _storage.Metadata.SetChainHeight(1);
        _storage.Metadata.SetCumulativeDifficulty(genesisBlock.Header.ComputeHash(), 1000);
        _storage.Metadata.SetCumulativeDifficulty(block1.Header.ComputeHash(), 2000);

        // Build alternative chain
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
        Assert.True(capturedEvent.Timestamp <= DateTimeOffset.UtcNow);
    }

    #region Helper Methods

    private static Block CreateGenesisBlock(long difficulty = 1000)
    {
        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: new byte[32],
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
            timestamp: parent.Header.Timestamp + 600,
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

    private static Block CreateBlockWithTransactions(
        Block parent,
        IReadOnlyList<Transaction> transactions,
        byte[] minerId,
        long difficulty = 1000)
    {
        var parentHash = parent.Header.ComputeHash();
        var header = new BlockHeader(
            version: BlockHeader.CurrentVersion,
            parentHash: parentHash,
            height: parent.Header.Height + 1,
            timestamp: parent.Header.Timestamp + 600,
            difficulty: difficulty,
            epoch: parent.Header.Epoch,
            challenge: parent.Header.Challenge.ToArray(),
            plotRoot: RandomNumberGenerator.GetBytes(32),
            proofScore: RandomNumberGenerator.GetBytes(32),
            txRoot: new byte[32],
            minerId: minerId,
            signature: new byte[64]);

        var plotMetadata = BlockPlotMetadata.Create(
            leafCount: 1024,
            plotId: RandomNumberGenerator.GetBytes(32),
            plotHeaderHash: RandomNumberGenerator.GetBytes(32),
            version: 1);

        var body = new BlockBody(
            transactions,
            new BlockProof(
                RandomNumberGenerator.GetBytes(32),
                0,
                Array.Empty<byte[]>(),
                Array.Empty<bool>(),
                plotMetadata));

        return new Block(header, body);
    }

    private static Transaction CreateTransaction(
        byte[] sender,
        byte[] recipient,
        long amount,
        long nonce,
        long fee)
    {
        return new Transaction(
            sender: sender,
            recipient: recipient,
            amount: amount,
            nonce: nonce,
            fee: fee,
            signature: new byte[64]);
    }

    #endregion
}
