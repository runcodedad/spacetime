namespace Spacetime.Plotting.Tests;

public class PlotMetadataTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var plotId = Guid.NewGuid();
        var filePath = "/path/to/plot.dat";
        var spaceAllocatedBytes = 1024L * 1024 * 1024; // 1GB
        var merkleRoot = new byte[32];
        var createdAtUtc = DateTime.UtcNow;
        var status = PlotStatus.Valid;

        // Act
        var metadata = new PlotMetadata(
            plotId,
            filePath,
            spaceAllocatedBytes,
            merkleRoot,
            createdAtUtc,
            status);

        // Assert
        Assert.Equal(plotId, metadata.PlotId);
        Assert.Equal(filePath, metadata.FilePath);
        Assert.Equal(spaceAllocatedBytes, metadata.SpaceAllocatedBytes);
        Assert.Equal(merkleRoot, metadata.MerkleRoot);
        Assert.Equal(createdAtUtc, metadata.CreatedAtUtc);
        Assert.Equal(status, metadata.Status);
    }

    [Fact]
    public void WithStatus_ReturnsNewInstanceWithUpdatedStatus()
    {
        // Arrange
        var original = new PlotMetadata(
            Guid.NewGuid(),
            "/path/to/plot.dat",
            1024 * 1024,
            new byte[32],
            DateTime.UtcNow,
            PlotStatus.Valid);

        // Act
        var updated = original.WithStatus(PlotStatus.Corrupted);

        // Assert
        Assert.NotSame(original, updated);
        Assert.Equal(PlotStatus.Corrupted, updated.Status);
        Assert.Equal(original.PlotId, updated.PlotId);
        Assert.Equal(original.FilePath, updated.FilePath);
        Assert.Equal(original.SpaceAllocatedBytes, updated.SpaceAllocatedBytes);
    }

    [Fact]
    public void WithStatus_PreservesOtherProperties()
    {
        // Arrange
        var plotId = Guid.NewGuid();
        var filePath = "/test/path.plot";
        var spaceAllocatedBytes = 5000L;
        var merkleRoot = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 };
        var createdAtUtc = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        var original = new PlotMetadata(
            plotId,
            filePath,
            spaceAllocatedBytes,
            merkleRoot,
            createdAtUtc,
            PlotStatus.Valid);

        // Act
        var updated = original.WithStatus(PlotStatus.Missing);

        // Assert
        Assert.Equal(plotId, updated.PlotId);
        Assert.Equal(filePath, updated.FilePath);
        Assert.Equal(spaceAllocatedBytes, updated.SpaceAllocatedBytes);
        Assert.Equal(merkleRoot, updated.MerkleRoot);
        Assert.Equal(createdAtUtc, updated.CreatedAtUtc);
        Assert.Equal(PlotStatus.Missing, updated.Status);
    }

    [Fact]
    public void Equality_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var plotId = Guid.NewGuid();
        var filePath = "/path/to/plot.dat";
        var spaceAllocatedBytes = 1024L;
        var merkleRoot = new byte[32];
        var createdAtUtc = DateTime.UtcNow;
        var status = PlotStatus.Valid;

        var metadata1 = new PlotMetadata(plotId, filePath, spaceAllocatedBytes, merkleRoot, createdAtUtc, status);
        var metadata2 = new PlotMetadata(plotId, filePath, spaceAllocatedBytes, merkleRoot, createdAtUtc, status);

        // Act & Assert
        Assert.Equal(metadata1, metadata2);
    }

    [Fact]
    public void Equality_WithDifferentPlotId_ReturnsFalse()
    {
        // Arrange
        var filePath = "/path/to/plot.dat";
        var spaceAllocatedBytes = 1024L;
        var merkleRoot = new byte[32];
        var createdAtUtc = DateTime.UtcNow;
        var status = PlotStatus.Valid;

        var metadata1 = new PlotMetadata(Guid.NewGuid(), filePath, spaceAllocatedBytes, merkleRoot, createdAtUtc, status);
        var metadata2 = new PlotMetadata(Guid.NewGuid(), filePath, spaceAllocatedBytes, merkleRoot, createdAtUtc, status);

        // Act & Assert
        Assert.NotEqual(metadata1, metadata2);
    }

    [Theory]
    [InlineData(PlotStatus.Valid)]
    [InlineData(PlotStatus.Corrupted)]
    [InlineData(PlotStatus.Missing)]
    public void Constructor_WithAllStatusValues_CreatesInstance(PlotStatus status)
    {
        // Arrange & Act
        var metadata = new PlotMetadata(
            Guid.NewGuid(),
            "/path/to/plot.dat",
            1024,
            new byte[32],
            DateTime.UtcNow,
            status);

        // Assert
        Assert.Equal(status, metadata.Status);
    }

    [Fact]
    public void FromPlotLoader_WithNullLoader_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PlotMetadata.FromPlotLoader(null!));
    }
}
