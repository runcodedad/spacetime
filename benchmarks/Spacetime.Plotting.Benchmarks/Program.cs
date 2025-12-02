using BenchmarkDotNet.Running;
using Spacetime.Plotting.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
