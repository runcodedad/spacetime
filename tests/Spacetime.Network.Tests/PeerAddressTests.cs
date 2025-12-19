using System.Net;

namespace Spacetime.Network.Tests;

public class PeerAddressTests
{
    private static IPEndPoint CreateTestEndPoint(string ip = "192.168.1.100", int port = 8000)
    {
        return new IPEndPoint(IPAddress.Parse(ip), port);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var endPoint = CreateTestEndPoint();

        // Act
        var address = new PeerAddress(endPoint, "test");

        // Assert
        Assert.Equal(endPoint, address.EndPoint);
        Assert.Equal("test", address.Source);
        Assert.Equal(0, address.SuccessCount);
        Assert.Equal(0, address.FailureCount);
        Assert.True(address.FirstSeen <= DateTimeOffset.UtcNow);
        Assert.True(address.LastSeen <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Constructor_WithNullEndPoint_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PeerAddress(null!, "test"));
    }

    [Fact]
    public void Constructor_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        var endPoint = CreateTestEndPoint();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PeerAddress(endPoint, null!));
    }

    [Fact]
    public void WithUpdatedLastSeen_UpdatesTimestamp()
    {
        // Arrange
        var address = new PeerAddress(CreateTestEndPoint(), "test");
        var originalLastSeen = address.LastSeen;

        // Act
        Task.Delay(10).Wait(); // Ensure time passes
        var updated = address.WithUpdatedLastSeen();

        // Assert
        Assert.True(updated.LastSeen > originalLastSeen);
        Assert.Equal(address.EndPoint, updated.EndPoint);
        Assert.Equal(address.Source, updated.Source);
    }

    [Fact]
    public void WithUpdatedLastAttempt_UpdatesTimestamp()
    {
        // Arrange
        var address = new PeerAddress(CreateTestEndPoint(), "test");

        // Act
        var updated = address.WithUpdatedLastAttempt();

        // Assert
        Assert.True(updated.LastAttempt > DateTimeOffset.MinValue);
        Assert.Equal(address.EndPoint, updated.EndPoint);
    }

    [Fact]
    public void WithRecordedSuccess_IncrementsSuccessCountAndResetsFailures()
    {
        // Arrange
        var address = new PeerAddress(CreateTestEndPoint(), "test");
        address = address.WithRecordedFailure();
        address = address.WithRecordedFailure();

        // Act
        var updated = address.WithRecordedSuccess();

        // Assert
        Assert.Equal(1, updated.SuccessCount);
        Assert.Equal(0, updated.FailureCount);
    }

    [Fact]
    public void WithRecordedFailure_IncrementsFailureCount()
    {
        // Arrange
        var address = new PeerAddress(CreateTestEndPoint(), "test");

        // Act
        var updated = address.WithRecordedFailure();
        updated = updated.WithRecordedFailure();

        // Assert
        Assert.Equal(0, updated.SuccessCount);
        Assert.Equal(2, updated.FailureCount);
    }

    [Fact]
    public void QualityScore_WithNoAttempts_ReturnsNeutralScore()
    {
        // Arrange
        var address = new PeerAddress(CreateTestEndPoint(), "test");

        // Act
        var score = address.QualityScore;

        // Assert
        Assert.Equal(0.5, score);
    }

    [Fact]
    public void QualityScore_WithAllSuccesses_ReturnsOne()
    {
        // Arrange
        var address = new PeerAddress(CreateTestEndPoint(), "test");
        address = address.WithRecordedSuccess();
        address = address.WithRecordedSuccess();
        address = address.WithRecordedSuccess();

        // Act
        var score = address.QualityScore;

        // Assert
        Assert.Equal(1.0, score);
    }

    [Fact]
    public void QualityScore_WithMixedResults_ReturnsCorrectRatio()
    {
        // Arrange
        var address = new PeerAddress(CreateTestEndPoint(), "test");
        address = address.WithRecordedSuccess();
        address = address.WithRecordedSuccess();
        address = address.WithRecordedSuccess();
        address = address.WithRecordedFailure();

        // Act
        var score = address.QualityScore;

        // Assert
        Assert.Equal(0.75, score); // 3 successes / 4 total
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var endPoint = CreateTestEndPoint();
        var address = new PeerAddress(endPoint, "test");

        // Act
        var result = address.ToString();

        // Assert
        Assert.Contains(endPoint.ToString(), result);
        Assert.Contains("test", result);
        Assert.Contains("quality", result);
    }
}
