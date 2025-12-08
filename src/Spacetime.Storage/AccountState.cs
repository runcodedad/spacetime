namespace Spacetime.Storage;

/// <summary>
/// Represents the state of an account in the blockchain.
/// </summary>
/// <param name="Balance">The account balance.</param>
/// <param name="Nonce">The account nonce for replay protection.</param>
public record AccountState(long Balance, long Nonce)
{
    /// <summary>
    /// Serializes the account state to a byte array.
    /// </summary>
    /// <returns>16-byte array (8 bytes balance + 8 bytes nonce).</returns>
    public byte[] Serialize()
    {
        var buffer = new byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(0), Balance);
        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(8), Nonce);
        return buffer;
    }

    /// <summary>
    /// Deserializes an account state from a byte array.
    /// </summary>
    /// <param name="data">The 16-byte serialized account data.</param>
    /// <returns>A new AccountState instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when data length is invalid.</exception>
    public static AccountState Deserialize(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        
        if (data.Length != 16)
        {
            throw new InvalidOperationException("Invalid account data.");
        }

        var balance = System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(data.AsSpan(0));
        var nonce = System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(data.AsSpan(8));

        return new AccountState(balance, nonce);
    }
}
