using System.CommandLine;
using MerkleTree.Hashing;
using Spacetime.Miner;
using Spacetime.Miner.Commands;

var hashFunction = new Sha256HashFunction();
var configurationLoader = new ConfigurationLoader();

var rootCommand = new RootCommand("Spacetime Miner - Proof-of-Space-Time blockchain miner")
{
    new CreatePlotCommand(hashFunction, configurationLoader),
    new ListPlotsCommand(hashFunction, configurationLoader),
    new DeletePlotCommand(hashFunction, configurationLoader),
    new StartCommand(configurationLoader),
    new StopCommand(),
    new StatusCommand(hashFunction, configurationLoader)
};

return await rootCommand.InvokeAsync(args);
