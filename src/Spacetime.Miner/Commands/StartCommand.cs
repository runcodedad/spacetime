using System.CommandLine;
using MerkleTree.Hashing;
using Spacetime.Consensus;
using Spacetime.Core;
using Spacetime.Network;
using Spacetime.Plotting;
using Spacetime.Storage;

namespace Spacetime.Miner.Commands;

/// <summary>
/// CLI command to start the mining process.
/// </summary>
public sealed class StartCommand : MinerCommand
{
    private readonly IConfigurationLoader _configurationLoader;
    private readonly IHashFunction _hashFunction;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartCommand"/> class.
    /// </summary>
    public StartCommand(
        IHashFunction hashFunction,
        IConfigurationLoader configurationLoader
        ) : base("start", "Start mining")
    {
        _configurationLoader = configurationLoader;
        _hashFunction = hashFunction;

        var configOption = new Option<string?>(
            aliases: ["--config", "-c"],
            description: "Path to configuration file (default: ~/.spacetime/miner.yaml)");

        var daemonOption = new Option<bool>(
            aliases: ["--daemon", "-d"],
            description: "Run as background daemon",
            getDefaultValue: () => false);

        AddOption(configOption);
        AddOption(daemonOption);

        this.SetHandler(ExecuteAsync, configOption, daemonOption);
    }

    private async Task<int> ExecuteAsync(string? configPath, bool daemon)
    {
        MinerEventLoop? minerEventLoop = null;
        IChainStorage? chainStorage = null;

        try
        {
            // Load configuration
            var config = await LoadConfigurationAsync(_configurationLoader, configPath);

            Console.WriteLine("Spacetime Miner Starting...");
            Console.WriteLine($"  Plot Directory: {config.PlotDirectory}");
            Console.WriteLine($"  Node Address: {config.NodeAddress}:{config.NodePort}");
            Console.WriteLine($"  Network: {config.NetworkId}");
            Console.WriteLine();

            if (daemon)
            {
                Console.WriteLine("Running in daemon mode...");
                Console.WriteLine("Note: Daemon mode is not yet fully implemented.");
                Console.WriteLine("The miner will run in foreground mode.");
                Console.WriteLine();
            }

            // Initialize dependencies
            var plotManager = new PlotManager(_hashFunction, config.PlotMetadataPath);
            var epochManager = new EpochManager();
            
            // Network components
            var messageCodec = new LengthPrefixedMessageCodec();
            var peerManager = new PeerManager();
            var connectionManager = new TcpConnectionManager(messageCodec, peerManager, useTls: false);
            var messageRelay = new MessageRelay(connectionManager, peerManager);
            
            // Consensus components
            var signatureVerifier = new MockSignatureVerifier();
            var blockSigner = LoadOrCreateBlockSigner(config.PrivateKeyPath);
            var proofValidator = new ProofValidator(_hashFunction);

            // TODO: This will be replaced with a real chain state implementation in https://github.com/runcodedad/spacetime/issues/95
            var chainState = new InMemoryChainState();
            var blockValidator = new BlockValidator(signatureVerifier, proofValidator, chainState, _hashFunction);
            
            // storage 
            chainStorage = RocksDbChainStorage.Open(config.ChainStoragePath); 

            // Transaction validator and mempool
            var transactionValidator = new TransactionValidator(
                signatureVerifier,
                chainStorage.Accounts,
                chainStorage.Transactions,
                TransactionValidationConfig.Default
            );

            var mempoolConfig = MempoolConfig.Default;
            var mempool = new Mempool(transactionValidator, mempoolConfig);

            // Create miner event loop
            minerEventLoop = new MinerEventLoop(
                config,
                plotManager,
                epochManager,
                connectionManager,
                messageRelay,
                blockSigner,
                blockValidator,
                mempool,
                _hashFunction,
                chainState);

            // Setup graceful shutdown
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\nShutdown signal received...");
                cts.Cancel();
            };

            // Start the miner
            await minerEventLoop.StartAsync(cts.Token);

            // Wait for shutdown signal
            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            return 1;
        }
        finally
        {
            chainStorage?.Dispose();

            if (minerEventLoop != null)
            {
                await minerEventLoop.StopAsync();
                await minerEventLoop.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Loads an existing block signer or creates a new one if it doesn't exist.
    /// </summary>
    private static IBlockSigner LoadOrCreateBlockSigner(string privateKeyPath)
    {
        try
        {
            if (File.Exists(privateKeyPath))
            {
                var keyData = File.ReadAllBytes(privateKeyPath);
                return MockBlockSigner.FromPrivateKey(keyData);
            }
            else
            {
                // Create new key
                var signer = MockBlockSigner.Generate();
                
                // Create directory if needed
                var directory = Path.GetDirectoryName(privateKeyPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Save private key
                File.WriteAllBytes(privateKeyPath, signer.GetPrivateKey());
                
                Console.WriteLine($"⚠️  WARNING: Using mock cryptographic signer (not production-ready)");
                Console.WriteLine($"New miner key generated and saved to: {privateKeyPath}");
                Console.WriteLine($"Public key: {Convert.ToHexString(signer.GetPublicKey())}");
                Console.WriteLine();
                
                return signer;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load or create block signer: {ex.Message}", ex);
        }
    }
}
