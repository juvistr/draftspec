using DraftSpec.Cli.History;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Flaky;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Flaky;

/// <summary>
/// Tests for <see cref="FlakyOutputPhase"/>.
/// </summary>
public class FlakyOutputPhaseTests
{
    private string _tempDir = null!;
    private MockConsole _console = null!;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_flaky_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _console = new MockConsole();
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region Clear Option Tests

    [Test]
    public async Task ExecuteAsync_ClearOption_Success_ShowsSuccessMessage()
    {
        var historyService = new MockSpecHistoryService().WithClearSpecResult(true);
        var phase = new FlakyOutputPhase(historyService);
        var context = CreateContext(SpecHistory.Empty);
        context.Set(ContextKeys.Clear, "test.spec.csx:Context/spec1");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("Cleared history for");
        await Assert.That(_console.Output).Contains("test.spec.csx:Context/spec1");
    }

    [Test]
    public async Task ExecuteAsync_ClearOption_NotFound_ShowsWarning()
    {
        var historyService = new MockSpecHistoryService().WithClearSpecResult(false);
        var phase = new FlakyOutputPhase(historyService);
        var context = CreateContext(SpecHistory.Empty);
        context.Set(ContextKeys.Clear, "nonexistent-spec");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Output).Contains("No history found for");
        await Assert.That(_console.Output).Contains("nonexistent-spec");
    }

    [Test]
    public async Task ExecuteAsync_ClearOption_ShortCircuitsHistoryCheck()
    {
        var historyService = new MockSpecHistoryService().WithClearSpecResult(true);
        var phase = new FlakyOutputPhase(historyService);
        // Even with empty history, clear should succeed without "no history" message
        var context = CreateContext(SpecHistory.Empty);
        context.Set(ContextKeys.Clear, "test-spec");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).DoesNotContain("No test history found");
    }

    #endregion

    #region Empty History Tests

    [Test]
    public async Task ExecuteAsync_NoHistory_ShowsRunSpecsFirstMessage()
    {
        var historyService = new MockSpecHistoryService();
        var phase = new FlakyOutputPhase(historyService);
        var context = CreateContext(SpecHistory.Empty);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No test history found");
        await Assert.That(_console.Output).Contains("Run specs first to collect data");
        await Assert.That(_console.Output).Contains("draftspec run");
    }

    #endregion

    #region No Flaky Specs Tests

    [Test]
    public async Task ExecuteAsync_HistoryWithNoFlakySpecs_ShowsNoFlakySpecsMessage()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test.spec.csx:Context/spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "Context > spec1",
                    Runs = [
                        new SpecRun { Status = "passed", Timestamp = DateTime.UtcNow },
                        new SpecRun { Status = "passed", Timestamp = DateTime.UtcNow }
                    ]
                }
            }
        };
        var historyService = new MockSpecHistoryService();
        var phase = new FlakyOutputPhase(historyService);
        var context = CreateContext(history);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No flaky specs detected");
        await Assert.That(_console.Output).Contains("Analyzed 1 specs");
    }

    #endregion

    #region Flaky Specs Detection Tests

    [Test]
    public async Task ExecuteAsync_WithFlakySpecs_ShowsReport()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test.spec.csx:Context/spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "Context > spec1",
                    Runs = [new SpecRun { Status = "passed" }]
                }
            }
        };
        var flakySpec = new FlakySpec
        {
            SpecId = "test.spec.csx:Context/spec1",
            DisplayName = "Context > spec1",
            StatusChanges = 3,
            TotalRuns = 5,
            PassRate = 0.6
        };
        var historyService = new MockSpecHistoryService()
            .WithFlakySpecs(flakySpec);
        var phase = new FlakyOutputPhase(historyService);
        var context = CreateContext(history);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("Flaky Tests Detected: 1");
        await Assert.That(_console.Output).Contains("Context > spec1");
        // Use flexible assertion to handle locale-specific formatting ("60%" vs "60 %")
        await Assert.That(_console.Output).Contains("60");
        await Assert.That(_console.Output).Contains("pass rate");
        await Assert.That(_console.Output).Contains("3 status change(s)");
    }

    [Test]
    public async Task ExecuteAsync_WithFlakySpecs_ShowsSeverityLevels()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["spec1"] = new SpecHistoryEntry { DisplayName = "Spec 1", Runs = [new SpecRun { Status = "passed" }] },
                ["spec2"] = new SpecHistoryEntry { DisplayName = "Spec 2", Runs = [new SpecRun { Status = "passed" }] }
            }
        };
        var mediumFlaky = new FlakySpec
        {
            SpecId = "spec1",
            DisplayName = "Medium Flaky Spec",
            StatusChanges = 2,
            TotalRuns = 5,
            PassRate = 0.6
        };
        var highFlaky = new FlakySpec
        {
            SpecId = "spec2",
            DisplayName = "High Flaky Spec",
            StatusChanges = 5,
            TotalRuns = 8,
            PassRate = 0.5
        };
        var historyService = new MockSpecHistoryService()
            .WithFlakySpecs(mediumFlaky, highFlaky);
        var phase = new FlakyOutputPhase(historyService);
        var context = CreateContext(history);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("MEDIUM");
        await Assert.That(_console.Output).Contains("HIGH");
    }

    [Test]
    public async Task ExecuteAsync_WithLowSeveritySpec_ShowsLowSeverity()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["spec1"] = new SpecHistoryEntry { DisplayName = "Spec 1", Runs = [new SpecRun { Status = "passed" }] }
            }
        };
        var lowFlaky = new FlakySpec
        {
            SpecId = "spec1",
            DisplayName = "Low Flaky Spec",
            StatusChanges = 1, // < 2 results in LOW severity
            TotalRuns = 10,
            PassRate = 0.8
        };
        var historyService = new MockSpecHistoryService()
            .WithFlakySpecs(lowFlaky);
        var phase = new FlakyOutputPhase(historyService);
        var context = CreateContext(history);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("LOW");
    }

    [Test]
    public async Task ExecuteAsync_WithFlakySpecs_ShowsRecommendations()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["spec1"] = new SpecHistoryEntry { DisplayName = "Spec 1", Runs = [new SpecRun { Status = "passed" }] }
            }
        };
        var flakySpec = new FlakySpec
        {
            SpecId = "spec1",
            DisplayName = "Flaky Spec",
            StatusChanges = 3,
            TotalRuns = 5,
            PassRate = 0.6
        };
        var historyService = new MockSpecHistoryService()
            .WithFlakySpecs(flakySpec);
        var phase = new FlakyOutputPhase(historyService);
        var context = CreateContext(history);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("Recommendations");
        await Assert.That(_console.Output).Contains("--quarantine");
        await Assert.That(_console.Output).Contains("--clear");
    }

    #endregion

    #region Window Size and Min Changes Tests

    [Test]
    public async Task ExecuteAsync_UsesWindowSizeFromContext()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["spec1"] = new SpecHistoryEntry { DisplayName = "Spec 1", Runs = [new SpecRun { Status = "passed" }] }
            }
        };
        var flakySpec = new FlakySpec
        {
            SpecId = "spec1",
            DisplayName = "Flaky Spec",
            StatusChanges = 3,
            TotalRuns = 5,
            PassRate = 0.6
        };
        var historyService = new MockSpecHistoryService()
            .WithFlakySpecs(flakySpec);
        var phase = new FlakyOutputPhase(historyService);
        var context = CreateContext(history);
        context.Set(ContextKeys.WindowSize, 20);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("last 20 runs per spec");
    }

    [Test]
    public async Task ExecuteAsync_UsesMinStatusChangesFromContext()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["spec1"] = new SpecHistoryEntry { DisplayName = "Spec 1", Runs = [new SpecRun { Status = "passed" }] }
            }
        };
        var historyService = new MockSpecHistoryService();
        var phase = new FlakyOutputPhase(historyService);
        var context = CreateContext(history);
        context.Set(ContextKeys.MinStatusChanges, 5);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("5+ status changes threshold");
    }

    #endregion

    #region Helper Methods

    private CommandContext CreateContext(SpecHistory history)
    {
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = new MockFileSystem()
        };
        context.Set(ContextKeys.ProjectPath, _tempDir);
        context.Set(ContextKeys.History, history);
        context.Set(ContextKeys.MinStatusChanges, 2);
        context.Set(ContextKeys.WindowSize, 10);
        context.Set<string?>(ContextKeys.Clear, null);
        return context;
    }

    #endregion
}
