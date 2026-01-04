using DraftSpec.Cli.History;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.History;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.History;

/// <summary>
/// Tests for <see cref="HistoryLoadPhase"/>.
/// </summary>
public class HistoryLoadPhaseTests
{
    #region Load and Propagate Tests

    [Test]
    public async Task ExecuteAsync_LoadsHistoryFromService()
    {
        var expectedHistory = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "spec1",
                    Runs = [new SpecRun { Status = "passed", DurationMs = 1000 }]
                }
            }
        };
        var historyService = new MockSpecHistoryService().WithHistory(expectedHistory);
        var phase = new HistoryLoadPhase(historyService);
        var context = CreateContext("/project/path");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var loadedHistory = context.Get<SpecHistory>(ContextKeys.History);
        await Assert.That(loadedHistory).IsNotNull();
        await Assert.That(loadedHistory!.Specs).Count().IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_SetsHistoryInContext()
    {
        var expectedHistory = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry { DisplayName = "spec1", Runs = [] }
            }
        };
        var historyService = new MockSpecHistoryService().WithHistory(expectedHistory);
        var phase = new HistoryLoadPhase(historyService);
        var context = CreateContext("/project/path");
        SpecHistory? capturedHistory = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                capturedHistory = ctx.Get<SpecHistory>(ContextKeys.History);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(capturedHistory).IsSameReferenceAs(expectedHistory);
    }

    [Test]
    public async Task ExecuteAsync_CallsNextPhase()
    {
        var historyService = new MockSpecHistoryService();
        var phase = new HistoryLoadPhase(historyService);
        var context = CreateContext("/project/path");
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

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineResult()
    {
        var historyService = new MockSpecHistoryService();
        var phase = new HistoryLoadPhase(historyService);
        var context = CreateContext("/project/path");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task ExecuteAsync_UsesProjectPathFromContext()
    {
        var historyService = new MockSpecHistoryService();
        var phase = new HistoryLoadPhase(historyService);
        var expectedPath = "/expected/project/path";
        var context = CreateContext(expectedPath);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(historyService.LoadAsyncCalls).Count().IsEqualTo(1);
        await Assert.That(historyService.LoadAsyncCalls[0]).IsEqualTo(expectedPath);
    }

    [Test]
    public async Task ExecuteAsync_EmptyHistory_SetsEmptyHistoryInContext()
    {
        var historyService = new MockSpecHistoryService().WithHistory(SpecHistory.Empty);
        var phase = new HistoryLoadPhase(historyService);
        var context = CreateContext("/project/path");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var loadedHistory = context.Get<SpecHistory>(ContextKeys.History);
        await Assert.That(loadedHistory).IsNotNull();
        await Assert.That(loadedHistory!.Specs).IsEmpty();
    }

    #endregion

    #region Cancellation Tests

    [Test]
    public async Task ExecuteAsync_PassesCancellationToken()
    {
        var historyService = new MockSpecHistoryService();
        var phase = new HistoryLoadPhase(historyService);
        var context = CreateContext("/project/path");
        var cts = new CancellationTokenSource();

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            cts.Token);

        await Assert.That(historyService.LoadAsyncCancellationTokens).Count().IsEqualTo(1);
        await Assert.That(historyService.LoadAsyncCancellationTokens[0]).IsEqualTo(cts.Token);
    }

    #endregion

    #region Helper Methods

    private static CommandContext CreateContext(string projectPath)
    {
        var context = new CommandContext
        {
            Path = ".",
            Console = new MockConsole(),
            FileSystem = new MockFileSystem()
        };
        context.Set(ContextKeys.ProjectPath, projectPath);
        return context;
    }

    #endregion
}
