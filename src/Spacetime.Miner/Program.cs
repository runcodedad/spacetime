using System.CommandLine;
using Spacetime.Miner.Commands;

var rootCommand = new RootCommand("Spacetime Miner - Proof-of-Space-Time blockchain miner")
{
    new CreatePlotCommand(),
    new ListPlotsCommand(),
    new DeletePlotCommand(),
    new StartCommand(),
    new StopCommand(),
    new StatusCommand()
};

return await rootCommand.InvokeAsync(args);
