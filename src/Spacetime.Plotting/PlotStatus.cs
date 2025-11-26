namespace Spacetime.Plotting;

/// <summary>
/// Represents the status of a plot file.
/// </summary>
public enum PlotStatus
{
    /// <summary>
    /// The plot file is valid and can be used for proof generation.
    /// </summary>
    Valid,

    /// <summary>
    /// The plot file exists but is corrupted or has invalid data.
    /// </summary>
    Corrupted,

    /// <summary>
    /// The plot file is missing from the file system.
    /// </summary>
    Missing
}
