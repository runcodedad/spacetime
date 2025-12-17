using BenchmarkDotNet.Attributes;
using NSubstitute;
using Spacetime.Core;
using Spacetime.Network;
using Spacetime.Storage;
using System.Net;

namespace Spacetime.Benchmarks.Network;

/// <summary>
/// Benchmarks for block synchronization performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3)]
public class BlockSynchronizationBenchmark
{
    private IConnectionManager _connectionManager = null!;
    private IPeerManager _peerManager = null!;
    private IChainStorage _chainStorage = null!;
    private IBlockValidator _blockValidator = null!;
    private BandwidthMonitor _bandwidthMonitor = null!;

    [Params(1, 4, 8)]
    public int ParallelDownloads { get; set; }

    [Params(100, 1000)]
    public int TargetHeight { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _connectionManager = Substitute.For<IConnectionManager>();
        _peerManager = Substitute.For<IPeerManager>();
        _chainStorage = CreateMockStorage();
        _blockValidator = Substitute.For<IBlockValidator>();
        _bandwidthMonitor = new BandwidthMonitor();

        // Setup peers
        var peers = new List<PeerInfo>();
        for (var i = 0; i < 10; i++)
        {
            peers.Add(new PeerInfo($"peer{i}", new IPEndPoint(IPAddress.Loopback, 8000 + i), 1));
        }
        _peerManager.GetBestPeers(Arg.Any<int>()).Returns(peers);
        _connectionManager.GetActiveConnections().Returns(new List<IPeerConnection>());

        // Setup storage to simulate blocks
        _chainStorage.Metadata.GetChainHeight().Returns(0L);
        _chainStorage.Metadata.GetBestBlockHash().Returns(new byte[32]);

        // Validator always returns valid
        var validResult = BlockValidationResult.Success();
        _blockValidator.ValidateBlockAsync(Arg.Any<Block>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(validResult));
    }

    [Benchmark]
    public async Task SynchronizeBlocks()
    {
        var config = new SyncConfig
        {
            ParallelDownloads = ParallelDownloads,
            MaxHeadersPerRequest = 500,
            MaxRetries = 1
        };

        var synchronizer = new BlockSynchronizer(
            _connectionManager,
            _peerManager,
            _chainStorage,
            _blockValidator,
            _bandwidthMonitor,
            config);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            await synchronizer.StartAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected for benchmark
        }
        finally
        {
            await synchronizer.DisposeAsync();
        }
    }

    [Benchmark]
    public void CreateSyncProgress()
    {
        var progress = new SyncProgress(
            100,
            1000,
            50,
            45,
            5000000,
            100000.0,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow,
            SyncState.DownloadingBlocks);

        _ = progress.PercentComplete;
        _ = progress.EstimatedTimeRemaining;
        _ = progress.ToString();
    }

    [Benchmark]
    public void QueueBlockDownloads()
    {
        var queue = new Queue<(byte[] hash, long height)>();
        for (var i = 0; i < TargetHeight; i++)
        {
            queue.Enqueue((new byte[32], i));
        }
    }

    private static IChainStorage CreateMockStorage()
    {
        var storage = Substitute.For<IChainStorage>();
        var blockStorage = Substitute.For<IBlockStorage>();
        var metadata = Substitute.For<IChainMetadata>();
        var accounts = Substitute.For<IAccountStorage>();
        var transactions = Substitute.For<ITransactionIndex>();

        storage.Blocks.Returns(blockStorage);
        storage.Metadata.Returns(metadata);
        storage.Accounts.Returns(accounts);
        storage.Transactions.Returns(transactions);

        return storage;
    }
}
