using DraftSpec.Cli.History;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Run;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Run;

/// <summary>
/// Tests for <see cref="QuarantinePhase"/>.
/// </summary>
public class QuarantinePhaseTests
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

    #region Quarantine Disabled Tests

    [Test]
    public async Task ExecuteAsync_QuarantineDisabled_PassesThroughUnchanged()
    {
        var phase = new QuarantinePhase(_historyService);
        var context = CreateContext(quarantine: false);
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
        await Assert.That(_historyService.LoadAsyncCalls).IsEmpty();
    }

    [Test]
    public async Task ExecuteAsync_QuarantineDisabled_DoesNotModifyFilter()
    {
        var phase = new QuarantinePhase(_historyService);
        var existingFilter = new FilterOptions { FilterName = "existing" };
        var context = CreateContext(quarantine: false);
        context.Set(ContextKeys.Filter, existingFilter);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(filter).IsSameReferenceAs(existingFilter);
        await Assert.That(filter!.ExcludeName).IsNull();
    }

    #endregion

    #region No Flaky Specs Tests

    [Test]
    public async Task ExecuteAsync_QuarantineEnabled_NoFlakySpecs_PassesThroughUnchanged()
    {
        _historyService.WithHistory(SpecHistory.Empty);
        var phase = new QuarantinePhase(_historyService);
        var context = CreateContext(quarantine: true);
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
        await Assert.That(_console.Output).DoesNotContain("Quarantining");
    }

    [Test]
    public async Task ExecuteAsync_QuarantineEnabled_NoFlakySpecs_LoadsHistory()
    {
        _historyService.WithHistory(SpecHistory.Empty);
        var phase = new QuarantinePhase(_historyService);
        var context = CreateContext(quarantine: true, projectPath: "/project");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_historyService.LoadAsyncCalls).Count().IsEqualTo(1);
        await Assert.That(_historyService.LoadAsyncCalls[0]).IsEqualTo("/project");
    }

    #endregion

    #region Flaky Specs Tests

    [Test]
    public async Task ExecuteAsync_QuarantineEnabled_WithFlakySpecs_AddsExcludePattern()
    {
        var flakySpec = new FlakySpec
        {
            SpecId = "test:flaky1",
            DisplayName = "Service > should handle errors",
            StatusChanges = 3,
            TotalRuns = 10,
            PassRate = 0.5
        };
        _historyService.WithFlakySpecs(flakySpec);
        var phase = new QuarantinePhase(_historyService);
        var context = CreateContext(quarantine: true);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(filter).IsNotNull();
        // Regex.Escape escapes spaces and special chars
        await Assert.That(filter!.ExcludeName).Contains("Service");
        await Assert.That(filter!.ExcludeName).Contains("should");
        await Assert.That(filter!.ExcludeName).Contains("handle");
        await Assert.That(filter!.ExcludeName).Contains("errors");
    }

    [Test]
    public async Task ExecuteAsync_QuarantineEnabled_WithFlakySpecs_OutputsMessage()
    {
        var flakySpec = new FlakySpec
        {
            SpecId = "test:flaky1",
            DisplayName = "flaky spec",
            StatusChanges = 3,
            TotalRuns = 10,
            PassRate = 0.5
        };
        _historyService.WithFlakySpecs(flakySpec);
        var phase = new QuarantinePhase(_historyService);
        var context = CreateContext(quarantine: true);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("Quarantining 1 flaky spec(s)");
    }

    [Test]
    public async Task ExecuteAsync_QuarantineEnabled_MultipleFlakySpecs_BuildsOrPattern()
    {
        var flaky1 = new FlakySpec
        {
            SpecId = "test:flaky1",
            DisplayName = "spec one",
            StatusChanges = 3,
            TotalRuns = 10,
            PassRate = 0.5
        };
        var flaky2 = new FlakySpec
        {
            SpecId = "test:flaky2",
            DisplayName = "spec two",
            StatusChanges = 4,
            TotalRuns = 10,
            PassRate = 0.3
        };
        _historyService.WithFlakySpecs(flaky1, flaky2);
        var phase = new QuarantinePhase(_historyService);
        var context = CreateContext(quarantine: true);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        // Check for escaped pattern parts (Regex.Escape escapes spaces)
        await Assert.That(filter!.ExcludeName).Contains("spec");
        await Assert.That(filter!.ExcludeName).Contains("one");
        await Assert.That(filter!.ExcludeName).Contains("two");
        await Assert.That(filter!.ExcludeName).Contains("|");
        await Assert.That(_console.Output).Contains("Quarantining 2 flaky spec(s)");
    }

    [Test]
    public async Task ExecuteAsync_QuarantineEnabled_EscapesSpecialCharacters()
    {
        var flakySpec = new FlakySpec
        {
            SpecId = "test:flaky1",
            DisplayName = "Service > should handle (.*) regex chars",
            StatusChanges = 3,
            TotalRuns = 10,
            PassRate = 0.5
        };
        _historyService.WithFlakySpecs(flakySpec);
        var phase = new QuarantinePhase(_historyService);
        var context = CreateContext(quarantine: true);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        // The pattern should have escaped regex special chars
        await Assert.That(filter!.ExcludeName).Contains(@"\(");
        await Assert.That(filter!.ExcludeName).Contains(@"\.\*");
        await Assert.That(filter!.ExcludeName).Contains(@"\)");
    }

    #endregion

    #region Filter Merging Tests

    [Test]
    public async Task ExecuteAsync_ExistingExcludeFilter_MergesPatterns()
    {
        var flakySpec = new FlakySpec
        {
            SpecId = "test:flaky1",
            DisplayName = "flaky spec",
            StatusChanges = 3,
            TotalRuns = 10,
            PassRate = 0.5
        };
        _historyService.WithFlakySpecs(flakySpec);
        var phase = new QuarantinePhase(_historyService);
        var existingFilter = new FilterOptions { ExcludeName = "existing pattern" };
        var context = CreateContext(quarantine: true);
        context.Set(ContextKeys.Filter, existingFilter);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(filter!.ExcludeName).Contains("existing pattern");
        // Regex.Escape escapes spaces
        await Assert.That(filter!.ExcludeName).Contains("flaky");
        await Assert.That(filter!.ExcludeName).Contains("spec");
    }

    [Test]
    public async Task ExecuteAsync_NoExistingFilter_CreatesNewFilter()
    {
        var flakySpec = new FlakySpec
        {
            SpecId = "test:flaky1",
            DisplayName = "flaky spec",
            StatusChanges = 3,
            TotalRuns = 10,
            PassRate = 0.5
        };
        _historyService.WithFlakySpecs(flakySpec);
        var phase = new QuarantinePhase(_historyService);
        var context = CreateContext(quarantine: true);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var filter = context.Get<FilterOptions>(ContextKeys.Filter);
        await Assert.That(filter).IsNotNull();
        await Assert.That(filter!.ExcludeName).StartsWith("^(");
        await Assert.That(filter!.ExcludeName).EndsWith(")$");
    }

    #endregion

    #region History Reuse Tests

    [Test]
    public async Task ExecuteAsync_HistoryAlreadyInContext_ReusesHistory()
    {
        var existingHistory = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry { DisplayName = "spec1", Runs = [] }
            }
        };
        var phase = new QuarantinePhase(_historyService);
        var context = CreateContext(quarantine: true);
        context.Set(ContextKeys.History, existingHistory);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        // Should not have loaded history since it was already in context
        await Assert.That(_historyService.LoadAsyncCalls).IsEmpty();
    }

    [Test]
    public async Task ExecuteAsync_LoadsHistory_SetsInContext()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry { DisplayName = "spec1", Runs = [] }
            }
        };
        _historyService.WithHistory(history);
        var phase = new QuarantinePhase(_historyService);
        var context = CreateContext(quarantine: true);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var loadedHistory = context.Get<SpecHistory>(ContextKeys.History);
        await Assert.That(loadedHistory).IsSameReferenceAs(history);
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task ExecuteAsync_NoProjectPath_ReturnsError()
    {
        var phase = new QuarantinePhase(_historyService);
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        context.Set(ContextKeys.Quarantine, true);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("ProjectPath not set");
    }

    #endregion

    #region Helper Methods

    private CommandContext CreateContext(bool quarantine, string projectPath = "/project/path")
    {
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        context.Set(ContextKeys.Quarantine, quarantine);
        context.Set(ContextKeys.ProjectPath, projectPath);
        return context;
    }

    #endregion
}
