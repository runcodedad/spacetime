using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Spacetime.Network;

namespace Spacetime.Benchmarks;

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
