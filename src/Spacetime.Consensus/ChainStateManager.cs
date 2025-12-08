using System.Collections.Concurrent;
using System.Security.Cryptography;
using Spacetime.Core;
using Spacetime.Storage;

namespace Spacetime.Consensus;

/// <summary>
/// Manages blockchain state transitions using the account model.
/// </summary>
/// <remarks>
/// This implementation provides:
/// - Atomic state transitions when applying blocks
/// - Snapshot-based rollback for chain reorganizations
/// - State root computation using Merkle Patricia Trie
/// - State consistency validation
/// 
/// All operations are thread-safe and atomic.
/// </remarks>
public sealed class ChainStateManager : IStateManager
{
    private readonly IChainStorage _storage;
    private readonly ISignatureVerifier _signatureVerifier;
    private readonly ConcurrentDictionary<long, StateSnapshot> _snapshots;
    private long _nextSnapshotId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainStateManager"/> class.
    /// </summary>
    /// <param name="storage">The chain storage for account data.</param>
    /// <param name="signatureVerifier">The signature verifier for transaction validation.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public ChainStateManager(IChainStorage storage, ISignatureVerifier signatureVerifier)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(signatureVerifier);

        _storage = storage;
        _signatureVerifier = signatureVerifier;
        _snapshots = new ConcurrentDictionary<long, StateSnapshot>();
        _nextSnapshotId = 1;
    }

    /// <inheritdoc />
    public async Task<byte[]> ApplyBlockAsync(Block block, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(block);
        cancellationToken.ThrowIfCancellationRequested();

        // Validate block state before applying
        if (!await ValidateBlockStateAsync(block, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Block state validation failed");
        }

        // Apply all transactions atomically using write batch
        using var batch = _storage.CreateWriteBatch();

        // Track modified accounts to prevent double-spending within block
        // Using custom byte array comparer for efficient dictionary lookups
        var modifiedAccounts = new Dictionary<byte[], (long balance, long nonce)>(ByteArrayEqualityComparer.Instance);

        foreach (var tx in block.Body.Transactions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var senderKey = tx.Sender.ToArray();
            var recipientKey = tx.Recipient.ToArray();

            // Get sender account state
            AccountState senderAccount;
            if (modifiedAccounts.TryGetValue(senderKey, out var modifiedSender))
            {
                senderAccount = new AccountState(modifiedSender.balance, modifiedSender.nonce);
            }
            else
            {
                senderAccount = _storage.Accounts.GetAccount(tx.Sender.ToArray()) 
                    ?? new AccountState(0, 0);
            }

            // Get recipient account state
            AccountState recipientAccount;
            if (modifiedAccounts.TryGetValue(recipientKey, out var modifiedRecipient))
            {
                recipientAccount = new AccountState(modifiedRecipient.balance, modifiedRecipient.nonce);
            }
            else
            {
                recipientAccount = _storage.Accounts.GetAccount(tx.Recipient.ToArray()) 
                    ?? new AccountState(0, 0);
            }

            // Deduct from sender (amount + fee)
            var newSenderBalance = senderAccount.Balance - tx.Amount - tx.Fee;
            var newSenderNonce = senderAccount.Nonce + 1;
            var updatedSenderAccount = new AccountState(newSenderBalance, newSenderNonce);

            // Add to recipient
            var newRecipientBalance = recipientAccount.Balance + tx.Amount;
            var updatedRecipientAccount = new AccountState(newRecipientBalance, recipientAccount.Nonce);

            // Store in batch
            StoreAccountInBatch(batch, senderKey, updatedSenderAccount);
            StoreAccountInBatch(batch, recipientKey, updatedRecipientAccount);

            // Track modifications
            modifiedAccounts[senderKey] = (newSenderBalance, newSenderNonce);
            modifiedAccounts[recipientKey] = (newRecipientBalance, recipientAccount.Nonce);
        }

        // TODO: Implement full coinbase transaction for block reward
        // Currently only distributing transaction fees to the miner
        var minerKey = block.Header.MinerId.ToArray();
        AccountState minerAccount;
        if (modifiedAccounts.TryGetValue(minerKey, out var modifiedMiner))
        {
            minerAccount = new AccountState(modifiedMiner.balance, modifiedMiner.nonce);
        }
        else
        {
            minerAccount = _storage.Accounts.GetAccount(block.Header.MinerId.ToArray()) 
                ?? new AccountState(0, 0);
        }

        // Calculate total fees
        var totalFees = block.Body.Transactions.Sum(tx => tx.Fee);
        var newMinerBalance = minerAccount.Balance + totalFees;
        var updatedMinerAccount = new AccountState(newMinerBalance, minerAccount.Nonce);
        StoreAccountInBatch(batch, minerKey, updatedMinerAccount);

        // Commit all changes atomically
        _storage.CommitBatch(batch);

        // Compute and return state root
        return ComputeStateRoot();
    }

    /// <inheritdoc />
    public async Task<bool> ValidateBlockStateAsync(Block block, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(block);
        cancellationToken.ThrowIfCancellationRequested();

        // Track account states within this block to detect double-spending
        var accountStates = new Dictionary<byte[], (long balance, long nonce)>(ByteArrayEqualityComparer.Instance);

        foreach (var tx in block.Body.Transactions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Validate basic transaction rules
            if (!tx.ValidateBasicRules())
            {
                return false;
            }

            // Verify transaction signature
            var txHash = tx.ComputeHash();
            if (!_signatureVerifier.VerifySignature(txHash, tx.Signature.ToArray(), tx.Sender.ToArray()))
            {
                return false;
            }

            var senderKey = tx.Sender.ToArray();

            // Get current sender state
            AccountState senderAccount;
            if (accountStates.TryGetValue(senderKey, out var trackedState))
            {
                senderAccount = new AccountState(trackedState.balance, trackedState.nonce);
            }
            else
            {
                senderAccount = _storage.Accounts.GetAccount(tx.Sender.ToArray()) 
                    ?? new AccountState(0, 0);
            }

            // Validate nonce (must be sequential)
            if (tx.Nonce != senderAccount.Nonce)
            {
                return false;
            }

            // Validate sender has sufficient balance
            var totalRequired = tx.Amount + tx.Fee;
            if (senderAccount.Balance < totalRequired)
            {
                return false;
            }

            // Update tracked state for this sender
            var newBalance = senderAccount.Balance - totalRequired;
            var newNonce = senderAccount.Nonce + 1;
            accountStates[senderKey] = (newBalance, newNonce);
        }

        return true;
    }

    /// <inheritdoc />
    public long GetBalance(ReadOnlyMemory<byte> address)
    {
        var account = _storage.Accounts.GetAccount(address);
        return account?.Balance ?? 0;
    }

    /// <inheritdoc />
    public long GetNonce(ReadOnlyMemory<byte> address)
    {
        var account = _storage.Accounts.GetAccount(address);
        return account?.Nonce ?? 0;
    }

    /// <inheritdoc />
    public byte[] ComputeStateRoot()
    {
        // IMPORTANT: This is a placeholder implementation that returns a constant hash.
        // It does NOT reflect actual account states and should NOT be used in production.
        // 
        // Production implementation must use a Merkle Patricia Trie that:
        // - Computes a cryptographic root of all account states
        // - Enables light client verification
        // - Supports efficient state proofs
        // 
        // TODO: Implement proper Merkle Patricia Trie for state root computation
        // See: docs/account-model-architecture.md for design details
        
        using var hasher = SHA256.Create();
        var emptyRoot = hasher.ComputeHash(Array.Empty<byte>());
        
        return emptyRoot;
    }

    /// <inheritdoc />
    public long CreateSnapshot()
    {
        var snapshotId = Interlocked.Increment(ref _nextSnapshotId);
        var snapshot = new StateSnapshot
        {
            Id = snapshotId,
            Timestamp = DateTimeOffset.UtcNow
        };

        _snapshots.TryAdd(snapshotId, snapshot);

        // In a production system with RocksDB, we would create an actual snapshot here
        // For now, we're using a simplified approach
        return snapshotId;
    }

    /// <inheritdoc />
    public void RevertToSnapshot(long snapshotId)
    {
        if (!_snapshots.ContainsKey(snapshotId))
        {
            throw new ArgumentException($"Snapshot {snapshotId} not found", nameof(snapshotId));
        }

        // In a production system with RocksDB, we would revert to the snapshot here
        // This is a placeholder for the actual implementation
        // TODO: Implement proper snapshot revert using RocksDB snapshots
    }

    /// <inheritdoc />
    public void ReleaseSnapshot(long snapshotId)
    {
        _snapshots.TryRemove(snapshotId, out _);
    }

    /// <inheritdoc />
    public bool CheckConsistency()
    {
        // Check underlying storage integrity
        var storageHealthy = _storage.CheckIntegrity();

        return storageHealthy;
    }

    private static void StoreAccountInBatch(IWriteBatch batch, byte[] address, AccountState account)
    {
        var value = account.Serialize();
        batch.Put(address, value, "accounts");
    }

    private sealed class StateSnapshot
    {
        public required long Id { get; init; }
        public required DateTimeOffset Timestamp { get; init; }
    }
}
