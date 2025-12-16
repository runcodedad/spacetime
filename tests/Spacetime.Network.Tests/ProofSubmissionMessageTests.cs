namespace Spacetime.Network.Tests;

public class ProofSubmissionMessageTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesProofSubmissionMessage()
    {
        // Arrange
        var proofData = new byte[500];
        Array.Fill(proofData, (byte)0xAA);
        var minerId = new byte[33];
        Array.Fill(minerId, (byte)0xBB);

        // Act
        var message = new ProofSubmissionMessage(proofData, minerId, 100);

        // Assert
        Assert.Equal(500, message.ProofData.Length);
        Assert.Equal(33, message.MinerId.Length);
        Assert.Equal(100, message.BlockHeight);
    }

    [Fact]
    public void Constructor_WithEmptyProofData_ThrowsArgumentException()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();
        var minerId = new byte[33];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ProofSubmissionMessage(emptyData, minerId, 0));
    }

    [Fact]
    public void Constructor_WithTooLargeProofData_ThrowsArgumentException()
    {
        // Arrange
        var largeData = new byte[ProofSubmissionMessage.MaxProofSize + 1];
        var minerId = new byte[33];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ProofSubmissionMessage(largeData, minerId, 0));
    }

    [Fact]
    public void Constructor_WithInvalidMinerId_ThrowsArgumentException()
    {
        // Arrange
        var proofData = new byte[500];
        var invalidMinerId = new byte[32]; // Should be 33

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ProofSubmissionMessage(proofData, invalidMinerId, 0));
    }

    [Fact]
    public void Constructor_WithNegativeBlockHeight_ThrowsArgumentException()
    {
        // Arrange
        var proofData = new byte[500];
        var minerId = new byte[33];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ProofSubmissionMessage(proofData, minerId, -1));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var proofData = new byte[500];
        Array.Fill(proofData, (byte)0xAA);
        var minerId = new byte[33];
        Array.Fill(minerId, (byte)0xBB);
        var original = new ProofSubmissionMessage(proofData, minerId, 100);

        // Act
        var serialized = original.Payload;
        var deserialized = ProofSubmissionMessage.Deserialize(serialized);

        // Assert
        Assert.True(original.ProofData.Span.SequenceEqual(deserialized.ProofData.Span));
        Assert.True(original.MinerId.Span.SequenceEqual(deserialized.MinerId.Span));
        Assert.Equal(original.BlockHeight, deserialized.BlockHeight);
    }

    [Fact]
    public void Deserialize_WithInvalidProofLength_ThrowsInvalidDataException()
    {
        // Arrange
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(100L); // block height
        writer.Write(new byte[33]); // miner id
        writer.Write(-1); // invalid proof length

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => ProofSubmissionMessage.Deserialize(ms.ToArray()));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var proofData = new byte[500];
        var minerId = new byte[33];
        var message = new ProofSubmissionMessage(proofData, minerId, 100);

        // Act
        var result = message.ToString();

        // Assert
        Assert.Contains("100", result);
        Assert.Contains("500", result);
    }
}
