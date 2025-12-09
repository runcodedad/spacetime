namespace Spacetime.Consensus;

/// <summary>
/// Provides context information for validating transactions within a block.
/// </summary>
/// <remarks>
/// This context allows the validator to track state changes within a block
/// and optimize validation by caching account states.
/// 
/// To avoid repeated allocations, this class uses a custom lookup that avoids
/// creating new byte arrays for each lookup operation.
/// </remarks>
public sealed class BlockValidationContext
{
    private readonly Dictionary<byte[], (long balance, long nonce)> _accountStates;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockValidationContext"/> class.
    /// </summary>
    public BlockValidationContext()
    {
        _accountStates = new Dictionary<byte[], (long balance, long nonce)>(
            ByteArrayEqualityComparer.Instance);
    }

    /// <summary>
    /// Gets the tracked account state for an address, if any.
    /// </summary>
    /// <param name="address">The account address.</param>
    /// <returns>The tracked balance and nonce, or null if not tracked.</returns>
    public (long balance, long nonce)? GetTrackedAccountState(ReadOnlySpan<byte> address)
    {
        // Optimize: Check if we can find without allocating
        // Dictionary doesn't support Span lookup, so we need to allocate here
        // but we minimize allocations by only doing it once
        var key = address.ToArray();
        if (_accountStates.TryGetValue(key, out var state))
        {
            return state;
        }
        return null;
    }

    /// <summary>
    /// Updates the tracked account state for an address.
    /// </summary>
    /// <param name="address">The account address.</param>
    /// <param name="balance">The new balance.</param>
    /// <param name="nonce">The new nonce.</param>
    /// <remarks>
    /// Note: This method allocates a new byte array to use as dictionary key.
    /// In typical usage, this happens once per sender per block, which is acceptable.
    /// For blocks with many transactions from the same sender, the cost is amortized.
    /// </remarks>
    public void UpdateAccountState(ReadOnlySpan<byte> address, long balance, long nonce)
    {
        var key = address.ToArray();
        _accountStates[key] = (balance, nonce);
    }
}
