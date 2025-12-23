using System.CommandLine;
using MerkleTree.Hashing;
using Spacetime.Common;
using Spacetime.Plotting;

namespace Spacetime.Miner.Commands;

/// <summary>
/// CLI command to create a new plot file.
/// </summary>
public sealed class CreatePlotCommand : MinerCommand
{
    private readonly IHashFunction _hashFunction;
    private readonly IConfigurationLoader _configurationLoader;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePlotCommand"/> class.
    /// </summary>
    public CreatePlotCommand(
        IHashFunction hashFunction,
        IConfigurationLoader configurationLoader) : base("create-plot", "Create a new plot file")
    {
        _hashFunction = hashFunction;
        _configurationLoader = configurationLoader;

        var sizeOption = new Option<long>(
            aliases: ["--size", "-s"],
            description: "Plot size in gigabytes",
            getDefaultValue: () => 1L);
            
        sizeOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(sizeOption);
            if (value < 1)
            {
                result.ErrorMessage = "Plot size must be at least 1 GB";
            }
        });

        var outputOption = new Option<string?>(
            aliases: ["--output", "-o"],
            description: "Output file path (default: auto-generated in plot directory)");

        var configOption = new Option<string?>(
            aliases: ["--config", "-c"],
            description: "Path to configuration file (default: ~/.spacetime/miner.yaml)");

        var includeCacheOption = new Option<bool>(
            aliases: ["--cache"],
            description: "Include Merkle tree cache for faster proof generation",
            getDefaultValue: () => false);

        var cacheLevelsOption = new Option<int>(
            aliases: ["--cache-levels"],
            description: "Number of Merkle tree levels to cache",
            getDefaultValue: () => 5);
        cacheLevelsOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(cacheLevelsOption);
            if (value < 0 || value > 20)
            {
                result.ErrorMessage = "Cache levels must be between 0 and 20";
            }
        });

        AddOption(sizeOption);
        AddOption(outputOption);
        AddOption(configOption);
        AddOption(includeCacheOption);
        AddOption(cacheLevelsOption);

        this.SetHandler(ExecuteAsync, sizeOption, outputOption, configOption, includeCacheOption, cacheLevelsOption);
    }

    private async Task<int> ExecuteAsync(
        long sizeGB,
        string? outputPath,
        string? configPath,
        bool includeCache,
        int cacheLevels)
    {
        try
        {
            Console.WriteLine("Creating plot...");
            Console.WriteLine($"  Size: {sizeGB} GB");
            Console.WriteLine($"  Cache: {(includeCache ? $"Enabled ({cacheLevels} levels)" : "Disabled")}");

            // Load configuration
            var config = await LoadConfigurationAsync(_configurationLoader, configPath);

            // Ensure plot directory exists
            if (!Directory.Exists(config.PlotDirectory))
            {
                Directory.CreateDirectory(config.PlotDirectory);
                Console.WriteLine($"  Created plot directory: {config.PlotDirectory}");
            }

            // Generate output path if not provided
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                var plotId = Guid.NewGuid();
                outputPath = Path.Combine(config.PlotDirectory, $"plot_{plotId:N}.plot");
            }
            else if (!Path.IsPathRooted(outputPath))
            {
                outputPath = Path.Combine(config.PlotDirectory, outputPath);
            }

            Console.WriteLine($"  Output: {outputPath}");

            // Generate random keys for plot
            var minerPublicKey = new byte[32];
            var plotSeed = new byte[32];
            System.Security.Cryptography.RandomNumberGenerator.Fill(minerPublicKey);
            System.Security.Cryptography.RandomNumberGenerator.Fill(plotSeed);

            // Create plot configuration
            var plotConfig = PlotConfiguration.CreateFromGB(
                sizeGB,
                minerPublicKey,
                plotSeed,
                outputPath,
                includeCache,
                cacheLevels);

            // Create plot
            var creator = new PlotCreator(_hashFunction);
            var progress = new ProgressReporter("Creating plot");
            
            var result = await creator.CreatePlotAsync(
                plotConfig,
                progress,
                CancellationToken.None);

            var fileInfo = new FileInfo(outputPath);

            Console.WriteLine($"\n✓ Plot created successfully!");
            Console.WriteLine($"  Merkle Root: {Convert.ToHexString(result.Header.MerkleRoot)}");
            Console.WriteLine($"  File Size: {ByteFormatting.FormatBytes(fileInfo.Length)}");
            Console.WriteLine($"  Leaf Count: {result.Header.LeafCount:N0}");
            if (result.CacheFilePath != null)
            {
                Console.WriteLine($"  Cache File: {result.CacheFilePath}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error creating plot: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Simple progress reporter that writes to console.
    /// </summary>
    private sealed class ProgressReporter : IProgress<double>
    {
        private readonly string _message;
        private int _lastPercent = -1;

        public ProgressReporter(string message)
        {
            _message = message;
        }

        public void Report(double value)
        {
            var percent = (int)(value * 100);
            if (percent != _lastPercent && percent % 5 == 0)
            {
                Console.Write($"\r{_message}: {percent}%");
                _lastPercent = percent;
            }

            if (percent >= 100)
            {
                Console.WriteLine();
            }
        }
    }
}
