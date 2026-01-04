using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Integration tests for RunCommand with full pipeline execution.
/// These tests verify end-to-end behavior using mock infrastructure.
/// For unit tests of individual phases, see the respective *PhaseTests.cs files.
/// </summary>
public class RunCommandIntegrationTests
{
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;
    private CommandContext? _capturedContext;

    [Before(Test)]
    public void SetUp()
    {
        _console = new MockConsole();
        _fileSystem = new MockFileSystem();
        _capturedContext = null;
    }

    private RunCommand CreateCommand(int pipelineReturnValue = 0)
    {
        return new RunCommand(MockPipeline, _console, _fileSystem);

        Task<int> MockPipeline(CommandContext context, CancellationToken ct)
        {
            _capturedContext = context;
            return Task.FromResult(pipelineReturnValue);
        }
    }

    #region Coverage Options Wiring

    [Test]
    public async Task ExecuteAsync_SetsCoverageOptions()
    {
        var command = CreateCommand();
        var coverage = new CoverageOptions
        {
            Enabled = true,
            Output = "./coverage",
            Format = CoverageFormat.Cobertura
        };
        var options = new RunOptions { Coverage = coverage };

        await command.ExecuteAsync(options);

        var capturedCoverage = _capturedContext!.Get<CoverageOptions>(ContextKeys.Coverage);
        await Assert.That(capturedCoverage).IsNotNull();
        await Assert.That(capturedCoverage!.Enabled).IsTrue();
        await Assert.That(capturedCoverage.OutputDirectory).IsEqualTo("./coverage");
        await Assert.That(capturedCoverage.Format).IsEqualTo(CoverageFormat.Cobertura);
    }

    #endregion

    #region Output Format Variations

    [Test]
    public async Task ExecuteAsync_SetsOutputFormat_Html()
    {
        var command = CreateCommand();
        var options = new RunOptions { Format = OutputFormat.Html };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.OutputFormat))
            .IsEqualTo("html");
    }

    [Test]
    public async Task ExecuteAsync_SetsOutputFormat_Markdown()
    {
        var command = CreateCommand();
        var options = new RunOptions { Format = OutputFormat.Markdown };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.OutputFormat))
            .IsEqualTo("markdown");
    }

    [Test]
    public async Task ExecuteAsync_SetsOutputFormat_JUnit()
    {
        var command = CreateCommand();
        var options = new RunOptions { Format = OutputFormat.JUnit };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.OutputFormat))
            .IsEqualTo("junit");
    }

    #endregion

    #region Filter Combinations

    [Test]
    public async Task ExecuteAsync_SetsFilter_WithAllFields()
    {
        var command = CreateCommand();
        var filter = new FilterOptions
        {
            FilterTags = "smoke,unit",
            ExcludeTags = "slow",
            FilterName = "pattern",
            ExcludeName = "exclude",
            FilterContext = ["Context1", "Context2"],
            ExcludeContext = ["Excluded"]
        };
        var options = new RunOptions { Filter = filter };

        await command.ExecuteAsync(options);

        var capturedFilter = _capturedContext!.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(capturedFilter!.FilterTags).IsEqualTo("smoke,unit");
        await Assert.That(capturedFilter.ExcludeTags).IsEqualTo("slow");
        await Assert.That(capturedFilter.FilterName).IsEqualTo("pattern");
        await Assert.That(capturedFilter.ExcludeName).IsEqualTo("exclude");
        var filterContext = capturedFilter.FilterContext;
        var excludeContext = capturedFilter.ExcludeContext;
        await Assert.That(filterContext).IsNotNull();
        await Assert.That(filterContext!).Contains("Context1");
        await Assert.That(filterContext).Contains("Context2");
        await Assert.That(excludeContext).IsNotNull();
        await Assert.That(excludeContext!).Contains("Excluded");
    }

    #endregion

    #region Partition Variations

    [Test]
    public async Task ExecuteAsync_SetsPartition_NotEnabled()
    {
        var command = CreateCommand();
        var partition = new PartitionOptions();
        var options = new RunOptions { Partition = partition };

        await command.ExecuteAsync(options);

        var capturedPartition = _capturedContext!.Get<PartitionOptions>(ContextKeys.Partition);
        await Assert.That(capturedPartition!.IsEnabled).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_SetsPartition_Enabled()
    {
        var command = CreateCommand();
        var partition = new PartitionOptions
        {
            Total = 10,
            Index = 5,
            Strategy = PartitionStrategy.SpecCount
        };
        var options = new RunOptions { Partition = partition };

        await command.ExecuteAsync(options);

        var capturedPartition = _capturedContext!.Get<PartitionOptions>(ContextKeys.Partition);
        await Assert.That(capturedPartition!.IsEnabled).IsTrue();
        await Assert.That(capturedPartition.Total).IsEqualTo(10);
        await Assert.That(capturedPartition.Index).IsEqualTo(5);
        await Assert.That(capturedPartition.Strategy).IsEqualTo(PartitionStrategy.SpecCount);
    }

    #endregion

    #region Impact Analysis Variations

    [Test]
    public async Task ExecuteAsync_SetsAffectedBy_Staged()
    {
        var command = CreateCommand();
        var options = new RunOptions { AffectedBy = "staged" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.AffectedBy))
            .IsEqualTo("staged");
    }

    [Test]
    public async Task ExecuteAsync_SetsAffectedBy_WithDryRun()
    {
        var command = CreateCommand();
        var options = new RunOptions
        {
            AffectedBy = "main",
            DryRun = true
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.AffectedBy))
            .IsEqualTo("main");
        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.DryRun)).IsTrue();
    }

    #endregion

    #region All Options Combined

    [Test]
    public async Task ExecuteAsync_SetsAllOptions()
    {
        var command = CreateCommand();
        var options = new RunOptions
        {
            Path = "/test/specs",
            Format = OutputFormat.Json,
            OutputFile = "results.json",
            CssUrl = "style.css",
            Parallel = true,
            NoCache = true,
            NoStats = false,
            StatsOnly = false,
            Filter = new FilterOptions { FilterTags = "smoke" },
            Coverage = new CoverageOptions { Enabled = true },
            Partition = new PartitionOptions { Total = 2, Index = 1 },
            AffectedBy = "HEAD",
            DryRun = false,
            Quarantine = true,
            NoHistory = true,
            Interactive = false
        };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Path).IsEqualTo("/test/specs");
        await Assert.That(_capturedContext.Get<string>(ContextKeys.OutputFormat)).IsEqualTo("json");
        await Assert.That(_capturedContext.Get<string>(ContextKeys.OutputFile)).IsEqualTo("results.json");
        await Assert.That(_capturedContext.Get<string>(ContextKeys.CssUrl)).IsEqualTo("style.css");
        await Assert.That(_capturedContext.Get<bool>(ContextKeys.Parallel)).IsTrue();
        await Assert.That(_capturedContext.Get<bool>(ContextKeys.NoCache)).IsTrue();
        await Assert.That(_capturedContext.Get<bool>(ContextKeys.Quarantine)).IsTrue();
        await Assert.That(_capturedContext.Get<bool>(ContextKeys.NoHistory)).IsTrue();
        await Assert.That(_capturedContext.Get<string>(ContextKeys.AffectedBy)).IsEqualTo("HEAD");
    }

    #endregion
}
