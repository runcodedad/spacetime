using System.CommandLine;
using MerkleTree.Hashing;
using Spacetime.Common;
using Spacetime.Plotting;

namespace Spacetime.Miner.Commands;

/// <summary>
/// CLI command to list all plots.
/// </summary>
public sealed class ListPlotsCommand : MinerCommand
{
    private readonly IHashFunction _hashFunction;
    private readonly IConfigurationLoader _configurationLoader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListPlotsCommand"/> class.
    /// </summary>
    public ListPlotsCommand(
        IHashFunction hashFunction,
        IConfigurationLoader configurationLoader) : base("list-plots", "Show all registered plots")
    {
        _hashFunction = hashFunction;
        _configurationLoader = configurationLoader;

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

    private async Task<int> ExecuteAsync(string? configPath, bool verbose)
    {
        try
        {
            // Load configuration
            var config = await LoadConfigurationAsync(_configurationLoader, configPath);

            // Load plot manager
            var plotManager = new PlotManager(_hashFunction, config.PlotMetadataPath);
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
            Console.WriteLine($"Total Space: {ByteFormatting.FormatBytes(plotManager.TotalSpaceAllocatedBytes)}");
            Console.WriteLine();

            foreach (var plot in plots)
            {
                Console.WriteLine($"Plot ID: {plot.PlotId}");
                Console.WriteLine($"  Status: {plot.Status}");
                Console.WriteLine($"  File: {plot.FilePath}");
                Console.WriteLine($"  Size: {ByteFormatting.FormatBytes(plot.SpaceAllocatedBytes)}");
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
}
