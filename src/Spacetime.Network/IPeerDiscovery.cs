using System.Net;

namespace Spacetime.Network;

/// <summary>
/// Defines methods for discovering peers in the network.
/// </summary>
public interface IPeerDiscovery
{
    /// <summary>
    /// Adds a seed node endpoint that can be used for initial peer discovery.
    /// </summary>
    /// <param name="endPoint">The endpoint of the seed node.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endPoint"/> is null.</exception>
    void AddSeedNode(IPEndPoint endPoint);

    /// <summary>
    /// Gets all configured seed node endpoints.
    /// </summary>
    /// <returns>A read-only list of seed node endpoints.</returns>
    IReadOnlyList<IPEndPoint> GetSeedNodes();

    /// <summary>
    /// Discovers peers from seed nodes and adds them to the peer manager.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DiscoverPeersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests peer list from a connected peer.
    /// </summary>
    /// <param name="connection">The connection to request peers from.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of discovered peer endpoints.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    Task<IReadOnlyList<IPEndPoint>> RequestPeersAsync(IPeerConnection connection, CancellationToken cancellationToken = default);
}
