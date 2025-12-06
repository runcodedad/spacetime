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
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StoreAccountAsync(ReadOnlyMemory<byte> address, AccountState account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an account state by address.
    /// </summary>
    /// <param name="address">The account address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account state, or null if not found.</returns>
    Task<AccountState?> GetAccountAsync(ReadOnlyMemory<byte> address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an account exists.
    /// </summary>
    /// <param name="address">The account address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the account exists, false otherwise.</returns>
    Task<bool> ExistsAsync(ReadOnlyMemory<byte> address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an account.
    /// </summary>
    /// <param name="address">The account address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAccountAsync(ReadOnlyMemory<byte> address, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the state of an account in the blockchain.
/// </summary>
/// <param name="Balance">The account balance.</param>
/// <param name="Nonce">The account nonce for replay protection.</param>
public record AccountState(long Balance, long Nonce);
