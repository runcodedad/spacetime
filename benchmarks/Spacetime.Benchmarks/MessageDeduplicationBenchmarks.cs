using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Spacetime.Network;

namespace Spacetime.Benchmarks;

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
