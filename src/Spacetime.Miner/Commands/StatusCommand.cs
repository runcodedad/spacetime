using System.CommandLine;
using MerkleTree.Hashing;
using Spacetime.Plotting;

namespace Spacetime.Miner.Commands;

/// <summary>
/// CLI command to show mining status.
/// </summary>
public sealed class StatusCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatusCommand"/> class.
    /// </summary>
    public StatusCommand() : base("status", "Show mining status")
    {
        var configOption = new Option<string?>(
            aliases: ["--config", "-c"],
            description: "Path to configuration file (default: ~/.spacetime/miner.yaml)");

        AddOption(configOption);

        this.SetHandler(ExecuteAsync, configOption);
    }

    private static async Task<int> ExecuteAsync(string? configPath)
    {
        try
        {
            // Load configuration
            var loader = new ConfigurationLoader();
            var config = await LoadConfigurationAsync(loader, configPath);

            Console.WriteLine("Spacetime Miner Status");
            Console.WriteLine("======================");
            Console.WriteLine();

            // Configuration info
            Console.WriteLine("Configuration:");
            Console.WriteLine($"  Plot Directory: {config.PlotDirectory}");
            Console.WriteLine($"  Metadata File: {config.PlotMetadataPath}");
            Console.WriteLine($"  Node Address: {config.NodeAddress}:{config.NodePort}");
            Console.WriteLine($"  Network: {config.NetworkId}");
            Console.WriteLine($"  Max Concurrent Proofs: {config.MaxConcurrentProofs}");
            Console.WriteLine($"  Performance Monitoring: {(config.EnablePerformanceMonitoring ? "Enabled" : "Disabled")}");
            Console.WriteLine();

            // Load plot manager
            var hashFunction = new Sha256HashFunction();
            var plotManager = new PlotManager(hashFunction, config.PlotMetadataPath);
            await plotManager.LoadMetadataAsync(CancellationToken.None);

            // Plot info
            Console.WriteLine("Plots:");
            Console.WriteLine($"  Total Plots: {plotManager.TotalPlotCount}");
            Console.WriteLine($"  Valid Plots: {plotManager.ValidPlotCount}");
            Console.WriteLine($"  Total Space: {FormatBytes(plotManager.TotalSpaceAllocatedBytes)}");
            Console.WriteLine();

            // Mining status
            Console.WriteLine("Mining Status:");
            Console.WriteLine("  Status: Not Running");
            Console.WriteLine("  (Run 'spacetime-miner start' to begin mining)");
            Console.WriteLine();

            // TODO: When mining is implemented, show:
            // - Current mining state (running/stopped)
            // - Uptime
            // - Proofs submitted
            // - Success rate
            // - Last proof time

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting status: {ex.Message}");
            return 1;
        }
    }

    private static async Task<MinerConfiguration> LoadConfigurationAsync(
        ConfigurationLoader loader,
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

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:F2} {sizes[order]}";
    }
}
