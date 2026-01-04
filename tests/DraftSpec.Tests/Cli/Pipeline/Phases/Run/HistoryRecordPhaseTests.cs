using DraftSpec.Cli;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Run;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Run;

/// <summary>
/// Tests for <see cref="HistoryRecordPhase"/>.
/// </summary>
public class HistoryRecordPhaseTests
{
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;
    private MockSpecHistoryService _historyService = null!;

    [Before(Test)]
    public void SetUp()
    {
        _console = new MockConsole();
        _fileSystem = new MockFileSystem();
        _historyService = new MockSpecHistoryService();
    }

    #region NoHistory Flag Tests

    [Test]
    public async Task ExecuteAsync_NoHistoryTrue_SkipsRecording()
    {
        var phase = new HistoryRecordPhase(_historyService);
        var context = CreateContext();
        context.Set(ContextKeys.NoHistory, true);
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());
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
        await Assert.That(_historyService.RecordRunAsyncCalls).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_NoHistoryFalse_RecordsHistory()
    {
        var phase = new HistoryRecordPhase(_historyService);
        var context = CreateContext();
        context.Set(ContextKeys.NoHistory, false);
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_historyService.RecordRunAsyncCalls).IsEqualTo(1);
    }

    #endregion

    #region No Results Tests

    [Test]
    public async Task ExecuteAsync_NoRunResults_PassesThroughUnchanged()
    {
        var phase = new HistoryRecordPhase(_historyService);
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
        await Assert.That(_historyService.RecordRunAsyncCalls).IsEqualTo(0);
    }

    #endregion

    #region No Project Path Tests

    [Test]
    public async Task ExecuteAsync_NoProjectPath_PassesThroughUnchanged()
    {
        var phase = new HistoryRecordPhase(_historyService);
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());
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
        await Assert.That(_historyService.RecordRunAsyncCalls).IsEqualTo(0);
    }

    #endregion

    #region Recording Tests

    [Test]
    public async Task ExecuteAsync_WithResults_RecordsToHistory()
    {
        var phase = new HistoryRecordPhase(_historyService);
        var context = CreateContext();
        var summary = CreateSuccessfulRunSummary();
        context.Set(ContextKeys.RunResults, summary);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_historyService.RecordRunAsyncCalls).IsEqualTo(1);
        await Assert.That(_historyService.RecordedResults.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task ExecuteAsync_WithResults_ExtractsSpecIdCorrectly()
    {
        var phase = new HistoryRecordPhase(_historyService);
        var context = CreateContext();
        var summary = CreateRunSummaryWithSpec("MyContext", "should work", "passed");
        context.Set(ContextKeys.RunResults, summary);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var record = _historyService.RecordedResults[0];
        await Assert.That(record.SpecId).Contains("MyContext");
        await Assert.That(record.SpecId).Contains("should work");
    }

    [Test]
    public async Task ExecuteAsync_WithResults_ExtractsDisplayNameCorrectly()
    {
        var phase = new HistoryRecordPhase(_historyService);
        var context = CreateContext();
        var summary = CreateRunSummaryWithSpec("MyContext", "should work", "passed");
        context.Set(ContextKeys.RunResults, summary);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var record = _historyService.RecordedResults[0];
        await Assert.That(record.DisplayName).Contains("MyContext");
        await Assert.That(record.DisplayName).Contains("should work");
        await Assert.That(record.DisplayName).Contains(" > ");
    }

    [Test]
    public async Task ExecuteAsync_WithResults_ExtractsStatusCorrectly()
    {
        var phase = new HistoryRecordPhase(_historyService);
        var context = CreateContext();
        var summary = CreateRunSummaryWithSpec("Context", "spec", "Passed");
        context.Set(ContextKeys.RunResults, summary);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var record = _historyService.RecordedResults[0];
        await Assert.That(record.Status).IsEqualTo("passed");
    }

    [Test]
    public async Task ExecuteAsync_WithNestedContexts_ExtractsAllSpecs()
    {
        var phase = new HistoryRecordPhase(_historyService);
        var context = CreateContext();
        var summary = CreateRunSummaryWithNestedContexts();
        context.Set(ContextKeys.RunResults, summary);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_historyService.RecordedResults.Count).IsEqualTo(2);
    }

    #endregion

    #region Pipeline Continuation Tests

    [Test]
    public async Task ExecuteAsync_Always_ContinuesPipeline()
    {
        var phase = new HistoryRecordPhase(_historyService);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());
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
    public async Task ExecuteAsync_PipelineReturnsError_PropagatesError()
    {
        var phase = new HistoryRecordPhase(_historyService);
        var context = CreateContext();
        context.Set(ContextKeys.RunResults, CreateSuccessfulRunSummary());

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
        context.Set(ContextKeys.ProjectPath, TestPaths.ProjectDir);
        return context;
    }

    private static InProcessRunSummary CreateSuccessfulRunSummary()
    {
        return CreateRunSummaryWithSpec("TestContext", "should pass", "passed");
    }

    private static InProcessRunSummary CreateRunSummaryWithSpec(
        string contextDescription,
        string specDescription,
        string status)
    {
        var report = new SpecReport
        {
            Contexts =
            [
                new SpecContextReport
                {
                    Description = contextDescription,
                    Specs =
                    [
                        new SpecResultReport
                        {
                            Description = specDescription,
                            Status = status,
                            DurationMs = 10
                        }
                    ]
                }
            ]
        };

        var result = new InProcessRunResult(
            TestPaths.Spec("test.spec.csx"),
            report,
            TimeSpan.FromMilliseconds(10));

        return new InProcessRunSummary([result], TimeSpan.FromMilliseconds(10));
    }

    private static InProcessRunSummary CreateRunSummaryWithNestedContexts()
    {
        var report = new SpecReport
        {
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Parent",
                    Specs =
                    [
                        new SpecResultReport
                        {
                            Description = "parent spec",
                            Status = "passed",
                            DurationMs = 10
                        }
                    ],
                    Contexts =
                    [
                        new SpecContextReport
                        {
                            Description = "Child",
                            Specs =
                            [
                                new SpecResultReport
                                {
                                    Description = "child spec",
                                    Status = "passed",
                                    DurationMs = 5
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var result = new InProcessRunResult(
            TestPaths.Spec("test.spec.csx"),
            report,
            TimeSpan.FromMilliseconds(15));

        return new InProcessRunSummary([result], TimeSpan.FromMilliseconds(15));
    }

    #endregion
}
