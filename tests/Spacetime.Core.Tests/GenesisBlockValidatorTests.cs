using NSubstitute;
using System.Security.Cryptography;

namespace Spacetime.Core.Tests;

public class GenesisBlockValidatorTests
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

    private static async Task<Block> CreateValidGenesisBlock(GenesisConfig? config = null)
    {
        config ??= CreateValidConfig();
        var signer = CreateMockSigner();
        var generator = new GenesisBlockGenerator(signer);
        return await generator.GenerateGenesisBlockAsync(config);
    }

    [Fact]
    public async Task ValidateGenesisBlockAsync_WithNullBlock_ThrowsArgumentNullException()
    {
        // Arrange
        var validator = new GenesisBlockValidator();
        var config = CreateValidConfig();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await validator.ValidateGenesisBlockAsync(null!, config));
    }

    [Fact]
    public async Task ValidateGenesisBlockAsync_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var validator = new GenesisBlockValidator();
        var block = await CreateValidGenesisBlock();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await validator.ValidateGenesisBlockAsync(block, null!));
    }

    [Fact]
    public async Task ValidateGenesisBlockAsync_WithValidGenesisBlock_ReturnsTrue()
    {
        // Arrange
        var config = CreateValidConfig();
        var block = await CreateValidGenesisBlock(config);
        var validator = new GenesisBlockValidator();

        // Act
        var isValid = await validator.ValidateGenesisBlockAsync(block, config);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateGenesisBlockAsync_WithNonZeroHeight_ReturnsFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        var block = await CreateValidGenesisBlock(config);
        
        // Create block with non-zero height
        var invalidHeader = new BlockHeader(
            block.Header.Version,
            block.Header.ParentHash,
            height: 1, // Non-zero height
            block.Header.Timestamp,
            block.Header.Difficulty,
            block.Header.Epoch,
            block.Header.Challenge,
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            block.Header.Signature);
        
        var invalidBlock = new Block(invalidHeader, block.Body);
        var validator = new GenesisBlockValidator();

        // Act
        var isValid = await validator.ValidateGenesisBlockAsync(invalidBlock, config);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateGenesisBlockAsync_WithNonZeroParentHash_ReturnsFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        var block = await CreateValidGenesisBlock(config);
        
        // Create block with non-zero parent hash
        var nonZeroParentHash = RandomNumberGenerator.GetBytes(32);
        var invalidHeader = new BlockHeader(
            block.Header.Version,
            nonZeroParentHash, // Non-zero parent hash
            block.Header.Height,
            block.Header.Timestamp,
            block.Header.Difficulty,
            block.Header.Epoch,
            block.Header.Challenge,
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            block.Header.Signature);
        
        var invalidBlock = new Block(invalidHeader, block.Body);
        var validator = new GenesisBlockValidator();

        // Act
        var isValid = await validator.ValidateGenesisBlockAsync(invalidBlock, config);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateGenesisBlockAsync_WithWrongTimestamp_ReturnsFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        var block = await CreateValidGenesisBlock(config);
        
        // Create block with wrong timestamp
        var invalidHeader = new BlockHeader(
            block.Header.Version,
            block.Header.ParentHash,
            block.Header.Height,
            timestamp: 9999, // Wrong timestamp
            block.Header.Difficulty,
            block.Header.Epoch,
            block.Header.Challenge,
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            block.Header.Signature);
        
        var invalidBlock = new Block(invalidHeader, block.Body);
        var validator = new GenesisBlockValidator();

        // Act
        var isValid = await validator.ValidateGenesisBlockAsync(invalidBlock, config);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateGenesisBlockAsync_WithWrongDifficulty_ReturnsFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        var block = await CreateValidGenesisBlock(config);
        
        // Create block with wrong difficulty
        var invalidHeader = new BlockHeader(
            block.Header.Version,
            block.Header.ParentHash,
            block.Header.Height,
            block.Header.Timestamp,
            difficulty: 9999, // Wrong difficulty
            block.Header.Epoch,
            block.Header.Challenge,
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            block.Header.Signature);
        
        var invalidBlock = new Block(invalidHeader, block.Body);
        var validator = new GenesisBlockValidator();

        // Act
        var isValid = await validator.ValidateGenesisBlockAsync(invalidBlock, config);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateGenesisBlockAsync_WithWrongEpoch_ReturnsFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        var block = await CreateValidGenesisBlock(config);
        
        // Create block with wrong epoch
        var invalidHeader = new BlockHeader(
            block.Header.Version,
            block.Header.ParentHash,
            block.Header.Height,
            block.Header.Timestamp,
            block.Header.Difficulty,
            epoch: 99, // Wrong epoch
            block.Header.Challenge,
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            block.Header.Signature);
        
        var invalidBlock = new Block(invalidHeader, block.Body);
        var validator = new GenesisBlockValidator();

        // Act
        var isValid = await validator.ValidateGenesisBlockAsync(invalidBlock, config);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateGenesisBlockAsync_WithWrongChallenge_ReturnsFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        var block = await CreateValidGenesisBlock(config);
        
        // Create block with wrong challenge
        var wrongChallenge = RandomNumberGenerator.GetBytes(32);
        var invalidHeader = new BlockHeader(
            block.Header.Version,
            block.Header.ParentHash,
            block.Header.Height,
            block.Header.Timestamp,
            block.Header.Difficulty,
            block.Header.Epoch,
            wrongChallenge, // Wrong challenge
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            block.Header.Signature);
        
        var invalidBlock = new Block(invalidHeader, block.Body);
        var validator = new GenesisBlockValidator();

        // Act
        var isValid = await validator.ValidateGenesisBlockAsync(invalidBlock, config);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateGenesisBlockAsync_WithUnsignedBlock_ReturnsFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        var block = await CreateValidGenesisBlock(config);
        
        // Create unsigned block
        var unsignedHeader = new BlockHeader(
            block.Header.Version,
            block.Header.ParentHash,
            block.Header.Height,
            block.Header.Timestamp,
            block.Header.Difficulty,
            block.Header.Epoch,
            block.Header.Challenge,
            block.Header.PlotRoot,
            block.Header.ProofScore,
            block.Header.TxRoot,
            block.Header.MinerId,
            signature: []); // Empty signature
        
        var unsignedBlock = new Block(unsignedHeader, block.Body);
        var validator = new GenesisBlockValidator();

        // Act
        var isValid = await validator.ValidateGenesisBlockAsync(unsignedBlock, config);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateGenesisBlockAsync_WithInvalidConfig_ReturnsFalse()
    {
        // Arrange
        var validConfig = CreateValidConfig();
        var block = await CreateValidGenesisBlock(validConfig);
        
        var invalidConfig = new GenesisConfig
        {
            NetworkId = "", // Invalid
            InitialTimestamp = 1000,
            InitialDifficulty = 5000,
            InitialEpoch = 0,
            EpochDurationSeconds = 30,
            TargetBlockTime = 30,
            PreminedAllocations = new Dictionary<string, long>()
        };
        
        var validator = new GenesisBlockValidator();

        // Act
        var isValid = await validator.ValidateGenesisBlockAsync(block, invalidConfig);

        // Assert
        Assert.False(isValid);
    }


}
