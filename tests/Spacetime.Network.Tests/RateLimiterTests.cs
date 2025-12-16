namespace Spacetime.Network.Tests;

public class RateLimiterTests
{
    [Fact]
    public void TryConsume_WithAvailableTokens_ReturnsTrue()
    {
        // Arrange
        var rateLimiter = new RateLimiter(maxTokens: 10);

        // Act
        var result = rateLimiter.TryConsume("peer1", tokens: 1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryConsume_ExceedingMaxTokens_ReturnsFalse()
    {
        // Arrange
        var rateLimiter = new RateLimiter(maxTokens: 10);

        // Act - Consume all tokens
        for (int i = 0; i < 10; i++)
        {
            rateLimiter.TryConsume("peer1", tokens: 1);
        }
        var result = rateLimiter.TryConsume("peer1", tokens: 1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryConsume_WithMultipleTokens_ConsumesCorrectly()
    {
        // Arrange
        var rateLimiter = new RateLimiter(maxTokens: 10);

        // Act
        var result1 = rateLimiter.TryConsume("peer1", tokens: 5);
        var result2 = rateLimiter.TryConsume("peer1", tokens: 5);
        var result3 = rateLimiter.TryConsume("peer1", tokens: 1);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.False(result3);
    }

    [Fact]
    public void TryConsume_WithNullPeerId_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new RateLimiter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => rateLimiter.TryConsume(null!));
    }

    [Fact]
    public void TryConsume_DifferentPeers_IndependentLimits()
    {
        // Arrange
        var rateLimiter = new RateLimiter(maxTokens: 10);

        // Act
        var result1 = rateLimiter.TryConsume("peer1", tokens: 10);
        var result2 = rateLimiter.TryConsume("peer2", tokens: 10);
        var result3 = rateLimiter.TryConsume("peer1", tokens: 1);

        // Assert
        Assert.True(result1); // peer1 has tokens
        Assert.True(result2); // peer2 has tokens
        Assert.False(result3); // peer1 exhausted
    }

    [Fact]
    public void GetAvailableTokens_ReturnsCorrectCount()
    {
        // Arrange
        var rateLimiter = new RateLimiter(maxTokens: 10);
        rateLimiter.TryConsume("peer1", tokens: 3);

        // Act
        var available = rateLimiter.GetAvailableTokens("peer1");

        // Assert
        Assert.Equal(7, available);
    }

    [Fact]
    public void GetAvailableTokens_ForNewPeer_ReturnsMaxTokens()
    {
        // Arrange
        var rateLimiter = new RateLimiter(maxTokens: 10);

        // Act
        var available = rateLimiter.GetAvailableTokens("peer1");

        // Assert
        Assert.Equal(10, available);
    }

    [Fact]
    public void GetAvailableTokens_WithNullPeerId_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new RateLimiter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => rateLimiter.GetAvailableTokens(null!));
    }

    [Fact]
    public async Task TryConsume_AfterRefillInterval_RefillsTokens()
    {
        // Arrange
        var refillInterval = TimeSpan.FromMilliseconds(100);
        var rateLimiter = new RateLimiter(maxTokens: 10, refillInterval: refillInterval, refillAmount: 5);
        
        // Consume all tokens
        rateLimiter.TryConsume("peer1", tokens: 10);
        Assert.False(rateLimiter.TryConsume("peer1", tokens: 1));

        // Act - Wait for refill
        await Task.Delay(150);
        var result = rateLimiter.TryConsume("peer1", tokens: 5);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RemovePeer_RemovesPeerTracking()
    {
        // Arrange
        var rateLimiter = new RateLimiter(maxTokens: 10);
        rateLimiter.TryConsume("peer1", tokens: 5);

        // Act
        rateLimiter.RemovePeer("peer1");
        var available = rateLimiter.GetAvailableTokens("peer1");

        // Assert
        Assert.Equal(10, available); // Should be back to max for new peer
    }

    [Fact]
    public void RemovePeer_WithNullPeerId_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new RateLimiter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => rateLimiter.RemovePeer(null!));
    }

    [Fact]
    public void Clear_RemovesAllPeerTracking()
    {
        // Arrange
        var rateLimiter = new RateLimiter(maxTokens: 10);
        rateLimiter.TryConsume("peer1", tokens: 5);
        rateLimiter.TryConsume("peer2", tokens: 3);

        // Act
        rateLimiter.Clear();

        // Assert
        Assert.Equal(10, rateLimiter.GetAvailableTokens("peer1"));
        Assert.Equal(10, rateLimiter.GetAvailableTokens("peer2"));
    }
}
