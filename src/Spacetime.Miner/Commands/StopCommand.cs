using System.CommandLine;

namespace Spacetime.Miner.Commands;

/// <summary>
/// CLI command to stop the mining process.
/// </summary>
public sealed class StopCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StopCommand"/> class.
    /// </summary>
    public StopCommand() : base("stop", "Stop mining")
    {
        this.SetHandler(() => Execute());
    }

    private static int Execute()
    {
        try
        {
            Console.WriteLine("Stop command is not yet implemented.");
            Console.WriteLine("To stop a running miner, use Ctrl+C in the terminal where it is running.");
            Console.WriteLine();
            Console.WriteLine("In a future release, this command will:");
            Console.WriteLine("  - Send a stop signal to a running miner daemon");
            Console.WriteLine("  - Wait for graceful shutdown");
            Console.WriteLine("  - Report final mining statistics");
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
