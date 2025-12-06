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
    void IndexTransaction(
        ReadOnlyMemory<byte> txHash,
        ReadOnlyMemory<byte> blockHash,
        long blockHeight,
        int txIndex);

    /// <summary>
    /// Retrieves transaction location information by hash.
    /// </summary>
    /// <param name="txHash">The transaction hash.</param>
    /// <returns>Transaction location, or null if not found.</returns>
    TransactionLocation? GetTransactionLocation(ReadOnlyMemory<byte> txHash);

    /// <summary>
    /// Retrieves a transaction by hash.
    /// </summary>
    /// <param name="txHash">The transaction hash.</param>
    /// <returns>The transaction, or null if not found.</returns>
    Transaction? GetTransaction(ReadOnlyMemory<byte> txHash);
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
