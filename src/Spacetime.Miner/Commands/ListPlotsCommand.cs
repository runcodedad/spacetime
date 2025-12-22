using System.CommandLine;
using MerkleTree.Hashing;
using Spacetime.Plotting;

namespace Spacetime.Miner.Commands;

/// <summary>
/// CLI command to list all plots.
/// </summary>
public sealed class ListPlotsCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListPlotsCommand"/> class.
    /// </summary>
    public ListPlotsCommand() : base("list-plots", "Show all registered plots")
    {
        var configOption = new Option<string?>(
            aliases: ["--config", "-c"],
            description: "Path to configuration file (default: ~/.spacetime/miner.yaml)");

        var verboseOption = new Option<bool>(
            aliases: ["--verbose", "-v"],
            description: "Show detailed information",
            getDefaultValue: () => false);

        AddOption(configOption);
        AddOption(verboseOption);

        this.SetHandler(ExecuteAsync, configOption, verboseOption);
    }

    private static async Task<int> ExecuteAsync(string? configPath, bool verbose)
    {
        try
        {
            // Load configuration
            var loader = new ConfigurationLoader();
            var config = await LoadConfigurationAsync(loader, configPath);

            // Load plot manager
            var hashFunction = new Sha256HashFunction();
            var plotManager = new PlotManager(hashFunction, config.PlotMetadataPath);
            await plotManager.LoadMetadataAsync(CancellationToken.None);

            var plots = plotManager.PlotMetadataCollection;

            if (plots.Count == 0)
            {
                Console.WriteLine("No plots found.");
                Console.WriteLine($"\nTo create a plot, run: spacetime-miner create-plot --size 1");
                return 0;
            }

            Console.WriteLine($"Total Plots: {plotManager.TotalPlotCount}");
            Console.WriteLine($"Valid Plots: {plotManager.ValidPlotCount}");
            Console.WriteLine($"Total Space: {FormatBytes(plotManager.TotalSpaceAllocatedBytes)}");
            Console.WriteLine();

            foreach (var plot in plots)
            {
                Console.WriteLine($"Plot ID: {plot.PlotId}");
                Console.WriteLine($"  Status: {plot.Status}");
                Console.WriteLine($"  File: {plot.FilePath}");
                Console.WriteLine($"  Size: {FormatBytes(plot.SpaceAllocatedBytes)}");
                Console.WriteLine($"  Created: {plot.CreatedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");

                if (verbose)
                {
                    Console.WriteLine($"  Merkle Root: {Convert.ToHexString(plot.MerkleRoot)}");
                    if (!string.IsNullOrEmpty(plot.CacheFilePath))
                    {
                        Console.WriteLine($"  Cache File: {plot.CacheFilePath}");
                    }
                }

                Console.WriteLine();
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error listing plots: {ex.Message}");
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
