namespace Spacetime.Common;

/// <summary>
/// Console-based progress reporter.
/// </summary>
/// <param name="message">The message to display alongside progress.</param>
public sealed class ConsoleProgressReporter(string message) : IProgress<double>
{
    private int _lastPercent = -1;

    public void Report(double value)
    {
        var percent = (int)(value * 100);
        if (percent != _lastPercent && percent % 5 == 0)
        {
            Console.Write($"\r{message}: {percent}%");
            _lastPercent = percent;
        }

        if (percent >= 100)
        {
            Console.WriteLine();
        }
    }
}
