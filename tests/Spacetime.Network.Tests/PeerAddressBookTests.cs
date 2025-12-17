using System.Net;

namespace Spacetime.Network.Tests;

public class PeerAddressBookTests
{
    private static PeerAddress CreateTestAddress(string ip = "203.0.113.100", int port = 8000, string source = "test")
    {
        return new PeerAddress(new IPEndPoint(IPAddress.Parse(ip), port), source);
    }

    [Fact]
    public void Constructor_WithDefaultParameters_CreatesInstance()
    {
        // Act
        var book = new PeerAddressBook();

        // Assert
        Assert.Equal(0, book.Count);
    }

    [Fact]
    public void Constructor_WithInvalidMaxAddresses_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PeerAddressBook(maxAddresses: 0));
    }

    [Fact]
    public void AddAddress_WithValidAddress_AddsSuccessfully()
    {
        // Arrange
        var book = new PeerAddressBook();
        var address = CreateTestAddress();

        // Act
        var result = book.AddAddress(address);

        // Assert
        Assert.True(result);
        Assert.Equal(1, book.Count);
    }

    [Fact]
    public void AddAddress_WithDuplicateAddress_ReturnsFalse()
    {
        // Arrange
        var book = new PeerAddressBook();
        var address = CreateTestAddress();
        book.AddAddress(address);

        // Act
        var result = book.AddAddress(address);

        // Assert
        Assert.False(result);
        Assert.Equal(1, book.Count);
    }

    [Fact]
    public void AddAddress_WithNullAddress_ThrowsArgumentNullException()
    {
        // Arrange
        var book = new PeerAddressBook();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => book.AddAddress(null!));
    }

    [Fact]
    public void AddAddress_WithPrivateAddress_WhenNotAllowed_ReturnsFalse()
    {
        // Arrange
        var book = new PeerAddressBook(allowPrivateAddresses: false);
        var address = CreateTestAddress("192.168.1.100"); // Private address

        // Act
        var result = book.AddAddress(address);

        // Assert
        Assert.False(result);
        Assert.Equal(0, book.Count);
    }

    [Fact]
    public void AddAddress_WithPrivateAddress_WhenAllowed_AddsSuccessfully()
    {
        // Arrange
        var book = new PeerAddressBook(allowPrivateAddresses: true);
        var address = CreateTestAddress("192.168.1.100"); // Private address

        // Act
        var result = book.AddAddress(address);

        // Assert
        Assert.True(result);
        Assert.Equal(1, book.Count);
    }

    [Fact]
    public void AddAddress_WithLoopbackAddress_WhenNotAllowed_ReturnsFalse()
    {
        // Arrange
        var book = new PeerAddressBook(allowPrivateAddresses: false);
        var address = CreateTestAddress("127.0.0.1"); // Loopback

        // Act
        var result = book.AddAddress(address);

        // Assert
        Assert.False(result);
        Assert.Equal(0, book.Count);
    }

    [Fact]
    public void AddAddress_ExceedingMaxAddresses_EvictsLowestQuality()
    {
        // Arrange
        var book = new PeerAddressBook(maxAddresses: 2);
        var address1 = CreateTestAddress("203.0.113.1", 8000);
        var address2 = CreateTestAddress("203.0.113.2", 8000);
        var address3 = CreateTestAddress("203.0.113.3", 8000);

        // Add two addresses
        book.AddAddress(address1);
        book.AddAddress(address2);

        // Make address2 higher quality
        book.RecordSuccess(address2.EndPoint);

        // Act
        var result = book.AddAddress(address3);

        // Assert
        Assert.True(result);
        Assert.Equal(2, book.Count);
        Assert.Null(book.GetAddress(address1.EndPoint)); // Lowest quality evicted
    }

    [Fact]
    public void AddAddress_ExceedingSubnetLimit_ReturnsFalse()
    {
        // Arrange
        var book = new PeerAddressBook(maxAddressesPerSubnet: 2);
        var address1 = CreateTestAddress("203.0.113.1", 8000);
        var address2 = CreateTestAddress("203.0.113.2", 8001);
        var address3 = CreateTestAddress("203.0.113.3", 8002); // Same /24 subnet

        book.AddAddress(address1);
        book.AddAddress(address2);

        // Act
        var result = book.AddAddress(address3);

        // Assert
        Assert.False(result);
        Assert.Equal(2, book.Count);
    }

    [Fact]
    public void RemoveAddress_WithExistingAddress_RemovesSuccessfully()
    {
        // Arrange
        var book = new PeerAddressBook();
        var address = CreateTestAddress();
        book.AddAddress(address);

        // Act
        var result = book.RemoveAddress(address.EndPoint);

        // Assert
        Assert.True(result);
        Assert.Equal(0, book.Count);
    }

    [Fact]
    public void RemoveAddress_WithNonExistentAddress_ReturnsFalse()
    {
        // Arrange
        var book = new PeerAddressBook();
        var endPoint = new IPEndPoint(IPAddress.Parse("203.0.113.1"), 8000);

        // Act
        var result = book.RemoveAddress(endPoint);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetAddress_WithExistingAddress_ReturnsAddress()
    {
        // Arrange
        var book = new PeerAddressBook();
        var address = CreateTestAddress();
        book.AddAddress(address);

        // Act
        var result = book.GetAddress(address.EndPoint);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(address.EndPoint, result.EndPoint);
    }

    [Fact]
    public void GetAddress_WithNonExistentAddress_ReturnsNull()
    {
        // Arrange
        var book = new PeerAddressBook();
        var endPoint = new IPEndPoint(IPAddress.Parse("203.0.113.1"), 8000);

        // Act
        var result = book.GetAddress(endPoint);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void UpdateLastSeen_WithExistingAddress_UpdatesTimestamp()
    {
        // Arrange
        var book = new PeerAddressBook();
        var address = CreateTestAddress();
        book.AddAddress(address);
        var originalLastSeen = address.LastSeen;

        // Act
        Task.Delay(10).Wait();
        book.UpdateLastSeen(address.EndPoint);
        var updated = book.GetAddress(address.EndPoint);

        // Assert
        Assert.NotNull(updated);
        Assert.True(updated.LastSeen > originalLastSeen);
    }

    [Fact]
    public void RecordSuccess_WithExistingAddress_IncrementsSuccess()
    {
        // Arrange
        var book = new PeerAddressBook();
        var address = CreateTestAddress();
        book.AddAddress(address);

        // Act
        book.RecordSuccess(address.EndPoint);
        var updated = book.GetAddress(address.EndPoint);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(1, updated.SuccessCount);
        Assert.Equal(0, updated.FailureCount);
    }

    [Fact]
    public void RecordFailure_WithExistingAddress_IncrementsFailure()
    {
        // Arrange
        var book = new PeerAddressBook();
        var address = CreateTestAddress();
        book.AddAddress(address);

        // Act
        book.RecordFailure(address.EndPoint);
        var updated = book.GetAddress(address.EndPoint);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(0, updated.SuccessCount);
        Assert.Equal(1, updated.FailureCount);
    }

    [Fact]
    public void GetBestAddresses_ReturnsHighestQualityAddresses()
    {
        // Arrange
        var book = new PeerAddressBook();
        var address1 = CreateTestAddress("203.0.113.1", 8000);
        var address2 = CreateTestAddress("203.0.113.2", 8000);
        var address3 = CreateTestAddress("203.0.113.3", 8000);

        book.AddAddress(address1);
        book.AddAddress(address2);
        book.AddAddress(address3);

        // Make address2 have the best quality
        book.RecordSuccess(address2.EndPoint);
        book.RecordSuccess(address2.EndPoint);
        
        // Make address3 have medium quality
        book.RecordSuccess(address3.EndPoint);
        book.RecordFailure(address3.EndPoint);
        
        // Make address1 have worst quality
        book.RecordFailure(address1.EndPoint);

        // Act
        var best = book.GetBestAddresses(2);

        // Assert
        Assert.Equal(2, best.Count);
        // address2 has 100% quality, address3 has 50% quality, address1 has 0% quality
        Assert.Equal(address2.EndPoint, best[0].EndPoint);
        Assert.Equal(address3.EndPoint, best[1].EndPoint);
    }

    [Fact]
    public void GetBestAddresses_ExcludesSpecifiedEndPoints()
    {
        // Arrange
        var book = new PeerAddressBook();
        var address1 = CreateTestAddress("203.0.113.1", 8000);
        var address2 = CreateTestAddress("203.0.113.2", 8000);

        book.AddAddress(address1);
        book.AddAddress(address2);

        // Act
        var best = book.GetBestAddresses(2, new[] { address1.EndPoint });

        // Assert
        Assert.Single(best);
        Assert.Equal(address2.EndPoint, best[0].EndPoint);
    }

    [Fact]
    public void RemoveStaleAddresses_RemovesOldAddresses()
    {
        // Arrange
        var book = new PeerAddressBook();
        var address = CreateTestAddress();
        book.AddAddress(address);

        // Simulate old address by modifying (we need to use reflection or accept the limitation)
        // For this test, we'll use a very small maxAge
        Task.Delay(100).Wait();

        // Act
        var removed = book.RemoveStaleAddresses(TimeSpan.FromMilliseconds(50));

        // Assert
        Assert.Equal(1, removed);
        Assert.Equal(0, book.Count);
    }

    [Fact]
    public void RemovePoorQualityAddresses_RemovesLowQualityAddresses()
    {
        // Arrange
        var book = new PeerAddressBook();
        var address = CreateTestAddress();
        book.AddAddress(address);

        // Create poor quality: many failures
        for (int i = 0; i < 10; i++)
        {
            book.RecordFailure(address.EndPoint);
        }

        // Act
        var removed = book.RemovePoorQualityAddresses(minQualityScore: 0.2, minAttempts: 5);

        // Assert
        Assert.Equal(1, removed);
        Assert.Equal(0, book.Count);
    }

    [Fact]
    public void Clear_RemovesAllAddresses()
    {
        // Arrange
        var book = new PeerAddressBook();
        book.AddAddress(CreateTestAddress("203.0.113.1", 8000));
        book.AddAddress(CreateTestAddress("203.0.113.2", 8000));

        // Act
        book.Clear();

        // Assert
        Assert.Equal(0, book.Count);
    }

    [Fact]
    public void GetAllAddresses_ReturnsAllAddresses()
    {
        // Arrange
        var book = new PeerAddressBook();
        book.AddAddress(CreateTestAddress("203.0.113.1", 8000));
        book.AddAddress(CreateTestAddress("203.0.113.2", 8000));

        // Act
        var all = book.GetAllAddresses();

        // Assert
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task SaveAsync_WithoutPersistencePath_CompletesSuccessfully()
    {
        // Arrange
        var book = new PeerAddressBook();

        // Act & Assert
        await book.SaveAsync();
    }

    [Fact]
    public async Task LoadAsync_WithoutPersistencePath_CompletesSuccessfully()
    {
        // Arrange
        var book = new PeerAddressBook();

        // Act & Assert
        await book.LoadAsync();
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesAddresses()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_addressbook_{Guid.NewGuid()}.json");
        try
        {
            var book1 = new PeerAddressBook(persistencePath: tempPath, allowPrivateAddresses: true);
            var address1 = CreateTestAddress("192.168.1.1", 8000);
            var address2 = CreateTestAddress("192.168.1.2", 8001);
            
            book1.AddAddress(address1);
            book1.AddAddress(address2);
            book1.RecordSuccess(address1.EndPoint);

            // Act
            await book1.SaveAsync();

            var book2 = new PeerAddressBook(persistencePath: tempPath, allowPrivateAddresses: true);
            await book2.LoadAsync();

            // Assert
            Assert.Equal(2, book2.Count);
            var loaded1 = book2.GetAddress(address1.EndPoint);
            Assert.NotNull(loaded1);
            Assert.Equal(1, loaded1.SuccessCount);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
