using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using DraftSpec.Benchmarks.Helpers;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Console;
using DraftSpec.Formatters.Html;
using DraftSpec.Formatters.Markdown;

namespace DraftSpec.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for formatter performance.
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
public class FormatterBenchmarks
{
    private SpecReport _smallReport = null!;
    private SpecReport _largeReport = null!;

    private ConsoleFormatter _consoleFormatter = null!;
    private HtmlFormatter _htmlFormatter = null!;
    private MarkdownFormatter _markdownFormatter = null!;

    [GlobalSetup]
    public void Setup()
    {
        var small = ReportGenerator.CreateTestData(10);
        var large = ReportGenerator.CreateTestData(500);

        _smallReport = SpecReportBuilder.Build(small.Context, small.Results);
        _largeReport = SpecReportBuilder.Build(large.Context, large.Results);

        _consoleFormatter = new ConsoleFormatter();
        _htmlFormatter = new HtmlFormatter();
        _markdownFormatter = new MarkdownFormatter();
    }

    // Console Formatter
    [Benchmark(Baseline = true), BenchmarkCategory("Console")]
    public void Console_Small()
    {
        using var writer = new StringWriter();
        _consoleFormatter.Format(_smallReport, writer, useColors: false);
    }

    [Benchmark, BenchmarkCategory("Console")]
    public void Console_Large()
    {
        using var writer = new StringWriter();
        _consoleFormatter.Format(_largeReport, writer, useColors: false);
    }

    // HTML Formatter
    [Benchmark(Baseline = true), BenchmarkCategory("Html")]
    public string Html_Small()
        => _htmlFormatter.Format(_smallReport);

    [Benchmark, BenchmarkCategory("Html")]
    public string Html_Large()
        => _htmlFormatter.Format(_largeReport);

    // Markdown Formatter
    [Benchmark(Baseline = true), BenchmarkCategory("Markdown")]
    public string Markdown_Small()
        => _markdownFormatter.Format(_smallReport);

    [Benchmark, BenchmarkCategory("Markdown")]
    public string Markdown_Large()
        => _markdownFormatter.Format(_largeReport);
}
