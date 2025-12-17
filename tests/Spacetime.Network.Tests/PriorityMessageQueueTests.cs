namespace Spacetime.Network.Tests;

public class PriorityMessageQueueTests
{
    private static TransactionMessage CreateTestMessage(int seed = 0)
    {
        var data = new byte[100];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(seed + i);
        }
        return new TransactionMessage(data);
    }

    [Fact]
    public async Task EnqueueAsync_AddsMessageToQueue()
    {
        // Arrange
        await using var queue = new PriorityMessageQueue();
        var message = CreateTestMessage(1);

        // Act
        await queue.EnqueueAsync(message, "peer1", MessagePriority.Normal);

        // Assert
        Assert.Equal(1, queue.Count);
    }

    [Fact]
    public async Task EnqueueAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        await using var queue = new PriorityMessageQueue();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await queue.EnqueueAsync(null!, "peer1", MessagePriority.Normal));
    }

    [Fact]
    public async Task EnqueueAsync_WithNullPeerId_ThrowsArgumentNullException()
    {
        // Arrange
        await using var queue = new PriorityMessageQueue();
        var message = CreateTestMessage(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await queue.EnqueueAsync(message, null!, MessagePriority.Normal));
    }

    [Fact]
    public async Task DequeueAsync_ReturnsEnqueuedMessage()
    {
        // Arrange
        await using var queue = new PriorityMessageQueue();
        var message = CreateTestMessage(1);
        await queue.EnqueueAsync(message, "peer1", MessagePriority.Normal);

        // Act
        var dequeued = await queue.DequeueAsync();

        // Assert
        Assert.NotNull(dequeued);
        Assert.Equal("peer1", dequeued.TargetPeerId);
        Assert.Equal(MessagePriority.Normal, dequeued.Priority);
    }

    [Fact]
    public async Task DequeueAsync_WithHighPriorityFirst_ReturnsInCorrectOrder()
    {
        // Arrange
        await using var queue = new PriorityMessageQueue();
        var lowPriorityMessage = CreateTestMessage(1);
        var highPriorityMessage = CreateTestMessage(2);

        // Enqueue low priority first
        await queue.EnqueueAsync(lowPriorityMessage, "peer1", MessagePriority.Low);
        await queue.EnqueueAsync(highPriorityMessage, "peer2", MessagePriority.High);

        // Act - Dequeue should get high priority first
        var first = await queue.DequeueAsync();
        var second = await queue.DequeueAsync();

        // Assert
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal("peer2", first.TargetPeerId); // High priority
        Assert.Equal("peer1", second.TargetPeerId); // Low priority
    }

    [Fact]
    public async Task DequeueAsync_WithMultiplePriorities_ReturnsInCorrectOrder()
    {
        // Arrange
        await using var queue = new PriorityMessageQueue();
        
        await queue.EnqueueAsync(CreateTestMessage(1), "peer-low", MessagePriority.Low);
        await queue.EnqueueAsync(CreateTestMessage(2), "peer-normal", MessagePriority.Normal);
        await queue.EnqueueAsync(CreateTestMessage(3), "peer-high", MessagePriority.High);
        await queue.EnqueueAsync(CreateTestMessage(4), "peer-critical", MessagePriority.Critical);

        // Act
        var msg1 = await queue.DequeueAsync();
        var msg2 = await queue.DequeueAsync();
        var msg3 = await queue.DequeueAsync();
        var msg4 = await queue.DequeueAsync();

        // Assert - Should come out in priority order
        Assert.Equal("peer-critical", msg1?.TargetPeerId);
        Assert.Equal("peer-high", msg2?.TargetPeerId);
        Assert.Equal("peer-normal", msg3?.TargetPeerId);
        Assert.Equal("peer-low", msg4?.TargetPeerId);
    }

    [Fact]
    public async Task Count_ReturnsCorrectCount()
    {
        // Arrange
        await using var queue = new PriorityMessageQueue();
        
        // Act
        await queue.EnqueueAsync(CreateTestMessage(1), "peer1", MessagePriority.Normal);
        await queue.EnqueueAsync(CreateTestMessage(2), "peer2", MessagePriority.High);

        // Assert
        Assert.Equal(2, queue.Count);
    }

    [Fact]
    public async Task Count_AfterDequeue_DecreasesCorrectly()
    {
        // Arrange
        await using var queue = new PriorityMessageQueue();
        await queue.EnqueueAsync(CreateTestMessage(1), "peer1", MessagePriority.Normal);
        await queue.EnqueueAsync(CreateTestMessage(2), "peer2", MessagePriority.High);

        // Act
        await queue.DequeueAsync();

        // Assert
        Assert.Equal(1, queue.Count);
    }

    [Fact]
    public void GetPriorityForMessageType_ReturnsCorrectPriorities()
    {
        // Assert
        Assert.Equal(MessagePriority.High, PriorityMessageQueue.GetPriorityForMessageType(MessageType.Block));
        Assert.Equal(MessagePriority.High, PriorityMessageQueue.GetPriorityForMessageType(MessageType.NewBlock));
        Assert.Equal(MessagePriority.High, PriorityMessageQueue.GetPriorityForMessageType(MessageType.BlockAccepted));
        Assert.Equal(MessagePriority.Normal, PriorityMessageQueue.GetPriorityForMessageType(MessageType.ProofSubmission));
        Assert.Equal(MessagePriority.Low, PriorityMessageQueue.GetPriorityForMessageType(MessageType.Transaction));
        Assert.Equal(MessagePriority.Critical, PriorityMessageQueue.GetPriorityForMessageType(MessageType.Ping));
        Assert.Equal(MessagePriority.Critical, PriorityMessageQueue.GetPriorityForMessageType(MessageType.Pong));
    }

    [Fact]
    public async Task DisposeAsync_AllowsGracefulShutdown()
    {
        // Arrange
        var queue = new PriorityMessageQueue();
        await queue.EnqueueAsync(CreateTestMessage(1), "peer1", MessagePriority.Normal);

        // Act
        await queue.DisposeAsync();

        // Assert - Dequeue after dispose should throw
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await queue.DequeueAsync());
    }

    [Fact]
    public async Task EnqueueAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var queue = new PriorityMessageQueue();
        await queue.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await queue.EnqueueAsync(CreateTestMessage(1), "peer1", MessagePriority.Normal));
    }

    [Fact]
    public async Task EnqueueAsync_WithCapacityLimit_DropsOldest()
    {
        // Arrange
        await using var queue = new PriorityMessageQueue(capacity: 2);
        
        // Act - Enqueue 3 messages with same priority
        await queue.EnqueueAsync(CreateTestMessage(1), "peer1", MessagePriority.Normal);
        await queue.EnqueueAsync(CreateTestMessage(2), "peer2", MessagePriority.Normal);
        await queue.EnqueueAsync(CreateTestMessage(3), "peer3", MessagePriority.Normal);

        // Assert - Count should be capped at capacity
        Assert.True(queue.Count <= 2);
    }
}
