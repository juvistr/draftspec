using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using DraftSpec.Benchmarks.Helpers;

namespace DraftSpec.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for SpecRunner execution performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
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
    public List<SpecResult> SmallTree_10Specs()
        => _runner.Run(_smallTree);

    [Benchmark]
    public List<SpecResult> MediumTree_100Specs()
        => _runner.Run(_mediumTree);

    [Benchmark]
    public List<SpecResult> LargeTree_1000Specs()
        => _runner.Run(_largeTree);

    [Benchmark]
    public List<SpecResult> DeepTree_50Levels()
        => _runner.Run(_deepTree);

    [Benchmark]
    [Arguments(2)]
    [Arguments(4)]
    public List<SpecResult> LargeTree_Parallel(int parallelism)
    {
        var runner = new SpecRunner([], null, parallelism);
        return runner.Run(_largeTree);
    }
}
