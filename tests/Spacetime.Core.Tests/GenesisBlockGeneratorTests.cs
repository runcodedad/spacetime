using NSubstitute;
using System.Security.Cryptography;

namespace Spacetime.Core.Tests;

public class GenesisBlockGeneratorTests
{
    private static IBlockSigner CreateMockSigner()
    {
        var signer = Substitute.For<IBlockSigner>();
        signer.GetPublicKey().Returns(RandomNumberGenerator.GetBytes(33));
        signer.SignBlockHeaderAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(RandomNumberGenerator.GetBytes(64)));
        return signer;
    }

    private static GenesisConfig CreateValidConfig()
    {
        return new GenesisConfig
        {
            NetworkId = "test-network",
            InitialTimestamp = 1000,
            InitialDifficulty = 5000,
            InitialEpoch = 0,
            EpochDurationSeconds = 30,
            TargetBlockTime = 30,
            PreminedAllocations = new Dictionary<string, long>()
        };
    }

    [Fact]
    public void Constructor_WithNullSigner_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GenesisBlockGenerator(null!));
    }

    [Fact]
    public async Task GenerateGenesisBlockAsync_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var signer = CreateMockSigner();
        var generator = new GenesisBlockGenerator(signer);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await generator.GenerateGenesisBlockAsync(null!));
    }

    [Fact]
    public async Task GenerateGenesisBlockAsync_WithValidConfig_CreatesGenesisBlock()
    {
        // Arrange
        var signer = CreateMockSigner();
        var generator = new GenesisBlockGenerator(signer);
        var config = CreateValidConfig();

        // Act
        var block = await generator.GenerateGenesisBlockAsync(config);

        // Assert
        Assert.NotNull(block);
        Assert.NotNull(block.Header);
        Assert.NotNull(block.Body);
    }

    [Fact]
    public async Task GenerateGenesisBlockAsync_CreatesBlockWithHeightZero()
    {
        // Arrange
        var signer = CreateMockSigner();
        var generator = new GenesisBlockGenerator(signer);
        var config = CreateValidConfig();

        // Act
        var block = await generator.GenerateGenesisBlockAsync(config);

        // Assert
        Assert.Equal(0, block.Header.Height);
    }

    [Fact]
    public async Task GenerateGenesisBlockAsync_CreatesBlockWithZeroParentHash()
    {
        // Arrange
        var signer = CreateMockSigner();
        var generator = new GenesisBlockGenerator(signer);
        var config = CreateValidConfig();

        // Act
        var block = await generator.GenerateGenesisBlockAsync(config);

        // Assert
        var parentHash = block.Header.ParentHash.ToArray();
        Assert.All(parentHash, b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task GenerateGenesisBlockAsync_UsesConfigTimestamp()
    {
        // Arrange
        var signer = CreateMockSigner();
        var generator = new GenesisBlockGenerator(signer);
        var config = CreateValidConfig();

        // Act
        var block = await generator.GenerateGenesisBlockAsync(config);

        // Assert
        Assert.Equal(config.InitialTimestamp, block.Header.Timestamp);
    }

    [Fact]
    public async Task GenerateGenesisBlockAsync_UsesConfigDifficulty()
    {
        // Arrange
        var signer = CreateMockSigner();
        var generator = new GenesisBlockGenerator(signer);
        var config = CreateValidConfig();

        // Act
        var block = await generator.GenerateGenesisBlockAsync(config);

        // Assert
        Assert.Equal(config.InitialDifficulty, block.Header.Difficulty);
    }

    [Fact]
    public async Task GenerateGenesisBlockAsync_UsesConfigEpoch()
    {
        // Arrange
        var signer = CreateMockSigner();
        var generator = new GenesisBlockGenerator(signer);
        var config = CreateValidConfig();

        // Act
        var block = await generator.GenerateGenesisBlockAsync(config);

        // Assert
        Assert.Equal(config.InitialEpoch, block.Header.Epoch);
    }

    [Fact]
    public async Task GenerateGenesisBlockAsync_CreatesSignedBlock()
    {
        // Arrange
        var signer = CreateMockSigner();
        var generator = new GenesisBlockGenerator(signer);
        var config = CreateValidConfig();

        // Act
        var block = await generator.GenerateGenesisBlockAsync(config);

        // Assert
        Assert.True(block.Header.IsSigned());
        Assert.Equal(64, block.Header.Signature.Length);
    }

    [Fact]
    public async Task GenerateGenesisBlockAsync_CallsSignerToSignBlock()
    {
        // Arrange
        var signer = CreateMockSigner();
        var generator = new GenesisBlockGenerator(signer);
        var config = CreateValidConfig();

        // Act
        await generator.GenerateGenesisBlockAsync(config);

        // Assert
        await signer.Received(1).SignBlockHeaderAsync(
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateGenesisBlockAsync_UsesSignerPublicKey()
    {
        // Arrange
        var expectedPublicKey = RandomNumberGenerator.GetBytes(33);
        var signer = Substitute.For<IBlockSigner>();
        signer.GetPublicKey().Returns(expectedPublicKey);
        signer.SignBlockHeaderAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(RandomNumberGenerator.GetBytes(64)));

        var generator = new GenesisBlockGenerator(signer);
        var config = CreateValidConfig();

        // Act
        var block = await generator.GenerateGenesisBlockAsync(config);

        // Assert
        Assert.Equal(expectedPublicKey, block.Header.MinerId.ToArray());
    }

    [Fact]
    public async Task GenerateGenesisBlockAsync_WithInvalidConfig_ThrowsException()
    {
        // Arrange
        var signer = CreateMockSigner();
        var generator = new GenesisBlockGenerator(signer);
        var config = new GenesisConfig
        {
            NetworkId = "", // Invalid
            InitialTimestamp = 1000,
            InitialDifficulty = 5000,
            InitialEpoch = 0,
            EpochDurationSeconds = 30,
            TargetBlockTime = 30,
            PreminedAllocations = new Dictionary<string, long>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await generator.GenerateGenesisBlockAsync(config));
    }

    [Fact]
    public async Task GenerateGenesisBlockAsync_ComputesChallenge_FromNetworkId()
    {
        // Arrange
        var signer = CreateMockSigner();
        var generator = new GenesisBlockGenerator(signer);
        var config = CreateValidConfig();

        // Act
        var block = await generator.GenerateGenesisBlockAsync(config);

        // Assert - Challenge should be deterministic hash of network ID
        var expectedChallenge = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(config.NetworkId));
        Assert.Equal(expectedChallenge, block.Header.Challenge.ToArray());
    }

    [Fact]
    public async Task GenerateGenesisBlockAsync_RespectsCancellationToken()
    {
        // Arrange
        var signer = CreateMockSigner();
        var generator = new GenesisBlockGenerator(signer);
        var config = CreateValidConfig();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await generator.GenerateGenesisBlockAsync(config, cts.Token));
    }

    [Fact]
    public async Task GenerateGenesisBlockAsync_WithDifferentNetworkIds_CreatesDifferentChallenges()
    {
        // Arrange
        var signer = CreateMockSigner();
        var generator = new GenesisBlockGenerator(signer);
        
        var config1 = CreateValidConfig();
        config1 = config1 with { NetworkId = "network-1" };
        
        var config2 = CreateValidConfig();
        config2 = config2 with { NetworkId = "network-2" };

        // Act
        var block1 = await generator.GenerateGenesisBlockAsync(config1);
        var block2 = await generator.GenerateGenesisBlockAsync(config2);

        // Assert
        Assert.NotEqual(block1.Header.Challenge.ToArray(), block2.Header.Challenge.ToArray());
    }
}
