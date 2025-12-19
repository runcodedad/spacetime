using System.Net;

namespace Spacetime.Network;

/// <summary>
/// Manages a collection of peer addresses with metadata, persistence, and maintenance operations.
/// </summary>
public interface IPeerAddressBook
{
    /// <summary>
    /// Gets the total number of addresses in the address book.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets all peer addresses in the address book.
    /// </summary>
    IReadOnlyList<PeerAddress> GetAllAddresses();

    /// <summary>
    /// Adds a peer address to the address book.
    /// </summary>
    /// <param name="address">The peer address to add.</param>
    /// <returns>True if the address was added, false if it already exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="address"/> is null.</exception>
    bool AddAddress(PeerAddress address);

    /// <summary>
    /// Removes a peer address from the address book.
    /// </summary>
    /// <param name="endPoint">The endpoint to remove.</param>
    /// <returns>True if the address was removed, false if it was not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endPoint"/> is null.</exception>
    bool RemoveAddress(IPEndPoint endPoint);

    /// <summary>
    /// Gets a peer address by endpoint.
    /// </summary>
    /// <param name="endPoint">The endpoint to look up.</param>
    /// <returns>The peer address, or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endPoint"/> is null.</exception>
    PeerAddress? GetAddress(IPEndPoint endPoint);

    /// <summary>
    /// Updates the last seen timestamp for a peer address.
    /// </summary>
    /// <param name="endPoint">The endpoint to update.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endPoint"/> is null.</exception>
    void UpdateLastSeen(IPEndPoint endPoint);

    /// <summary>
    /// Records a successful connection to a peer address.
    /// </summary>
    /// <param name="endPoint">The endpoint that succeeded.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endPoint"/> is null.</exception>
    void RecordSuccess(IPEndPoint endPoint);

    /// <summary>
    /// Records a failed connection attempt to a peer address.
    /// </summary>
    /// <param name="endPoint">The endpoint that failed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endPoint"/> is null.</exception>
    void RecordFailure(IPEndPoint endPoint);

    /// <summary>
    /// Gets the best peer addresses to connect to based on quality and diversity.
    /// </summary>
    /// <param name="count">The maximum number of addresses to return.</param>
    /// <param name="excludeEndPoints">Optional endpoints to exclude from the selection.</param>
    /// <returns>A list of recommended peer addresses.</returns>
    IReadOnlyList<PeerAddress> GetBestAddresses(int count, IEnumerable<IPEndPoint>? excludeEndPoints = null);

    /// <summary>
    /// Removes stale addresses that haven't been seen within the specified age.
    /// </summary>
    /// <param name="maxAge">The maximum age for addresses to keep.</param>
    /// <returns>The number of addresses removed.</returns>
    int RemoveStaleAddresses(TimeSpan maxAge);

    /// <summary>
    /// Removes addresses with poor connection quality.
    /// </summary>
    /// <param name="minQualityScore">The minimum quality score to keep (0.0 to 1.0).</param>
    /// <param name="minAttempts">Minimum number of attempts before considering quality.</param>
    /// <returns>The number of addresses removed.</returns>
    int RemovePoorQualityAddresses(double minQualityScore, int minAttempts = 5);

    /// <summary>
    /// Saves the address book to persistent storage.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the address book from persistent storage.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all addresses from the address book.
    /// </summary>
    void Clear();
}
