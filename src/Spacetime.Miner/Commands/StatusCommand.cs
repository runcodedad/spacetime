using System.CommandLine;
using MerkleTree.Hashing;
using Spacetime.Common;
using Spacetime.Plotting;

namespace Spacetime.Miner.Commands;

/// <summary>
/// CLI command to show mining status.
/// </summary>
public sealed class StatusCommand : MinerCommand
{
    private readonly IHashFunction _hashFunction;
    private readonly IConfigurationLoader _configurationLoader;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusCommand"/> class.
    /// </summary>
    public StatusCommand(
        IHashFunction hashFunction,
        IConfigurationLoader configurationLoader) : base("status", "Show mining status")
    {
        _hashFunction = hashFunction;
        _configurationLoader = configurationLoader;

        var configOption = new Option<string?>(
            aliases: ["--config", "-c"],
            description: "Path to configuration file (default: ~/.spacetime/miner.yaml)");

        AddOption(configOption);

        this.SetHandler(ExecuteAsync, configOption);
    }

    private async Task<int> ExecuteAsync(string? configPath)
    {
        try
        {
            // Load configuration
            var config = await LoadConfigurationAsync(_configurationLoader, configPath);

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
            var plotManager = new PlotManager(_hashFunction, config.PlotMetadataPath);
            await plotManager.LoadMetadataAsync(CancellationToken.None);

            // Plot info
            Console.WriteLine("Plots:");
            Console.WriteLine($"  Total Plots: {plotManager.TotalPlotCount}");
            Console.WriteLine($"  Valid Plots: {plotManager.ValidPlotCount}");
            Console.WriteLine($"  Total Space: {ByteFormatting.FormatBytes(plotManager.TotalSpaceAllocatedBytes)}");
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
}
