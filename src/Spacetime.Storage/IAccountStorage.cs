namespace Spacetime.Storage;

/// <summary>
/// Interface for storing and retrieving account state.
/// </summary>
/// <remarks>
/// Implements the account model for blockchain state management.
/// Each account has a balance and nonce for replay protection.
/// </remarks>
public interface IAccountStorage
{
    /// <summary>
    /// Stores an account state.
    /// </summary>
    /// <param name="address">The account address.</param>
    /// <param name="account">The account state.</param>
    void StoreAccount(ReadOnlyMemory<byte> address, AccountState account);

    /// <summary>
    /// Retrieves an account state by address.
    /// </summary>
    /// <param name="address">The account address.</param>
    /// <returns>The account state, or null if not found.</returns>
    AccountState? GetAccount(ReadOnlyMemory<byte> address);

    /// <summary>
    /// Checks if an account exists.
    /// </summary>
    /// <param name="address">The account address.</param>
    /// <returns>True if the account exists, false otherwise.</returns>
    bool Exists(ReadOnlyMemory<byte> address);

    /// <summary>
    /// Deletes an account.
    /// </summary>
    /// <param name="address">The account address.</param>
    void DeleteAccount(ReadOnlyMemory<byte> address);
}

/// <summary>
/// Represents the state of an account in the blockchain.
/// </summary>
/// <param name="Balance">The account balance.</param>
/// <param name="Nonce">The account nonce for replay protection.</param>
public record AccountState(long Balance, long Nonce);
