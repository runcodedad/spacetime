using BenchmarkDotNet.Running;
using Spacetime.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
