namespace Spacetime.Miner;

/// <summary>
/// Defines a contract for loading and managing miner configuration from various sources.
/// </summary>
/// <remarks>
/// Implementations of this interface are responsible for:
/// <list type="bullet">
/// <item><description>Loading configuration from files (e.g., YAML, JSON)</description></item>
/// <item><description>Applying environment variable overrides to configuration</description></item>
/// <item><description>Creating default configuration files when none exist</description></item>
/// </list>
/// </remarks>
public interface IConfigurationLoader
{
    /// <summary>
    /// Loads configuration from a YAML file.
    /// </summary>
    /// <param name="filePath">Path to the YAML configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded configuration.</returns>
    Task<MinerConfiguration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads configuration from a YAML file and applies environment variable overrides.
    /// </summary>
    /// <param name="filePath">Path to the YAML configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded configuration with environment variable overrides applied.</returns>
    Task<MinerConfiguration> LoadWithEnvironmentOverridesAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a default configuration file at the specified path.
    /// </summary>
    /// <param name="filePath">Path where the configuration file should be created.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateDefaultConfigAsync(string filePath, CancellationToken cancellationToken = default);
}
