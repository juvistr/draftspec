using DraftSpec.Cli;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Run;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Run;

/// <summary>
/// Tests for <see cref="PreRunStatsPhase"/>.
/// </summary>
public class PreRunStatsPhaseTests
{
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;
    private MockSpecStatsCollector _statsCollector = null!;

    [Before(Test)]
    public void SetUp()
    {
        _console = new MockConsole();
        _fileSystem = new MockFileSystem();
        _statsCollector = new MockSpecStatsCollector();
    }

    #region NoStats Flag Tests

    [Test]
    public async Task ExecuteAsync_NoStatsTrue_SkipsStatsDisplay()
    {
        var phase = new PreRunStatsPhase(_statsCollector);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);
        context.Set(ContextKeys.NoStats, true);
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
        await Assert.That(_statsCollector.CollectAsyncCalls).IsEmpty();
    }

    [Test]
    public async Task ExecuteAsync_NoStatsFalse_CollectsStats()
    {
        var stats = new SpecStats(10, 8, 1, 1, 2, false, 3);
        _statsCollector.WithStats(stats);
        var phase = new PreRunStatsPhase(_statsCollector);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);
        context.Set(ContextKeys.NoStats, false);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_statsCollector.CollectAsyncCalls).Count().IsEqualTo(1);
    }

    #endregion

    #region StatsOnly Mode Tests

    [Test]
    public async Task ExecuteAsync_StatsOnlyNoFocus_ReturnsZero()
    {
        var stats = new SpecStats(10, 10, 0, 0, 0, false, 2);
        _statsCollector.WithStats(stats);
        var phase = new PreRunStatsPhase(_statsCollector);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);
        context.Set(ContextKeys.StatsOnly, true);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_StatsOnlyWithFocus_ReturnsTwo()
    {
        var stats = new SpecStats(10, 8, 2, 0, 0, true, 2);
        _statsCollector.WithStats(stats);
        var phase = new PreRunStatsPhase(_statsCollector);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);
        context.Set(ContextKeys.StatsOnly, true);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_StatsOnly_DoesNotContinuePipeline()
    {
        var stats = new SpecStats(10, 10, 0, 0, 0, false, 2);
        _statsCollector.WithStats(stats);
        var phase = new PreRunStatsPhase(_statsCollector);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);
        context.Set(ContextKeys.StatsOnly, true);
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_StatsOnlyOverridesNoStats_CollectsStats()
    {
        var stats = new SpecStats(10, 10, 0, 0, 0, false, 2);
        _statsCollector.WithStats(stats);
        var phase = new PreRunStatsPhase(_statsCollector);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);
        context.Set(ContextKeys.NoStats, true);
        context.Set(ContextKeys.StatsOnly, true);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_statsCollector.CollectAsyncCalls).Count().IsEqualTo(1);
    }

    #endregion

    #region Empty/Null SpecFiles Tests

    [Test]
    public async Task ExecuteAsync_NoSpecFiles_PassesThrough()
    {
        var phase = new PreRunStatsPhase(_statsCollector);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, []);
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
        await Assert.That(_statsCollector.CollectAsyncCalls).IsEmpty();
    }

    [Test]
    public async Task ExecuteAsync_NullSpecFiles_PassesThrough()
    {
        var phase = new PreRunStatsPhase(_statsCollector);
        var context = CreateContext();
        // Don't set SpecFiles
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
        await Assert.That(_statsCollector.CollectAsyncCalls).IsEmpty();
    }

    #endregion

    #region Normal Flow Tests

    [Test]
    public async Task ExecuteAsync_ShowsStats_ContinuesPipeline()
    {
        var stats = new SpecStats(10, 8, 1, 1, 2, false, 3);
        _statsCollector.WithStats(stats);
        var phase = new PreRunStatsPhase(_statsCollector);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsTrue();
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_DisplaysStatsToConsole()
    {
        var stats = new SpecStats(10, 8, 1, 1, 2, false, 3);
        _statsCollector.WithStats(stats);
        var phase = new PreRunStatsPhase(_statsCollector);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("10 spec(s)");
        await Assert.That(_console.Output).Contains("3 file(s)");
    }

    [Test]
    public async Task ExecuteAsync_PassesCorrectParametersToCollector()
    {
        var stats = new SpecStats(10, 10, 0, 0, 0, false, 2);
        _statsCollector.WithStats(stats);
        var phase = new PreRunStatsPhase(_statsCollector);
        var context = CreateContext();
        var specFiles = new List<string> { TestPaths.Spec("a.spec.csx"), TestPaths.Spec("b.spec.csx") };
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, specFiles);
        context.Set(ContextKeys.ProjectPath, "/project");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_statsCollector.CollectAsyncCalls).Count().IsEqualTo(1);
        var call = _statsCollector.CollectAsyncCalls[0];
        await Assert.That(call.SpecFiles).IsEqualTo(specFiles);
        await Assert.That(call.ProjectPath).IsEqualTo("/project");
    }

    [Test]
    public async Task ExecuteAsync_NoProjectPath_UsesDotDefault()
    {
        var stats = new SpecStats(10, 10, 0, 0, 0, false, 2);
        _statsCollector.WithStats(stats);
        var phase = new PreRunStatsPhase(_statsCollector);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);
        // Don't set ProjectPath

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var call = _statsCollector.CollectAsyncCalls[0];
        await Assert.That(call.ProjectPath).IsEqualTo(".");
    }

    #endregion

    #region Pipeline Result Propagation Tests

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineResult()
    {
        var stats = new SpecStats(10, 10, 0, 0, 0, false, 2);
        _statsCollector.WithStats(stats);
        var phase = new PreRunStatsPhase(_statsCollector);
        var context = CreateContext();
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, [TestPaths.Spec("test.spec.csx")]);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
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
        return context;
    }

    #endregion
}
