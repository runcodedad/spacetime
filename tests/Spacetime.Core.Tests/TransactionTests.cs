using System.Security.Cryptography;

namespace Spacetime.Core.Tests;

public class TransactionTests
{
    private static byte[] CreateValidPublicKey() => RandomNumberGenerator.GetBytes(33);
    
    private static Transaction CreateValidTransaction(bool signed = true)
    {
        return new Transaction(
            sender: CreateValidPublicKey(),
            recipient: CreateValidPublicKey(),
            amount: 1000,
            nonce: 5,
            fee: 10,
            signature: signed ? RandomNumberGenerator.GetBytes(64) : Array.Empty<byte>());
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesTransaction()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var amount = 1000L;
        var nonce = 5L;
        var fee = 10L;
        var signature = RandomNumberGenerator.GetBytes(64);

        // Act
        var transaction = new Transaction(sender, recipient, amount, nonce, fee, signature);

        // Assert
        Assert.Equal(Transaction.CurrentVersion, transaction.Version);
        Assert.Equal(sender, transaction.Sender.ToArray());
        Assert.Equal(recipient, transaction.Recipient.ToArray());
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal(nonce, transaction.Nonce);
        Assert.Equal(fee, transaction.Fee);
        Assert.Equal(signature, transaction.Signature.ToArray());
    }

    [Fact]
    public void Constructor_WithExplicitVersion_UsesSpecifiedVersion()
    {
        // Arrange
        byte version = 2;
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();

        // Act
        var transaction = new Transaction(version, sender, recipient, 1000, 5, 10, Array.Empty<byte>());

        // Assert
        Assert.Equal(version, transaction.Version);
    }

    [Fact]
    public void Constructor_WithInvalidSenderSize_ThrowsArgumentException()
    {
        // Arrange
        var invalidSender = RandomNumberGenerator.GetBytes(32); // Should be 33
        var recipient = CreateValidPublicKey();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Transaction(invalidSender, recipient, 1000, 5, 10, Array.Empty<byte>()));
        Assert.Contains("Sender", ex.Message);
    }

    [Fact]
    public void Constructor_WithInvalidRecipientSize_ThrowsArgumentException()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var invalidRecipient = RandomNumberGenerator.GetBytes(32); // Should be 33

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Transaction(sender, invalidRecipient, 1000, 5, 10, Array.Empty<byte>()));
        Assert.Contains("Recipient", ex.Message);
    }

    [Fact]
    public void Constructor_WithZeroAmount_ThrowsArgumentException()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Transaction(sender, recipient, 0, 5, 10, Array.Empty<byte>()));
        Assert.Contains("Amount", ex.Message);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Transaction(sender, recipient, -100, 5, 10, Array.Empty<byte>()));
        Assert.Contains("Amount", ex.Message);
    }

    [Fact]
    public void Constructor_WithNegativeNonce_ThrowsArgumentException()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Transaction(sender, recipient, 1000, -1, 10, Array.Empty<byte>()));
        Assert.Contains("Nonce", ex.Message);
    }

    [Fact]
    public void Constructor_WithNegativeFee_ThrowsArgumentException()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Transaction(sender, recipient, 1000, 5, -1, Array.Empty<byte>()));
        Assert.Contains("Fee", ex.Message);
    }

    [Fact]
    public void Constructor_WithInvalidSignatureSize_ThrowsArgumentException()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var invalidSignature = RandomNumberGenerator.GetBytes(63); // Should be 0 or 64

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Transaction(sender, recipient, 1000, 5, 10, invalidSignature));
        Assert.Contains("Signature", ex.Message);
    }

    [Fact]
    public void IsSigned_WithSignature_ReturnsTrue()
    {
        // Arrange
        var transaction = CreateValidTransaction(signed: true);

        // Act & Assert
        Assert.True(transaction.IsSigned());
    }

    [Fact]
    public void IsSigned_WithoutSignature_ReturnsFalse()
    {
        // Arrange
        var transaction = CreateValidTransaction(signed: false);

        // Act & Assert
        Assert.False(transaction.IsSigned());
    }

    [Fact]
    public void SetSignature_WithValidSignature_SetsSignature()
    {
        // Arrange
        var transaction = CreateValidTransaction(signed: false);
        var signature = RandomNumberGenerator.GetBytes(64);

        // Act
        transaction.SetSignature(signature);

        // Assert
        Assert.True(transaction.IsSigned());
        Assert.Equal(signature, transaction.Signature.ToArray());
    }

    [Fact]
    public void SetSignature_WithInvalidSize_ThrowsArgumentException()
    {
        // Arrange
        var transaction = CreateValidTransaction(signed: false);
        var invalidSignature = RandomNumberGenerator.GetBytes(63);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => transaction.SetSignature(invalidSignature));
        Assert.Contains("Signature", ex.Message);
    }

    [Fact]
    public void ComputeHash_ReturnsConsistentHash()
    {
        // Arrange
        var transaction = CreateValidTransaction();

        // Act
        var hash1 = transaction.ComputeHash();
        var hash2 = transaction.ComputeHash();

        // Assert
        Assert.Equal(32, hash1.Length);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentTransactions_ProduceDifferentHashes()
    {
        // Arrange
        var tx1 = CreateValidTransaction();
        var tx2 = CreateValidTransaction();

        // Act
        var hash1 = tx1.ComputeHash();
        var hash2 = tx2.ComputeHash();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_IgnoresSignature()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();
        var tx1 = new Transaction(sender, recipient, 1000, 5, 10, RandomNumberGenerator.GetBytes(64));
        var tx2 = new Transaction(sender, recipient, 1000, 5, 10, RandomNumberGenerator.GetBytes(64));

        // Act
        var hash1 = tx1.ComputeHash();
        var hash2 = tx2.ComputeHash();

        // Assert - hashes should be equal despite different signatures
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void SerializeWithoutSignature_ReturnsExpectedSize()
    {
        // Arrange
        var transaction = CreateValidTransaction();

        // Act
        var data = transaction.SerializeWithoutSignature();

        // Assert
        var expectedSize = sizeof(byte) + 33 + 33 + sizeof(long) * 3; // version + sender + recipient + amount + nonce + fee
        Assert.Equal(expectedSize, data.Length);
    }

    [Fact]
    public void Serialize_WithSignedTransaction_ReturnsExpectedSize()
    {
        // Arrange
        var transaction = CreateValidTransaction(signed: true);

        // Act
        var data = transaction.Serialize();

        // Assert
        Assert.Equal(Transaction.SerializedSize, data.Length);
    }

    [Fact]
    public void Serialize_WithUnsignedTransaction_ThrowsInvalidOperationException()
    {
        // Arrange
        var transaction = CreateValidTransaction(signed: false);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => transaction.Serialize());
    }

    [Fact]
    public void Serialize_WithNullWriter_ThrowsArgumentNullException()
    {
        // Arrange
        var transaction = CreateValidTransaction(signed: true);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => transaction.Serialize(null!));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var original = CreateValidTransaction(signed: true);

        // Act
        var serialized = original.Serialize();
        var deserialized = Transaction.Deserialize(serialized);

        // Assert
        Assert.Equal(original.Version, deserialized.Version);
        Assert.Equal(original.Sender.ToArray(), deserialized.Sender.ToArray());
        Assert.Equal(original.Recipient.ToArray(), deserialized.Recipient.ToArray());
        Assert.Equal(original.Amount, deserialized.Amount);
        Assert.Equal(original.Nonce, deserialized.Nonce);
        Assert.Equal(original.Fee, deserialized.Fee);
        Assert.Equal(original.Signature.ToArray(), deserialized.Signature.ToArray());
    }

    [Fact]
    public void SerializeDeserialize_WithBinaryReader_PreservesData()
    {
        // Arrange
        var original = CreateValidTransaction(signed: true);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Act
        original.Serialize(writer);
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var deserialized = Transaction.Deserialize(reader);

        // Assert
        Assert.Equal(original.Amount, deserialized.Amount);
        Assert.Equal(original.Nonce, deserialized.Nonce);
        Assert.Equal(original.Fee, deserialized.Fee);
    }

    [Fact]
    public void Deserialize_ByteArray_WithNullData_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Transaction.Deserialize((byte[])null!));
    }

    [Fact]
    public void Deserialize_BinaryReader_WithNullReader_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Transaction.Deserialize((BinaryReader)null!));
    }

    [Fact]
    public void ValidateBasicRules_WithValidSignedTransaction_ReturnsTrue()
    {
        // Arrange
        var transaction = CreateValidTransaction(signed: true);

        // Act
        var isValid = transaction.ValidateBasicRules();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateBasicRules_WithUnsignedTransaction_ReturnsFalse()
    {
        // Arrange
        var transaction = CreateValidTransaction(signed: false);

        // Act
        var isValid = transaction.ValidateBasicRules();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateBasicRules_WithSameSenderAndRecipient_ReturnsFalse()
    {
        // Arrange
        var sameKey = CreateValidPublicKey();
        var transaction = new Transaction(
            sender: sameKey,
            recipient: sameKey,
            amount: 1000,
            nonce: 5,
            fee: 10,
            signature: RandomNumberGenerator.GetBytes(64));

        // Act
        var isValid = transaction.ValidateBasicRules();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void SerializedSize_ReturnsCorrectValue()
    {
        // Arrange & Act
        var size = Transaction.SerializedSize;
        var expected = sizeof(byte) + 33 + 33 + sizeof(long) * 3 + 64;

        // Assert
        Assert.Equal(expected, size);
    }

    [Fact]
    public void Constructor_WithZeroFee_IsValid()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();

        // Act
        var transaction = new Transaction(sender, recipient, 1000, 5, 0, Array.Empty<byte>());

        // Assert
        Assert.Equal(0, transaction.Fee);
    }

    [Fact]
    public void Constructor_WithZeroNonce_IsValid()
    {
        // Arrange
        var sender = CreateValidPublicKey();
        var recipient = CreateValidPublicKey();

        // Act
        var transaction = new Transaction(sender, recipient, 1000, 0, 10, Array.Empty<byte>());

        // Assert
        Assert.Equal(0, transaction.Nonce);
    }
}
