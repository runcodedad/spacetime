using System.CommandLine;
using MerkleTree.Hashing;
using Spacetime.Plotting;

namespace Spacetime.Miner.Commands;

/// <summary>
/// CLI command to delete a plot.
/// </summary>
public sealed class DeletePlotCommand : Command
{
    private readonly IHashFunction _hashFunction;
    /// <summary>
    /// Initializes a new instance of the <see cref="DeletePlotCommand"/> class.
    /// </summary>
    public DeletePlotCommand(IHashFunction hashFunction) : base("delete-plot", "Remove a plot from the miner")
    {
        _hashFunction = hashFunction;

        var plotIdArgument = new Argument<string>(
            "plot-id",
            "The ID of the plot to delete");

        var configOption = new Option<string?>(
            aliases: ["--config", "-c"],
            description: "Path to configuration file (default: ~/.spacetime/miner.yaml)");

        var deleteFileOption = new Option<bool>(
            aliases: ["--delete-file"],
            description: "Also delete the plot file from disk",
            getDefaultValue: () => false);

        var forceOption = new Option<bool>(
            aliases: ["--force", "-f"],
            description: "Skip confirmation prompt",
            getDefaultValue: () => false);

        AddArgument(plotIdArgument);
        AddOption(configOption);
        AddOption(deleteFileOption);
        AddOption(forceOption);

        this.SetHandler(ExecuteAsync, plotIdArgument, configOption, deleteFileOption, forceOption);
    }

    private async Task<int> ExecuteAsync(
        string plotIdStr,
        string? configPath,
        bool deleteFile,
        bool force)
    {
        try
        {
            if (!Guid.TryParse(plotIdStr, out var plotId))
            {
                Console.Error.WriteLine($"Invalid plot ID: {plotIdStr}");
                Console.Error.WriteLine("Plot ID must be a valid GUID.");
                return 1;
            }

            // Load configuration
            var loader = new ConfigurationLoader();
            var config = await LoadConfigurationAsync(loader, configPath);

            // Load plot manager
            var plotManager = new PlotManager(_hashFunction, config.PlotMetadataPath);
            await plotManager.LoadMetadataAsync(CancellationToken.None);

            // Find the plot
            var plot = plotManager.GetPlotMetadata(plotId);
            if (plot == null)
            {
                Console.Error.WriteLine($"Plot not found: {plotId}");
                Console.WriteLine("\nRun 'spacetime-miner list-plots' to see available plots.");
                return 1;
            }

            // Display plot info
            Console.WriteLine($"Plot to delete:");
            Console.WriteLine($"  ID: {plot.PlotId}");
            Console.WriteLine($"  File: {plot.FilePath}");
            Console.WriteLine($"  Size: {FormatBytes(plot.SpaceAllocatedBytes)}");
            Console.WriteLine($"  Status: {plot.Status}");
            Console.WriteLine();

            if (deleteFile)
            {
                Console.WriteLine("WARNING: This will also delete the plot file from disk!");
            }

            // Confirm deletion
            if (!force)
            {
                Console.Write("Are you sure you want to delete this plot? (yes/no): ");
                var confirmation = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (confirmation != "yes" && confirmation != "y")
                {
                    Console.WriteLine("Deletion cancelled.");
                    return 0;
                }
            }

            // Remove from manager
            var removed = await plotManager.RemovePlotAsync(plotId, CancellationToken.None);
            if (!removed)
            {
                Console.Error.WriteLine("Failed to remove plot from manager.");
                return 1;
            }

            // Save updated metadata
            await plotManager.SaveMetadataAsync(CancellationToken.None);

            Console.WriteLine("✓ Plot removed from manager.");

            // Delete file if requested
            if (deleteFile)
            {
                try
                {
                    if (File.Exists(plot.FilePath))
                    {
                        File.Delete(plot.FilePath);
                        Console.WriteLine($"✓ Deleted plot file: {plot.FilePath}");
                    }

                    if (!string.IsNullOrEmpty(plot.CacheFilePath) && File.Exists(plot.CacheFilePath))
                    {
                        File.Delete(plot.CacheFilePath);
                        Console.WriteLine($"✓ Deleted cache file: {plot.CacheFilePath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Warning: Failed to delete file: {ex.Message}");
                    Console.WriteLine("The plot has been removed from the manager, but the file remains on disk.");
                    Console.WriteLine("You may need to delete it manually.");
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error deleting plot: {ex.Message}");
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
