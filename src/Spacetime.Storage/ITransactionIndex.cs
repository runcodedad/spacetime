using Spacetime.Core;

namespace Spacetime.Storage;

/// <summary>
/// Interface for indexing and retrieving transactions.
/// </summary>
public interface ITransactionIndex
{
    /// <summary>
    /// Indexes a transaction.
    /// </summary>
    /// <param name="txHash">The transaction hash.</param>
    /// <param name="blockHash">The hash of the block containing the transaction.</param>
    /// <param name="blockHeight">The height of the block containing the transaction.</param>
    /// <param name="txIndex">The index of the transaction within the block.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IndexTransactionAsync(
        ReadOnlyMemory<byte> txHash,
        ReadOnlyMemory<byte> blockHash,
        long blockHeight,
        int txIndex,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves transaction location information by hash.
    /// </summary>
    /// <param name="txHash">The transaction hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction location, or null if not found.</returns>
    Task<TransactionLocation?> GetTransactionLocationAsync(
        ReadOnlyMemory<byte> txHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a transaction by hash.
    /// </summary>
    /// <param name="txHash">The transaction hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transaction, or null if not found.</returns>
    Task<Transaction?> GetTransactionAsync(
        ReadOnlyMemory<byte> txHash,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the location of a transaction in the blockchain.
/// </summary>
/// <param name="BlockHash">Hash of the block containing the transaction.</param>
/// <param name="BlockHeight">Height of the block containing the transaction.</param>
/// <param name="TransactionIndex">Index of the transaction within the block.</param>
public record TransactionLocation(
    ReadOnlyMemory<byte> BlockHash,
    long BlockHeight,
    int TransactionIndex);
