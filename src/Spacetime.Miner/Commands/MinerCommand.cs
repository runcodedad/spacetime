using System.CommandLine;

namespace Spacetime.Miner.Commands;

/// <summary>
/// Abstract base class for miner-related commands that provides common functionality
/// for loading and managing miner configuration.
/// </summary>
/// <param name="name">The name of the command.</param>
/// <param name="description">An optional description of what the command does.</param>
/// <remarks>
/// This class extends the base <see cref="Command"/> class and provides a shared
/// configuration loading mechanism for all miner commands. It handles default
/// configuration paths, automatic configuration file creation, and environment
/// variable overrides.
/// </remarks>
public abstract class MinerCommand(string name, string? description) : Command(name, description)
{
    /// <summary>
    /// Loads the miner configuration from the specified path or the default location.
    /// </summary>
    /// <param name="loader">The configuration loader instance.</param>
    /// <param name="configPath">The optional path to the configuration file.</param>
    /// <returns>The loaded <see cref="MinerConfiguration"/> instance.</returns>
    protected static async Task<MinerConfiguration> LoadConfigurationAsync(
        IConfigurationLoader loader,
        string? configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            configPath = Path.Combine(homeDir, ".spacetime", "miner.yaml");
        }

        if (!File.Exists(configPath))
        {
            Console.WriteLine($"Configuration file not found: {configPath}");
            Console.WriteLine("Creating default configuration...");
            await loader.CreateDefaultConfigAsync(configPath);
            Console.WriteLine($"Created default configuration at: {configPath}");
        }

        return await loader.LoadWithEnvironmentOverridesAsync(configPath);
    }
}
