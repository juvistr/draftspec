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
    public IList<SpecResult> SmallTree_10Specs()
    {
        return _runner.Run(_smallTree);
    }

    [Benchmark]
    public IList<SpecResult> MediumTree_100Specs()
    {
        return _runner.Run(_mediumTree);
    }

    [Benchmark]
    public IList<SpecResult> LargeTree_1000Specs()
    {
        return _runner.Run(_largeTree);
    }

    [Benchmark]
    public IList<SpecResult> DeepTree_50Levels()
    {
        return _runner.Run(_deepTree);
    }

    [Benchmark]
    [Arguments(2)]
    [Arguments(4)]
    public IList<SpecResult> LargeTree_Parallel(int parallelism)
    {
        var runner = new SpecRunner([], null, parallelism);
        return runner.Run(_largeTree);
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
    public IList<SpecResult> AsyncSpecs_Sequential()
    {
        var runner = new SpecRunner([], null, 1);
        return runner.Run(_asyncTree);
    }

    [Benchmark]
    [Arguments(2)]
    [Arguments(4)]
    [Arguments(8)]
    public IList<SpecResult> AsyncSpecs_Parallel(int parallelism)
    {
        var runner = new SpecRunner([], null, parallelism);
        return runner.Run(_asyncTree);
    }
}
