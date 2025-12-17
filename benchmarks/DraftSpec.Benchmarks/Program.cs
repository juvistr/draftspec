using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

// Run all benchmarks with standard config
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
    .Run(args, DefaultConfig.Instance.WithOptions(ConfigOptions.JoinSummary));