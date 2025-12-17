using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using DraftSpec.Benchmarks.Helpers;

namespace DraftSpec.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for SpecReportBuilder.Build() performance.
/// </summary>
[MemoryDiagnoser]
public class ReportBuilderBenchmarks
{
    private (SpecContext Context, List<SpecResult> Results) _small;
    private (SpecContext Context, List<SpecResult> Results) _medium;
    private (SpecContext Context, List<SpecResult> Results) _large;

    [GlobalSetup]
    public void Setup()
    {
        _small = ReportGenerator.CreateTestData(10);
        _medium = ReportGenerator.CreateTestData(100);
        _large = ReportGenerator.CreateTestData(1000);
    }

    [Benchmark(Baseline = true)]
    public Formatters.SpecReport Small_10Specs()
    {
        return SpecReportBuilder.Build(_small.Context, _small.Results);
    }

    [Benchmark]
    public Formatters.SpecReport Medium_100Specs()
    {
        return SpecReportBuilder.Build(_medium.Context, _medium.Results);
    }

    [Benchmark]
    public Formatters.SpecReport Large_1000Specs()
    {
        return SpecReportBuilder.Build(_large.Context, _large.Results);
    }
}