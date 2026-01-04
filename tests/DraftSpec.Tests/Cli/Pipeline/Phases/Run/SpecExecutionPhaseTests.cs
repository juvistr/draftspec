using DraftSpec.Cli;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Run;
using DraftSpec.Formatters;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Run;

/// <summary>
/// Tests for <see cref="SpecExecutionPhase"/>.
/// </summary>
public class SpecExecutionPhaseTests
{
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;
    private MockInProcessSpecRunnerFactory _runnerFactory = null!;

    [Before(Test)]
    public void SetUp()
    {
        _console = new MockConsole();
        _fileSystem = new MockFileSystem();
        _runnerFactory = new MockInProcessSpecRunnerFactory();
    }

    #region No Spec Files Tests

    [Test]
    public async Task ExecuteAsync_NoSpecFiles_ReturnsZero()
    {
        var phase = new SpecExecutionPhase(_runnerFactory);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, []);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No spec files");
    }

    [Test]
    public async Task ExecuteAsync_NullSpecFiles_ReturnsZero()
    {
        var phase = new SpecExecutionPhase(_runnerFactory);
        var context = CreateContext();
        // Don't set SpecFiles

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No spec files");
    }

    [Test]
    public async Task ExecuteAsync_NoSpecFiles_DoesNotCallRunner()
    {
        var phase = new SpecExecutionPhase(_runnerFactory);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, []);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_runnerFactory.CreateCalls).IsEmpty();
    }

    #endregion

    #region Execution Tests

    [Test]
    public async Task ExecuteAsync_WithSpecFiles_ExecutesSpecs()
    {
        var summary = new InProcessRunSummary([], TimeSpan.Zero);
        _runnerFactory.Runner.WithSummary(summary);
        var phase = new SpecExecutionPhase(_runnerFactory);
        var context = CreateContext();
        var specFiles = new List<string> { TestPaths.Spec("a.spec.csx"), TestPaths.Spec("b.spec.csx") };
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, specFiles);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_runnerFactory.Runner.RunAllAsyncCalls).Count().IsEqualTo(1);
        var call = _runnerFactory.Runner.RunAllAsyncCalls[0];
        await Assert.That(call.Files.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_WithSpecFiles_SetsRunResultsInContext()
    {
        var result = new InProcessRunResult(
            TestPaths.Spec("test.spec.csx"),
            new SpecReport(),
            TimeSpan.FromMilliseconds(100));
        var summary = new InProcessRunSummary([result], TimeSpan.FromMilliseconds(100));
        _runnerFactory.Runner.WithSummary(summary);
        var phase = new SpecExecutionPhase(_runnerFactory);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var runResults = context.Get<InProcessRunSummary>(ContextKeys.RunResults);
        await Assert.That(runResults).IsSameReferenceAs(summary);
    }

    [Test]
    public async Task ExecuteAsync_WithSpecFiles_ContinuesPipeline()
    {
        var summary = new InProcessRunSummary([], TimeSpan.Zero);
        _runnerFactory.Runner.WithSummary(summary);
        var phase = new SpecExecutionPhase(_runnerFactory);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsTrue();
    }

    #endregion

    #region Filter Options Tests

    [Test]
    public async Task ExecuteAsync_WithFilter_PassesFilterToRunner()
    {
        var summary = new InProcessRunSummary([], TimeSpan.Zero);
        _runnerFactory.Runner.WithSummary(summary);
        var phase = new SpecExecutionPhase(_runnerFactory);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);
        context.Set(ContextKeys.Filter, new FilterOptions
        {
            FilterTags = "fast",
            ExcludeTags = "slow",
            FilterName = "should",
            ExcludeName = "skip"
        });

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_runnerFactory.CreateCalls).Count().IsEqualTo(1);
        var createCall = _runnerFactory.CreateCalls[0];
        await Assert.That(createCall.FilterTags).IsEqualTo("fast");
        await Assert.That(createCall.ExcludeTags).IsEqualTo("slow");
        await Assert.That(createCall.FilterName).IsEqualTo("should");
        await Assert.That(createCall.ExcludeName).IsEqualTo("skip");
    }

    [Test]
    public async Task ExecuteAsync_NoFilter_UsesEmptyFilterOptions()
    {
        var summary = new InProcessRunSummary([], TimeSpan.Zero);
        _runnerFactory.Runner.WithSummary(summary);
        var phase = new SpecExecutionPhase(_runnerFactory);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_runnerFactory.CreateCalls).Count().IsEqualTo(1);
        var createCall = _runnerFactory.CreateCalls[0];
        await Assert.That(createCall.FilterTags).IsNull();
        await Assert.That(createCall.ExcludeTags).IsNull();
        await Assert.That(createCall.FilterName).IsNull();
        await Assert.That(createCall.ExcludeName).IsNull();
    }

    #endregion

    #region Parallel Execution Tests

    [Test]
    public async Task ExecuteAsync_ParallelTrue_PassesParallelToRunner()
    {
        var summary = new InProcessRunSummary([], TimeSpan.Zero);
        _runnerFactory.Runner.WithSummary(summary);
        var phase = new SpecExecutionPhase(_runnerFactory);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);
        context.Set(ContextKeys.Parallel, true);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var runCall = _runnerFactory.Runner.RunAllAsyncCalls[0];
        await Assert.That(runCall.Parallel).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_ParallelFalse_PassesParallelFalseToRunner()
    {
        var summary = new InProcessRunSummary([], TimeSpan.Zero);
        _runnerFactory.Runner.WithSummary(summary);
        var phase = new SpecExecutionPhase(_runnerFactory);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);
        context.Set(ContextKeys.Parallel, false);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var runCall = _runnerFactory.Runner.RunAllAsyncCalls[0];
        await Assert.That(runCall.Parallel).IsFalse();
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

    #endregion
}
