namespace Spacetime.Plotting
{
    /// <summary>
    /// Event arguments containing the affected <see cref="PlotMetadata"/>.
    /// </summary>
    public sealed class PlotChangedEventArgs : EventArgs
    {
        public PlotMetadata Metadata { get; }

        public PlotChangedEventArgs(PlotMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(metadata);
            Metadata = metadata;
        }
    }
}
