namespace Spacetime.Core;

/// <summary>
/// Provides functionality for generating genesis blocks.
/// </summary>
/// <remarks>
/// Genesis block generators create the first block in a blockchain based on a genesis configuration.
/// The genesis block is special because it has no parent and defines the initial state of the network.
/// </remarks>
public interface IGenesisBlockGenerator
{
    /// <summary>
    /// Generates a genesis block from the specified configuration.
    /// </summary>
    /// <param name="config">The genesis configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A fully constructed and signed genesis block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when genesis generation fails.</exception>
    /// <remarks>
    /// The generated genesis block will:
    /// - Have height 0
    /// - Have parent hash of all zeros
    /// - Include premine transactions if specified in config
    /// - Be signed by the genesis signer
    /// - Have all other fields populated according to config
    /// </remarks>
    Task<Block> GenerateGenesisBlockAsync(GenesisConfig config, CancellationToken cancellationToken = default);
}
