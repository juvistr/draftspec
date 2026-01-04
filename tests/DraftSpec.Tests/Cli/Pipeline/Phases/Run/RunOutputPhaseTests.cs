using DraftSpec.Cli;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Run;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Run;

/// <summary>
/// Tests for <see cref="RunOutputPhase"/>.
/// </summary>
public class RunOutputPhaseTests
{
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;
    private MockCliFormatterRegistry _formatterRegistry = null!;
    private MockPathValidator _pathValidator = null!;

    [Before(Test)]
    public void SetUp()
    {
        _console = new MockConsole();
        _fileSystem = new MockFileSystem();
        _formatterRegistry = new MockCliFormatterRegistry();
        _pathValidator = new MockPathValidator();
    }

    #region No Results Tests

    [Test]
    public async Task ExecuteAsync_NoResults_PassesThroughUnchanged()
    {
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsTrue();
    }

    #endregion

    #region Console Output Tests

    [Test]
    public async Task ExecuteAsync_ConsoleFormat_ShowsResults()
    {
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());
        context.Set(ContextKeys.OutputFormat, "console");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("PASS");
    }

    [Test]
    public async Task ExecuteAsync_DefaultFormat_UsesConsole()
    {
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());
        // No OutputFormat set

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("PASS");
        await Assert.That(_formatterRegistry.GetFormatterCalls).IsEmpty();
    }

    [Test]
    public async Task ExecuteAsync_SuccessfulRun_ShowsPassStatus()
    {
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("PASS");
    }

    [Test]
    public async Task ExecuteAsync_FailedRun_ShowsFailStatus()
    {
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateFailedRunSummary());

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("FAIL");
    }

    [Test]
    public async Task ExecuteAsync_WithStats_ShowsSummary()
    {
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());
        context.Set(ContextKeys.NoStats, false);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("Tests:");
        await Assert.That(_console.Output).Contains("Duration:");
    }

    [Test]
    public async Task ExecuteAsync_NoStats_HidesSummary()
    {
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());
        context.Set(ContextKeys.NoStats, true);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).DoesNotContain("Duration:");
    }

    [Test]
    public async Task ExecuteAsync_StatsOnly_HidesIndividualResults()
    {
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());
        context.Set(ContextKeys.StatsOnly, true);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).DoesNotContain("[PASS]");
        await Assert.That(_console.Output).Contains("Tests:");
    }

    #endregion

    #region Formatter Output Tests

    [Test]
    public async Task ExecuteAsync_JsonFormat_UsesFormatter()
    {
        var mockFormatter = new MockFormatter().WithOutput("{\"result\": \"ok\"}");
        _formatterRegistry.WithFormatter("json", mockFormatter);
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());
        context.Set(ContextKeys.OutputFormat, "json");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_formatterRegistry.GetFormatterCalls).Count().IsEqualTo(1);
        await Assert.That(_formatterRegistry.GetFormatterCalls[0].Name).IsEqualTo("json");
        await Assert.That(_console.Output).Contains("{\"result\": \"ok\"}");
    }

    [Test]
    public async Task ExecuteAsync_UnknownFormat_ReturnsError()
    {
        _formatterRegistry.ReturnsNullForUnknown();
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());
        context.Set(ContextKeys.OutputFormat, "unknown");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("Unknown format");
    }

    #endregion

    #region File Output Tests

    [Test]
    public async Task ExecuteAsync_WithOutputFile_WritesToFile()
    {
        var mockFormatter = new MockFormatter().WithOutput("<html>report</html>");
        _formatterRegistry.WithFormatter("html", mockFormatter);
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());
        context.Set(ContextKeys.OutputFormat, "html");
        context.Set(ContextKeys.OutputFile, "report.html");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_fileSystem.WrittenFiles).Count().IsEqualTo(1);
        await Assert.That(_console.Output).Contains("Output written to");
    }

    [Test]
    public async Task ExecuteAsync_WithOutputFile_ValidatesPath()
    {
        var mockFormatter = new MockFormatter().WithOutput("output");
        _formatterRegistry.WithFormatter("json", mockFormatter);
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());
        context.Set(ContextKeys.OutputFormat, "json");
        context.Set(ContextKeys.OutputFile, "output.json");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_pathValidator.ValidatePathWithinBaseCalls).Count().IsEqualTo(1);
    }

    #endregion

    #region Exit Code Tests

    [Test]
    public async Task ExecuteAsync_SuccessfulRun_ReturnsZero()
    {
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_FailedRun_ReturnsOne()
    {
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateFailedRunSummary());

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_PipelineReturnsError_PropagatesError()
    {
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    #endregion

    #region ProjectPath Fallback Tests

    [Test]
    public async Task ExecuteAsync_NoProjectPath_UsesDotAsFallback()
    {
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        // Don't set ProjectPath - should fallback to "."
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        // Should succeed without error
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("PASS");
    }

    #endregion

    #region Report Merging Tests

    [Test]
    public async Task ExecuteAsync_MultipleFiles_MergesReports()
    {
        var mockFormatter = new MockFormatter();
        _formatterRegistry.WithFormatter("json", mockFormatter);
        var phase = new RunOutputPhase(_formatterRegistry, _pathValidator);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateMultiFileRunSummary());
        context.Set(ContextKeys.OutputFormat, "json");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(mockFormatter.FormatCalls).Count().IsEqualTo(1);
        var report = mockFormatter.FormatCalls[0];
        await Assert.That(report.Contexts.Count).IsEqualTo(2);
    }

    #endregion

    #region Helper Methods

    private CommandContext CreateContext()
    {
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        context.Set(ContextKeys.ProjectPath, TestPaths.ProjectDir);
        return context;
    }

    private static InProcessRunSummary CreateSuccessfulRunSummary()
    {
        var report = new SpecReport
        {
            Summary = new SpecSummary { Total = 1, Passed = 1 },
            Contexts = [new SpecContextReport { Description = "Test" }]
        };

        var result = new InProcessRunResult(
            TestPaths.Spec("test.spec.csx"),
            report,
            TimeSpan.FromMilliseconds(100));

        return new InProcessRunSummary([result], TimeSpan.FromMilliseconds(100));
    }

    private static InProcessRunSummary CreateFailedRunSummary()
    {
        var report = new SpecReport
        {
            Summary = new SpecSummary { Total = 1, Failed = 1 },
            Contexts = [new SpecContextReport { Description = "Test" }]
        };

        var result = new InProcessRunResult(
            TestPaths.Spec("test.spec.csx"),
            report,
            TimeSpan.FromMilliseconds(100),
            new Exception("Test failed"));

        return new InProcessRunSummary([result], TimeSpan.FromMilliseconds(100));
    }

    private static InProcessRunSummary CreateMultiFileRunSummary()
    {
        var report1 = new SpecReport
        {
            Summary = new SpecSummary { Total = 1, Passed = 1 },
            Contexts = [new SpecContextReport { Description = "Test1" }]
        };

        var report2 = new SpecReport
        {
            Summary = new SpecSummary { Total = 1, Passed = 1 },
            Contexts = [new SpecContextReport { Description = "Test2" }]
        };

        return new InProcessRunSummary([
            new InProcessRunResult(TestPaths.Spec("a.spec.csx"), report1, TimeSpan.FromMilliseconds(50)),
            new InProcessRunResult(TestPaths.Spec("b.spec.csx"), report2, TimeSpan.FromMilliseconds(50))
        ], TimeSpan.FromMilliseconds(100));
    }

    #endregion
}
