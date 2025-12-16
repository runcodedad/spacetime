namespace Spacetime.Network.Tests;

public class MessageTrackerTests
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
    public void MarkAndCheckIfNew_WithNewMessage_ReturnsTrue()
    {
        // Arrange
        var tracker = new MessageTracker();
        var message = CreateTestMessage(1);

        // Act
        var result = tracker.MarkAndCheckIfNew(message);

        // Assert
        Assert.True(result);
        Assert.Equal(1, tracker.TrackedMessageCount);
    }

    [Fact]
    public void MarkAndCheckIfNew_WithDuplicateMessage_ReturnsFalse()
    {
        // Arrange
        var tracker = new MessageTracker();
        var message = CreateTestMessage(1);
        tracker.MarkAndCheckIfNew(message);

        // Act
        var result = tracker.MarkAndCheckIfNew(message);

        // Assert
        Assert.False(result);
        Assert.Equal(1, tracker.TrackedMessageCount);
    }

    [Fact]
    public void MarkAndCheckIfNew_WithDifferentMessages_ReturnsTrueForEach()
    {
        // Arrange
        var tracker = new MessageTracker();
        var message1 = CreateTestMessage(1);
        var message2 = CreateTestMessage(2);

        // Act
        var result1 = tracker.MarkAndCheckIfNew(message1);
        var result2 = tracker.MarkAndCheckIfNew(message2);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.Equal(2, tracker.TrackedMessageCount);
    }

    [Fact]
    public void MarkAndCheckIfNew_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var tracker = new MessageTracker();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tracker.MarkAndCheckIfNew(null!));
    }

    [Fact]
    public void HasSeen_WithSeenMessage_ReturnsTrue()
    {
        // Arrange
        var tracker = new MessageTracker();
        var message = CreateTestMessage(1);
        tracker.MarkAndCheckIfNew(message);

        // Act
        var result = tracker.HasSeen(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasSeen_WithUnseenMessage_ReturnsFalse()
    {
        // Arrange
        var tracker = new MessageTracker();
        var message = CreateTestMessage(1);

        // Act
        var result = tracker.HasSeen(message);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasSeen_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var tracker = new MessageTracker();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tracker.HasSeen(null!));
    }

    [Fact]
    public void Clear_RemovesAllTrackedMessages()
    {
        // Arrange
        var tracker = new MessageTracker();
        var message1 = CreateTestMessage(1);
        var message2 = CreateTestMessage(2);
        tracker.MarkAndCheckIfNew(message1);
        tracker.MarkAndCheckIfNew(message2);

        // Act
        tracker.Clear();

        // Assert
        Assert.Equal(0, tracker.TrackedMessageCount);
        Assert.True(tracker.MarkAndCheckIfNew(message1)); // Should be new again
    }

    [Fact]
    public async Task MarkAndCheckIfNew_AfterMessageLifetime_ReturnsTrueForSameMessage()
    {
        // Arrange
        var lifetime = TimeSpan.FromMilliseconds(100);
        var tracker = new MessageTracker(messageLifetime: lifetime);
        var message = CreateTestMessage(1);
        tracker.MarkAndCheckIfNew(message);

        // Act
        await Task.Delay(150); // Wait for message to expire
        var result = tracker.MarkAndCheckIfNew(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MarkAndCheckIfNew_HandlesMaxCapacity_RemovesOldestEntries()
    {
        // Arrange
        var tracker = new MessageTracker(maxTrackedMessages: 10);

        // Act - Add more than max capacity
        for (int i = 0; i < 20; i++)
        {
            var message = CreateTestMessage(i);
            tracker.MarkAndCheckIfNew(message);
        }

        // Assert - Should not exceed max capacity significantly
        Assert.True(tracker.TrackedMessageCount <= 10);
    }

    [Fact]
    public void MarkAndCheckIfNew_WithSameMessageType_ButDifferentPayload_ReturnsTrueForEach()
    {
        // Arrange
        var tracker = new MessageTracker();
        var message1 = CreateTestMessage(1);
        var message2 = CreateTestMessage(2);

        // Act
        var result1 = tracker.MarkAndCheckIfNew(message1);
        var result2 = tracker.MarkAndCheckIfNew(message2);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
    }
}
