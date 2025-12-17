using System.Net;
using NSubstitute;
using Spacetime.Core;
using Spacetime.Storage;

namespace Spacetime.Network.IntegrationTests;

/// <summary>
/// Integration tests for block synchronization with multiple nodes.
/// </summary>
public class BlockSynchronizerIntegrationTests : IDisposable
{
    private readonly List<IChainStorage> _storages = new();
    private readonly List<BlockSynchronizer> _synchronizers = new();

    [Fact]
    public async Task StartAsync_WithNoPeers_ThrowsInvalidOperationException()
    {
        // Arrange
        var peerManager = Substitute.For<IPeerManager>();
        var chainStorage = CreateMockStorage();
        var blockValidator = Substitute.For<IBlockValidator>();
        var bandwidthMonitor = new BandwidthMonitor();

        peerManager.GetBestPeers(Arg.Any<int>()).Returns(new List<PeerInfo>());

        var synchronizer = new BlockSynchronizer(
            peerManager,
            chainStorage,
            blockValidator,
            bandwidthMonitor);

        _synchronizers.Add(synchronizer);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await synchronizer.StartAsync());
    }

    [Fact]
    public async Task StartAsync_WithPeersButNoBlocks_CompletesWithoutError()
    {
        // Arrange
        var peerManager = Substitute.For<IPeerManager>();
        var chainStorage = CreateMockStorage();
        var blockValidator = Substitute.For<IBlockValidator>();
        var bandwidthMonitor = new BandwidthMonitor();

        var peer = new PeerInfo("peer1", new IPEndPoint(IPAddress.Loopback, 8000), 1);
        peerManager.GetBestPeers(Arg.Any<int>()).Returns(new List<PeerInfo> { peer });

        // Set current height to match target (already synced)
        chainStorage.Metadata.GetChainHeight().Returns(0L);

        var synchronizer = new BlockSynchronizer(
            peerManager,
            chainStorage,
            blockValidator,
            bandwidthMonitor);

        _synchronizers.Add(synchronizer);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act & Assert - should complete quickly since target is same as current
        await synchronizer.StartAsync(cts.Token);

        Assert.Equal(SyncState.Synced, synchronizer.Progress.State);
    }

    [Fact]
    public async Task MultipleNodes_SyncInParallel_CompleteSuccessfully()
    {
        // Arrange
        var nodeCount = 3;
        var synchronizers = new List<BlockSynchronizer>();

        for (var i = 0; i < nodeCount; i++)
        {
            var peerManager = Substitute.For<IPeerManager>();
            var chainStorage = CreateMockStorage();
            var blockValidator = Substitute.For<IBlockValidator>();
            var bandwidthMonitor = new BandwidthMonitor();

            var peer = new PeerInfo($"peer{i}", new IPEndPoint(IPAddress.Loopback, 8000 + i), 1);
            peerManager.GetBestPeers(Arg.Any<int>()).Returns(new List<PeerInfo> { peer });

            chainStorage.Metadata.GetChainHeight().Returns(0L);

            var synchronizer = new BlockSynchronizer(
                peerManager,
                chainStorage,
                blockValidator,
                bandwidthMonitor);

            synchronizers.Add(synchronizer);
            _synchronizers.Add(synchronizer);
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var tasks = synchronizers.Select(s => s.StartAsync(cts.Token)).ToList();
        await Task.WhenAll(tasks);

        // Assert
        foreach (var synchronizer in synchronizers)
        {
            Assert.Equal(SyncState.Synced, synchronizer.Progress.State);
        }
    }

    [Fact]
    public async Task ResumeAsync_AfterStop_ContinuesSynchronization()
    {
        // Arrange
        var peerManager = Substitute.For<IPeerManager>();
        var chainStorage = CreateMockStorage();
        var blockValidator = Substitute.For<IBlockValidator>();
        var bandwidthMonitor = new BandwidthMonitor();

        var peer = new PeerInfo("peer1", new IPEndPoint(IPAddress.Loopback, 8000), 1);
        peerManager.GetBestPeers(Arg.Any<int>()).Returns(new List<PeerInfo> { peer });

        chainStorage.Metadata.GetChainHeight().Returns(0L);

        var synchronizer = new BlockSynchronizer(
            peerManager,
            chainStorage,
            blockValidator,
            bandwidthMonitor);

        _synchronizers.Add(synchronizer);

        using var cts1 = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act - Start and let it run briefly
        try
        {
            await synchronizer.StartAsync(cts1.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Resume synchronization
        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await synchronizer.ResumeAsync(cts2.Token);

        // Assert
        Assert.Equal(SyncState.Synced, synchronizer.Progress.State);
    }

    [Fact]
    public async Task ProgressUpdated_Event_IsRaisedDuringSynchronization()
    {
        // Arrange
        var peerManager = Substitute.For<IPeerManager>();
        var chainStorage = CreateMockStorage();
        var blockValidator = Substitute.For<IBlockValidator>();
        var bandwidthMonitor = new BandwidthMonitor();

        var peer = new PeerInfo("peer1", new IPEndPoint(IPAddress.Loopback, 8000), 1);
        peerManager.GetBestPeers(Arg.Any<int>()).Returns(new List<PeerInfo> { peer });

        chainStorage.Metadata.GetChainHeight().Returns(0L);

        var synchronizer = new BlockSynchronizer(
            peerManager,
            chainStorage,
            blockValidator,
            bandwidthMonitor);

        _synchronizers.Add(synchronizer);

        var progressUpdates = new List<SyncProgress>();
        synchronizer.ProgressUpdated += (sender, progress) => progressUpdates.Add(progress);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        await synchronizer.StartAsync(cts.Token);

        // Assert
        Assert.NotEmpty(progressUpdates);
        Assert.Contains(progressUpdates, p => p.State == SyncState.Discovering);
    }

    [Fact]
    public async Task StopAsync_DuringSync_CancelsSynchronization()
    {
        // Arrange
        var peerManager = Substitute.For<IPeerManager>();
        var chainStorage = CreateMockStorage();
        var blockValidator = Substitute.For<IBlockValidator>();
        var bandwidthMonitor = new BandwidthMonitor();

        var peer = new PeerInfo("peer1", new IPEndPoint(IPAddress.Loopback, 8000), 1);
        peerManager.GetBestPeers(Arg.Any<int>()).Returns(new List<PeerInfo> { peer });

        // Set a higher target to make sync take longer
        chainStorage.Metadata.GetChainHeight().Returns(0L);

        var synchronizer = new BlockSynchronizer(
            peerManager,
            chainStorage,
            blockValidator,
            bandwidthMonitor);

        _synchronizers.Add(synchronizer);

        // Act
        var syncTask = Task.Run(async () =>
        {
            try
            {
                await synchronizer.StartAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        });

        await Task.Delay(100); // Let sync start
        await synchronizer.StopAsync();

        // Assert
        Assert.False(synchronizer.IsSynchronizing);
    }

    private IChainStorage CreateMockStorage()
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

        _storages.Add(storage);
        return storage;
    }

    public void Dispose()
    {
        foreach (var synchronizer in _synchronizers)
        {
            synchronizer.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
        }

        foreach (var storage in _storages)
        {
            storage.Dispose();
        }
    }
}
