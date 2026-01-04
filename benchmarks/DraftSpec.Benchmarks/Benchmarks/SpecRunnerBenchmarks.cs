using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using DraftSpec.Benchmarks.Helpers;

namespace DraftSpec.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for SpecRunner execution performance.
/// </summary>
[MemoryDiagnoser]
public class SpecRunnerBenchmarks
{
    private SpecContext _smallTree = null!;
    private SpecContext _mediumTree = null!;
    private SpecContext _largeTree = null!;
    private SpecContext _deepTree = null!;
    private SpecRunner _runner = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallTree = SpecTreeGenerator.CreateBalancedTree(10);
        _mediumTree = SpecTreeGenerator.CreateBalancedTree(100);
        _largeTree = SpecTreeGenerator.CreateBalancedTree(1000);
        _deepTree = SpecTreeGenerator.CreateDeepTree(50);
        _runner = new SpecRunner();
    }

    [Benchmark(Baseline = true)]
    public async Task<IList<SpecResult>> SmallTree_10Specs()
    {
        return await _runner.RunAsync(_smallTree);
    }

    [Benchmark]
    public async Task<IList<SpecResult>> MediumTree_100Specs()
    {
        return await _runner.RunAsync(_mediumTree);
    }

    [Benchmark]
    public async Task<IList<SpecResult>> LargeTree_1000Specs()
    {
        return await _runner.RunAsync(_largeTree);
    }

    [Benchmark]
    public async Task<IList<SpecResult>> DeepTree_50Levels()
    {
        return await _runner.RunAsync(_deepTree);
    }

    [Benchmark]
    [Arguments(2)]
    [Arguments(4)]
    public async Task<IList<SpecResult>> LargeTree_Parallel(int parallelism)
    {
        var runner = new SpecRunner([], null, parallelism);
        return await runner.RunAsync(_largeTree);
    }

    // --- Async benchmarks to demonstrate parallelism benefit ---
    // 20 specs × 10ms delay each = 200ms sequential, ~50ms with parallelism=4

    private SpecContext _asyncTree = null!;

    [GlobalSetup(Target = nameof(AsyncSpecs_Sequential))]
    public void SetupAsync()
    {
        _asyncTree = SpecTreeGenerator.CreateAsyncTree(20, 10);
    }

    [GlobalSetup(Target = nameof(AsyncSpecs_Parallel))]
    public void SetupAsyncParallel()
    {
        _asyncTree = SpecTreeGenerator.CreateAsyncTree(20, 10);
    }

    [Benchmark]
    public async Task<IList<SpecResult>> AsyncSpecs_Sequential()
    {
        var runner = new SpecRunner([], null, 1);
        return await runner.RunAsync(_asyncTree);
    }

    [Benchmark]
    [Arguments(2)]
    [Arguments(4)]
    [Arguments(8)]
    public async Task<IList<SpecResult>> AsyncSpecs_Parallel(int parallelism)
    {
        var runner = new SpecRunner([], null, parallelism);
        return await runner.RunAsync(_asyncTree);
    }
}
