namespace Spacetime.Consensus;

/// <summary>
/// Manages blockchain state transitions based on the account model.
/// </summary>
/// <remarks>
/// The state manager is responsible for:
/// - Applying blocks to update account balances and nonces
/// - Rolling back state changes during chain reorganizations
/// - Computing state roots for light client verification
/// - Validating state consistency
/// 
/// State transitions are atomic - either all changes from a block are applied,
/// or none are. This ensures the blockchain state remains consistent.
/// </remarks>
public interface IStateManager
{
    /// <summary>
    /// Applies a block to the current state, updating account balances and nonces.
    /// </summary>
    /// <param name="block">The block to apply.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The state root hash after applying the block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when block is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when state transition fails.</exception>
    Task<byte[]> ApplyBlockAsync(Core.Block block, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a block can be applied to the current state.
    /// </summary>
    /// <param name="block">The block to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the block is valid for the current state; otherwise, false.</returns>
    /// <remarks>
    /// This checks:
    /// - All transaction signatures are valid
    /// - Sender accounts have sufficient balance (amount + fee)
    /// - Transaction nonces are correct (sequential)
    /// - No double-spending within the block
    /// </remarks>
    Task<bool> ValidateBlockStateAsync(Core.Block block, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the balance of an account.
    /// </summary>
    /// <param name="address">The account address (33-byte public key).</param>
    /// <returns>The account balance, or 0 if the account doesn't exist.</returns>
    long GetBalance(ReadOnlyMemory<byte> address);

    /// <summary>
    /// Gets the nonce of an account.
    /// </summary>
    /// <param name="address">The account address (33-byte public key).</param>
    /// <returns>The account nonce, or 0 if the account doesn't exist.</returns>
    long GetNonce(ReadOnlyMemory<byte> address);

    /// <summary>
    /// Computes the current state root hash.
    /// </summary>
    /// <returns>The 32-byte state root hash.</returns>
    /// <remarks>
    /// The state root is a Merkle root of all account states.
    /// It allows light clients to verify state without downloading all accounts.
    /// </remarks>
    byte[] ComputeStateRoot();

    /// <summary>
    /// Creates a snapshot of the current state for potential rollback.
    /// </summary>
    /// <returns>A snapshot identifier that can be used to revert state.</returns>
    long CreateSnapshot();

    /// <summary>
    /// Reverts state to a previous snapshot.
    /// </summary>
    /// <param name="snapshotId">The snapshot identifier to revert to.</param>
    /// <exception cref="ArgumentException">Thrown when snapshot ID is invalid.</exception>
    void RevertToSnapshot(long snapshotId);

    /// <summary>
    /// Releases a snapshot, freeing resources.
    /// </summary>
    /// <param name="snapshotId">The snapshot identifier to release.</param>
    void ReleaseSnapshot(long snapshotId);

    /// <summary>
    /// Checks state consistency and detects corruption.
    /// </summary>
    /// <returns>True if state is consistent; otherwise, false.</returns>
    bool CheckConsistency();
}
