using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for RunCommand option-to-context wiring.
/// Phase logic is tested in QuarantinePhaseTests, SpecExecutionPhaseTests, RunOutputPhaseTests, etc.
/// </summary>
public class RunCommandTests
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

    #region Basic Context Setup

    [Test]
    public async Task ExecuteAsync_SetsPathInContext()
    {
        var command = CreateCommand();
        var options = new RunOptions { Path = "/test/specs" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Path).IsEqualTo("/test/specs");
    }

    [Test]
    public async Task ExecuteAsync_SetsConsoleInContext()
    {
        var command = CreateCommand();
        var options = new RunOptions();

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Console).IsSameReferenceAs(_console);
    }

    [Test]
    public async Task ExecuteAsync_SetsFileSystemInContext()
    {
        var command = CreateCommand();
        var options = new RunOptions();

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.FileSystem).IsSameReferenceAs(_fileSystem);
    }

    #endregion

    #region Output Format Wiring

    [Test]
    public async Task ExecuteAsync_SetsOutputFormat_Console()
    {
        var command = CreateCommand();
        var options = new RunOptions { Format = OutputFormat.Console };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.OutputFormat))
            .IsEqualTo("console");
    }

    [Test]
    public async Task ExecuteAsync_SetsOutputFormat_Json()
    {
        var command = CreateCommand();
        var options = new RunOptions { Format = OutputFormat.Json };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.OutputFormat))
            .IsEqualTo("json");
    }

    [Test]
    public async Task ExecuteAsync_SetsOutputFile()
    {
        var command = CreateCommand();
        var options = new RunOptions { OutputFile = "report.json" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.OutputFile))
            .IsEqualTo("report.json");
    }

    [Test]
    public async Task ExecuteAsync_SetsCssUrl()
    {
        var command = CreateCommand();
        var options = new RunOptions { CssUrl = "https://example.com/style.css" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.CssUrl))
            .IsEqualTo("https://example.com/style.css");
    }

    #endregion

    #region Run Flags Wiring

    [Test]
    public async Task ExecuteAsync_SetsParallel_True()
    {
        var command = CreateCommand();
        var options = new RunOptions { Parallel = true };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.Parallel)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SetsParallel_False()
    {
        var command = CreateCommand();
        var options = new RunOptions { Parallel = false };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.Parallel)).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_SetsNoCache_True()
    {
        var command = CreateCommand();
        var options = new RunOptions { NoCache = true };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.NoCache)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SetsNoStats_True()
    {
        var command = CreateCommand();
        var options = new RunOptions { NoStats = true };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.NoStats)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SetsStatsOnly_True()
    {
        var command = CreateCommand();
        var options = new RunOptions { StatsOnly = true };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.StatsOnly)).IsTrue();
    }

    #endregion

    #region Filter Options Wiring

    [Test]
    public async Task ExecuteAsync_SetsFilter()
    {
        var command = CreateCommand();
        var filter = new FilterOptions { FilterTags = "smoke" };
        var options = new RunOptions { Filter = filter };

        await command.ExecuteAsync(options);

        var capturedFilter = _capturedContext!.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(capturedFilter!.FilterTags).IsEqualTo("smoke");
    }

    #endregion

    #region Partition Options Wiring

    [Test]
    public async Task ExecuteAsync_SetsPartition()
    {
        var command = CreateCommand();
        var partition = new PartitionOptions { Total = 4, Index = 2 };
        var options = new RunOptions { Partition = partition };

        await command.ExecuteAsync(options);

        var capturedPartition = _capturedContext!.Get<PartitionOptions>(ContextKeys.Partition);
        await Assert.That(capturedPartition!.Total).IsEqualTo(4);
        await Assert.That(capturedPartition.Index).IsEqualTo(2);
    }

    #endregion

    #region Impact Analysis Wiring

    [Test]
    public async Task ExecuteAsync_SetsAffectedBy()
    {
        var command = CreateCommand();
        var options = new RunOptions { AffectedBy = "HEAD~1" };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<string>(ContextKeys.AffectedBy))
            .IsEqualTo("HEAD~1");
    }

    [Test]
    public async Task ExecuteAsync_SetsDryRun_True()
    {
        var command = CreateCommand();
        var options = new RunOptions { DryRun = true };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.DryRun)).IsTrue();
    }

    #endregion

    #region Quarantine and History Wiring

    [Test]
    public async Task ExecuteAsync_SetsQuarantine_True()
    {
        var command = CreateCommand();
        var options = new RunOptions { Quarantine = true };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.Quarantine)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_SetsNoHistory_True()
    {
        var command = CreateCommand();
        var options = new RunOptions { NoHistory = true };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.NoHistory)).IsTrue();
    }

    #endregion

    #region Interactive Mode Wiring

    [Test]
    public async Task ExecuteAsync_SetsInteractive_True()
    {
        var command = CreateCommand();
        var options = new RunOptions { Interactive = true };

        await command.ExecuteAsync(options);

        await Assert.That(_capturedContext!.Get<bool>(ContextKeys.Interactive)).IsTrue();
    }

    #endregion

    #region Pipeline Return Value

    [Test]
    public async Task ExecuteAsync_ReturnsPipelineResult_Success()
    {
        var command = CreateCommand(pipelineReturnValue: 0);
        var options = new RunOptions();

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_ReturnsPipelineResult_Failure()
    {
        var command = CreateCommand(pipelineReturnValue: 1);
        var options = new RunOptions();

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
    }

    #endregion
}
