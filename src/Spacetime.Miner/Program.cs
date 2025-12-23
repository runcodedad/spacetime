using System.CommandLine;
using MerkleTree.Hashing;
using Spacetime.Miner.Commands;

var hashFunction = new Sha256HashFunction();

var rootCommand = new RootCommand("Spacetime Miner - Proof-of-Space-Time blockchain miner")
{
    new CreatePlotCommand(hashFunction),
    new ListPlotsCommand(hashFunction),
    new DeletePlotCommand(hashFunction),
    new StartCommand(),
    new StopCommand(),
    new StatusCommand(hashFunction)
};

return await rootCommand.InvokeAsync(args);
