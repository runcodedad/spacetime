using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NSubstitute;
using Spacetime.Network;

namespace Spacetime.Benchmarks;

/// <summary>
/// Performance benchmarks for message relay operations.
/// Tests deduplication, rate limiting, bandwidth management, and message broadcasting.
/// </summary>
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
[MarkdownExporter]
public class MessageRelayBenchmarks
{
    private MessageTracker _messageTracker = null!;
    private RateLimiter _rateLimiter = null!;
    private BandwidthMonitor _bandwidthMonitor = null!;
    private PriorityMessageQueue _priorityQueue = null!;
    private MessageRelay _messageRelay = null!;
    private TransactionMessage[] _messages = null!;
    private IConnectionManager _connectionManager = null!;
    private IPeerManager _peerManager = null!;

    [Params(100, 1000, 10000)]
    public int MessageCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _messageTracker = new MessageTracker();
        _rateLimiter = new RateLimiter(maxTokens: 100000, refillInterval: TimeSpan.FromSeconds(10), refillAmount: 10000);
        _bandwidthMonitor = new BandwidthMonitor(maxBytesPerSecondPerPeer: 100_000_000, maxTotalBytesPerSecond: 1_000_000_000);
        _priorityQueue = new PriorityMessageQueue(capacity: 100000);

        _connectionManager = Substitute.For<IConnectionManager>();
        _peerManager = Substitute.For<IPeerManager>();
        _connectionManager.GetActiveConnections().Returns([]);

        _messageRelay = new MessageRelay(_connectionManager, _peerManager, _messageTracker, _rateLimiter, _bandwidthMonitor);

        // Create test messages
        _messages = new TransactionMessage[MessageCount];
        for (int i = 0; i < MessageCount; i++)
        {
            var data = new byte[100];
            for (int j = 0; j < data.Length; j++)
            {
                data[j] = (byte)((i + j) % 256);
            }
            _messages[i] = new TransactionMessage(data);
        }
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await _messageRelay.DisposeAsync();
        await _priorityQueue.DisposeAsync();
    }

    [Benchmark]
    public void MessageTracker_MarkAndCheckIfNew()
    {
        _messageTracker.Clear();
        foreach (var message in _messages)
        {
            _messageTracker.MarkAndCheckIfNew(message);
        }
    }

    [Benchmark]
    public void MessageTracker_HasSeen()
    {
        // First mark all messages as seen
        _messageTracker.Clear();
        foreach (var message in _messages)
        {
            _messageTracker.MarkAndCheckIfNew(message);
        }

        // Then check them all
        foreach (var message in _messages)
        {
            _messageTracker.HasSeen(message);
        }
    }

    [Benchmark]
    public void RateLimiter_TryConsume()
    {
        _rateLimiter.Clear();
        var peerId = "benchmark-peer";
        foreach (var _ in _messages)
        {
            _rateLimiter.TryConsume(peerId, tokens: 1);
        }
    }

    [Benchmark]
    public void BandwidthMonitor_CanSend()
    {
        var peerId = "benchmark-peer";
        foreach (var message in _messages)
        {
            _bandwidthMonitor.CanSend(peerId, message.Payload.Length);
        }
    }

    [Benchmark]
    public void BandwidthMonitor_RecordSent()
    {
        var peerId = "benchmark-peer";
        foreach (var message in _messages)
        {
            _bandwidthMonitor.RecordSent(peerId, message.Payload.Length);
        }
    }

    [Benchmark]
    public async Task PriorityQueue_EnqueueDequeue()
    {
        // Enqueue all messages
        foreach (var message in _messages)
        {
            await _priorityQueue.EnqueueAsync(message, "peer1", MessagePriority.Normal);
        }

        // Dequeue all messages
        for (int i = 0; i < MessageCount; i++)
        {
            await _priorityQueue.DequeueAsync();
        }
    }

    [Benchmark]
    public void MessageRelay_ShouldRelay()
    {
        _messageTracker.Clear();
        foreach (var message in _messages)
        {
            _messageRelay.ShouldRelay(message);
        }
    }

    [Benchmark]
    public async Task MessageRelay_BroadcastAsync()
    {
        foreach (var message in _messages)
        {
            await _messageRelay.BroadcastAsync(message, sourcePeerId: "source");
        }
    }
}

/// <summary>
/// Benchmarks for message deduplication under different load patterns.
/// </summary>
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
[MarkdownExporter]
public class MessageDeduplicationBenchmarks
{
    private MessageTracker _messageTracker = null!;
    private TransactionMessage[] _uniqueMessages = null!;
    private TransactionMessage[] _duplicateMessages = null!;

    [Params(1000, 10000)]
    public int MessageCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _messageTracker = new MessageTracker(maxTrackedMessages: 100000);

        // Create unique messages
        _uniqueMessages = new TransactionMessage[MessageCount];
        for (int i = 0; i < MessageCount; i++)
        {
            var data = new byte[100];
            for (int j = 0; j < data.Length; j++)
            {
                data[j] = (byte)((i + j) % 256);
            }
            _uniqueMessages[i] = new TransactionMessage(data);
        }

        // Create duplicate messages (50% duplicates)
        _duplicateMessages = new TransactionMessage[MessageCount];
        for (int i = 0; i < MessageCount; i++)
        {
            _duplicateMessages[i] = i % 2 == 0 ? _uniqueMessages[i / 2] : _uniqueMessages[i];
        }
    }

    [Benchmark]
    public void UniqueMessages_Deduplication()
    {
        _messageTracker.Clear();
        foreach (var message in _uniqueMessages)
        {
            _messageTracker.MarkAndCheckIfNew(message);
        }
    }

    [Benchmark]
    public void DuplicateMessages_Deduplication()
    {
        _messageTracker.Clear();
        foreach (var message in _duplicateMessages)
        {
            _messageTracker.MarkAndCheckIfNew(message);
        }
    }

    [Benchmark]
    public void AllDuplicates_Deduplication()
    {
        _messageTracker.Clear();
        var singleMessage = _uniqueMessages[0];
        for (int i = 0; i < MessageCount; i++)
        {
            _messageTracker.MarkAndCheckIfNew(singleMessage);
        }
    }
}

/// <summary>
/// Benchmarks for priority queue with different priority distributions.
/// </summary>
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
[MarkdownExporter]
public class PriorityQueueBenchmarks
{
    private PriorityMessageQueue _queue = null!;
    private TransactionMessage[] _messages = null!;

    [Params(1000, 10000)]
    public int MessageCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _queue = new PriorityMessageQueue(capacity: 100000);

        // Create test messages
        _messages = new TransactionMessage[MessageCount];
        for (int i = 0; i < MessageCount; i++)
        {
            var data = new byte[100];
            for (int j = 0; j < data.Length; j++)
            {
                data[j] = (byte)((i + j) % 256);
            }
            _messages[i] = new TransactionMessage(data);
        }
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await _queue.DisposeAsync();
    }

    [Benchmark]
    public async Task UniformPriority_EnqueueDequeue()
    {
        // All messages with same priority
        foreach (var message in _messages)
        {
            await _queue.EnqueueAsync(message, "peer1", MessagePriority.Normal);
        }

        for (int i = 0; i < MessageCount; i++)
        {
            await _queue.DequeueAsync();
        }
    }

    [Benchmark]
    public async Task MixedPriority_EnqueueDequeue()
    {
        // Messages with different priorities
        for (int i = 0; i < MessageCount; i++)
        {
            var priority = (MessagePriority)(i % 4);
            await _queue.EnqueueAsync(_messages[i], $"peer{i % 10}", priority);
        }

        for (int i = 0; i < MessageCount; i++)
        {
            await _queue.DequeueAsync();
        }
    }

    [Benchmark]
    public async Task HighPriorityBurst_EnqueueDequeue()
    {
        // 80% high priority, 20% low priority
        for (int i = 0; i < MessageCount; i++)
        {
            var priority = i % 5 == 0 ? MessagePriority.Low : MessagePriority.High;
            await _queue.EnqueueAsync(_messages[i], $"peer{i % 10}", priority);
        }

        for (int i = 0; i < MessageCount; i++)
        {
            await _queue.DequeueAsync();
        }
    }
}
