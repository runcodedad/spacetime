using Spacetime.Core;

namespace Spacetime.Consensus;

/// <summary>
/// In-memory transaction pool that stores pending transactions prioritized by fee.
/// </summary>
/// <remarks>
/// The mempool maintains transactions in priority order (highest fee first) for efficient
/// block building. It provides thread-safe operations for adding, removing, and querying
/// transactions. When the pool reaches capacity, the lowest-fee transaction is evicted
/// to make room for higher-fee transactions.
/// 
/// <example>
/// Using the mempool:
/// <code>
/// var config = MempoolConfig.Default;
/// var validator = new TransactionValidator(...);
/// var mempool = new Mempool(validator, config);
/// 
/// // Add a transaction
/// var added = await mempool.AddTransactionAsync(transaction);
/// if (added)
/// {
///     Console.WriteLine("Transaction added to mempool");
/// }
/// 
/// // Get transactions for a block
/// var transactions = await mempool.GetPendingTransactionsAsync(1000);
/// 
/// // Remove transactions after inclusion in a block
/// var hashes = transactions.Select(tx => tx.ComputeHash()).ToList();
/// await mempool.RemoveTransactionsAsync(hashes);
/// </code>
/// </example>
/// </remarks>
public sealed class Mempool : IMempool
{
    private readonly ITransactionValidator _validator;
    private readonly MempoolConfig _config;
    private readonly object _lock = new();
    
    // Store transactions by hash for quick lookup
    private readonly Dictionary<string, Transaction> _transactions = new();
    
    // Store transactions sorted by fee (descending) for priority ordering
    private readonly SortedSet<TransactionWithHash> _priorityQueue;

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _transactions.Count;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Mempool"/> class.
    /// </summary>
    /// <param name="validator">The transaction validator for validating incoming transactions.</param>
    /// <param name="config">The mempool configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when validator or config is null.</exception>
    public Mempool(ITransactionValidator validator, MempoolConfig config)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(config);

        _validator = validator;
        _config = config;
        _priorityQueue = new SortedSet<TransactionWithHash>(new TransactionPriorityComparer());
    }

    /// <inheritdoc />
    public bool AddTransaction(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        cancellationToken.ThrowIfCancellationRequested();

        // Validate transaction
        var validationResult = _validator.ValidateTransaction(transaction, cancellationToken);
        if (!validationResult.IsValid)
        {
            return false;
        }

        // Check minimum fee requirement
        if (transaction.Fee < _config.MinimumFee)
        {
            return false;
        }

        var txHash = transaction.ComputeHash();
        var txHashStr = Convert.ToHexString(txHash);

        lock (_lock)
        {
            // Check if transaction already exists
            if (_transactions.ContainsKey(txHashStr))
            {
                return false;
            }

            var txWithHash = new TransactionWithHash(transaction, txHash);

            // Check if mempool is full
            if (_transactions.Count >= _config.MaxTransactions)
            {
                // Get the lowest fee transaction (last element in descending order)
                // Note: Max returns the last element when using descending comparator
                var lowestFeeTx = _priorityQueue.Max;
                
                // Only evict if the new transaction has a higher fee
                if (lowestFeeTx != null && transaction.Fee > lowestFeeTx.Transaction.Fee)
                {
                    // Evict the lowest fee transaction
                    var lowestHashStr = Convert.ToHexString(lowestFeeTx.Hash);
                    _transactions.Remove(lowestHashStr);
                    _priorityQueue.Remove(lowestFeeTx);
                }
                else
                {
                    // Don't add transaction if it has lower or equal fee to the lowest in pool
                    return false;
                }
            }

            // Add transaction to the pool
            _transactions[txHashStr] = transaction;
            _priorityQueue.Add(txWithHash);
            return true;
        }
    }

    /// <inheritdoc />
    public int RemoveTransactions(
        IReadOnlyList<byte[]> transactionHashes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transactionHashes);
        cancellationToken.ThrowIfCancellationRequested();

        int removedCount = 0;

        lock (_lock)
        {
            foreach (var hash in transactionHashes)
            {
                var hashStr = Convert.ToHexString(hash);
                if (_transactions.TryGetValue(hashStr, out var transaction))
                {
                    _transactions.Remove(hashStr);
                    _priorityQueue.Remove(new TransactionWithHash(transaction, hash));
                    removedCount++;
                }
            }
        }

        return removedCount;
    }

    /// <inheritdoc />
    public bool ContainsTransaction(
        byte[] transactionHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transactionHash);
        cancellationToken.ThrowIfCancellationRequested();

        var hashStr = Convert.ToHexString(transactionHash);

        lock (_lock)
        {
            return _transactions.ContainsKey(hashStr);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<Transaction> GetPendingTransactions(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (maxCount <= 0)
        {
            return [];
        }

        lock (_lock)
        {
            var count = Math.Min(maxCount, Math.Min(_transactions.Count, _config.MaxTransactionsPerBlock));
            var transactions = _priorityQueue
                .Take(count)
                .Select(txh => txh.Transaction)
                .ToList();

            return transactions;
        }
    }

    /// <inheritdoc />
    public void Clear(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _transactions.Clear();
            _priorityQueue.Clear();
        }
    }

    /// <summary>
    /// Helper class to associate a transaction with its hash for efficient sorting.
    /// </summary>
    private sealed record TransactionWithHash(Transaction Transaction, byte[] Hash);

    /// <summary>
    /// Comparer that orders transactions by fee (descending), then by hash (for consistency).
    /// </summary>
    private sealed class TransactionPriorityComparer : IComparer<TransactionWithHash>
    {
        public int Compare(TransactionWithHash? x, TransactionWithHash? y)
        {
            if (x == null || y == null)
            {
                return 0;
            }

            // First, compare by fee (descending - higher fees first)
            var feeComparison = y.Transaction.Fee.CompareTo(x.Transaction.Fee);
            if (feeComparison != 0)
            {
                return feeComparison;
            }

            // If fees are equal, compare by hash for consistency
            // This ensures deterministic ordering and prevents duplicates
            return CompareByteArrays(x.Hash, y.Hash);
        }

        private static int CompareByteArrays(byte[] a, byte[] b)
        {
            var minLength = Math.Min(a.Length, b.Length);
            for (int i = 0; i < minLength; i++)
            {
                var comparison = a[i].CompareTo(b[i]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }
            return a.Length.CompareTo(b.Length);
        }
    }
}
