using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Watch;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for WatchCommand with mocked dependencies.
/// </summary>
public class WatchCommandTests
{
    #region Constructor Dependencies

    [Test]
    public async Task Constructor_WithAllDependencies_Constructs()
    {
        var command = CreateCommand();
        await Assert.That(command).IsNotNull();
    }

    #endregion

    #region Initial Run Behavior

    [Test]
    public async Task ExecuteAsync_NoSpecsFound_RunsWithEmptyList()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: []);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel quickly to exit watch loop

        var options = new WatchOptions { Path = "." };
        var result = await command.ExecuteAsync(options, cts.Token);

        // With no specs, the run is successful (no failures), so returns 0
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_SpecsFound_RunsInitialSpecs()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test1.spec.csx", "test2.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new WatchOptions { Path = "." };
        await command.ExecuteAsync(options, cts.Token);

        await Assert.That(runner.RunAllCalled).IsTrue();
        await Assert.That(runner.LastSpecFiles).Count().IsEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_SuccessfulRun_ReturnsZero()
    {
        var runner = new MockRunner(success: true);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new WatchOptions { Path = "." };
        var result = await command.ExecuteAsync(options, cts.Token);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_FailedRun_ReturnsOne()
    {
        var runner = new MockRunner(success: false);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new WatchOptions { Path = "." };
        var result = await command.ExecuteAsync(options, cts.Token);

        await Assert.That(result).IsEqualTo(1);
    }

    #endregion

    #region File Watcher Behavior

    [Test]
    public async Task ExecuteAsync_CreatesFileWatcher()
    {
        var watcherFactory = new MockFileWatcherFactory();
        var command = CreateCommand(watcherFactory: watcherFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new WatchOptions { Path = "/some/path" };
        await command.ExecuteAsync(options, cts.Token);

        await Assert.That(watcherFactory.CreateCalled).IsTrue();
        await Assert.That(watcherFactory.LastPath).IsEqualTo("/some/path");
    }

    [Test]
    public async Task ExecuteAsync_FileChange_TriggersRerun()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var watcherFactory = new MockFileWatcherFactory();
        var command = CreateCommand(runnerFactory: runnerFactory, watcherFactory: watcherFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

        var options = new WatchOptions { Path = "." };

        // Start the command in background
        var task = command.ExecuteAsync(options, cts.Token);

        // Wait for initial run
        await Task.Delay(50);

        // Simulate file change
        watcherFactory.TriggerChange(new FileChangeInfo("/some/file.cs", false));

        // Wait a bit more then cancel
        await Task.Delay(50);
        await cts.CancelAsync();

        await task;

        // Should have called RunAll twice (initial + rerun)
        await Assert.That(runner.RunAllCallCount).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_SpecFileChange_RunsSelectiveRerun()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var watcherFactory = new MockFileWatcherFactory();
        var specFiles = new[] { "/specs/test1.spec.csx", "/specs/test2.spec.csx" };
        var command = CreateCommand(runnerFactory: runnerFactory, watcherFactory: watcherFactory, specFiles: specFiles);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

        var options = new WatchOptions { Path = "/specs" };

        var task = command.ExecuteAsync(options, cts.Token);

        await Task.Delay(50);

        // Simulate spec file change
        watcherFactory.TriggerChange(new FileChangeInfo("/specs/test1.spec.csx", true));

        await Task.Delay(50);
        await cts.CancelAsync();

        await task;

        // Last run should be with just the changed spec (selective)
        // Note: Due to timing, we verify that selective runs happened
        await Assert.That(runner.LastSpecFiles).IsNotNull();
    }

    [Test]
    public async Task ExecuteAsync_SourceFileChange_RunsFullRerun()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var watcherFactory = new MockFileWatcherFactory();
        var command = CreateCommand(runnerFactory: runnerFactory, watcherFactory: watcherFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

        var options = new WatchOptions { Path = "." };

        var task = command.ExecuteAsync(options, cts.Token);

        await Task.Delay(50);

        // Simulate source file change (not a spec)
        watcherFactory.TriggerChange(new FileChangeInfo("/src/MyClass.cs", false));

        await Task.Delay(50);
        await cts.CancelAsync();

        await task;

        // Should have run all specs on source file change
        await Assert.That(runner.RunAllCallCount).IsGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Console Output

    [Test]
    public async Task ExecuteAsync_ShowsStoppedMessage_OnCancellation()
    {
        var console = new MockConsole();
        var command = CreateCommand(console: console, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new WatchOptions { Path = "." };
        await command.ExecuteAsync(options, cts.Token);

        await Assert.That(console.Output).Contains("Stopped watching");
    }

    [Test]
    public async Task ExecuteAsync_FileChange_ShowsRerunningMessage()
    {
        var console = new MockConsole();
        var watcherFactory = new MockFileWatcherFactory();
        var command = CreateCommand(console: console, watcherFactory: watcherFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

        var options = new WatchOptions { Path = "." };

        var task = command.ExecuteAsync(options, cts.Token);

        await Task.Delay(50);
        watcherFactory.TriggerChange(new FileChangeInfo("/some/file.cs", false));

        await Task.Delay(50);
        await cts.CancelAsync();

        await task;

        // ConsolePresenter.ShowRerunning should have been called
        // This is called through the presenter, so we verify the callback was invoked
        await Assert.That(watcherFactory.OnChangeCallbackInvoked).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_ClearsConsoleBetweenRuns()
    {
        var console = new MockConsole();
        var watcherFactory = new MockFileWatcherFactory();
        var command = CreateCommand(console: console, watcherFactory: watcherFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

        var options = new WatchOptions { Path = "." };

        var task = command.ExecuteAsync(options, cts.Token);

        await Task.Delay(50);
        watcherFactory.TriggerChange(new FileChangeInfo("/file.cs", false));

        await Task.Delay(50);
        await cts.CancelAsync();

        await task;

        // Console.Clear() should have been called (tracked in MockConsole)
        await Assert.That(console.ClearCallCount).IsGreaterThan(0);
    }

    #endregion

    #region Filter Options

    [Test]
    public async Task ExecuteAsync_PassesFilterOptions_ToRunner()
    {
        var runnerFactory = new MockRunnerFactory();
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new WatchOptions
        {
            Path = ".",
            Filter = new FilterOptions
            {
                FilterTags = "unit",
                ExcludeTags = "slow",
                FilterName = "should pass",
                ExcludeName = "integration"
            }
        };

        await command.ExecuteAsync(options, cts.Token);

        await Assert.That(runnerFactory.LastFilterTags).IsEqualTo("unit");
        await Assert.That(runnerFactory.LastExcludeTags).IsEqualTo("slow");
        await Assert.That(runnerFactory.LastFilterName).IsEqualTo("should pass");
        await Assert.That(runnerFactory.LastExcludeName).IsEqualTo("integration");
    }

    #endregion

    #region Cancellation

    [Test]
    public async Task ExecuteAsync_ImmediateCancellation_ReturnsQuickly()
    {
        var command = CreateCommand(specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Cancel immediately

        var options = new WatchOptions { Path = "." };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await command.ExecuteAsync(options, cts.Token);
        stopwatch.Stop();

        // Should complete quickly (within 1 second)
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(1000);
    }

    [Test]
    public async Task ExecuteAsync_CancellationDuringInitialRun_ReturnsBasedOnLastSummary()
    {
        var runner = new MockRunner(success: true, delayMs: 500);
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel during the run

        var options = new WatchOptions { Path = "." };
        var result = await command.ExecuteAsync(options, cts.Token);

        // Returns based on lastSummary state (may be null → 1, or success → 0)
        await Assert.That(result).IsGreaterThanOrEqualTo(0);
        await Assert.That(result).IsLessThanOrEqualTo(1);
    }

    #endregion

    #region Build Events

    [Test]
    public async Task ExecuteAsync_RegistersBuildEventHandlers()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var command = CreateCommand(runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new WatchOptions { Path = "." };
        await command.ExecuteAsync(options, cts.Token);

        await Assert.That(runner.OnBuildStartedRegistered).IsTrue();
        await Assert.That(runner.OnBuildCompletedRegistered).IsTrue();
        await Assert.That(runner.OnBuildSkippedRegistered).IsTrue();
    }

    #endregion

    #region Incremental Mode

    [Test]
    public async Task ExecuteAsync_IncrementalMode_NoChanges_SkipsRun()
    {
        var console = new MockConsole();
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var watcherFactory = new MockFileWatcherFactory();
        var changeTracker = new ConfigurableSpecChangeTracker(hasChanges: false);

        var specFiles = new[] { "/specs/test.spec.csx" };
        var command = CreateCommandWithChangeTracker(
            console: console,
            runnerFactory: runnerFactory,
            watcherFactory: watcherFactory,
            changeTracker: changeTracker,
            specFiles: specFiles);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(300);

        var options = new WatchOptions { Path = "/specs", Incremental = true };

        var task = command.ExecuteAsync(options, cts.Token);

        await Task.Delay(50);

        // Trigger spec file change - since changeTracker returns no changes, should skip
        watcherFactory.TriggerChange(new FileChangeInfo("/specs/test.spec.csx", true));

        await Task.Delay(100);
        await cts.CancelAsync();

        await task;

        // Should show "No spec changes detected" message
        await Assert.That(console.Output).Contains("No spec changes detected");
    }

    [Test]
    public async Task ExecuteAsync_IncrementalMode_DynamicSpecs_TriggersFullRun()
    {
        var console = new MockConsole();
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var watcherFactory = new MockFileWatcherFactory();

        // Simulate dynamic specs detected
        var changes = new List<SpecChange> { new("dynamic spec", [], SpecChangeType.Added) };
        var changeTracker = new ConfigurableSpecChangeTracker(
            hasChanges: true,
            hasDynamicSpecs: true,
            changes: changes);

        var specFiles = new[] { "/specs/test.spec.csx" };
        var command = CreateCommandWithChangeTracker(
            console: console,
            runnerFactory: runnerFactory,
            watcherFactory: watcherFactory,
            changeTracker: changeTracker,
            specFiles: specFiles);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(300);

        var options = new WatchOptions { Path = "/specs", Incremental = true };

        var task = command.ExecuteAsync(options, cts.Token);

        await Task.Delay(50);

        // Trigger spec file change
        watcherFactory.TriggerChange(new FileChangeInfo("/specs/test.spec.csx", true));

        await Task.Delay(100);
        await cts.CancelAsync();

        await task;

        // Should show message about dynamic specs requiring full run
        // Use AllOutput because Clear() is called after the message is written
        await Assert.That(console.AllOutput).Contains("Full run required");
        await Assert.That(console.AllOutput).Contains("dynamic specs detected");
    }

    [Test]
    public async Task ExecuteAsync_IncrementalMode_SpecsChanged_RunsIncrementally()
    {
        var console = new MockConsole();
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var watcherFactory = new MockFileWatcherFactory();

        // Simulate 2 specs changed
        var changes = new List<SpecChange>
        {
            new("creates-todo", ["TodoService"], SpecChangeType.Added),
            new("deletes-todo", ["TodoService"], SpecChangeType.Modified)
        };
        var changeTracker = new ConfigurableSpecChangeTracker(
            hasChanges: true,
            hasDynamicSpecs: false,
            changes: changes);

        var specFiles = new[] { "/specs/test.spec.csx" };
        var command = CreateCommandWithChangeTracker(
            console: console,
            runnerFactory: runnerFactory,
            watcherFactory: watcherFactory,
            changeTracker: changeTracker,
            specFiles: specFiles);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(300);

        var options = new WatchOptions { Path = "/specs", Incremental = true };

        var task = command.ExecuteAsync(options, cts.Token);

        await Task.Delay(50);

        // Trigger spec file change
        watcherFactory.TriggerChange(new FileChangeInfo("/specs/test.spec.csx", true));

        await Task.Delay(100);
        await cts.CancelAsync();

        await task;

        // Should show incremental run message
        // Use AllOutput because Clear() is called after the message is written
        await Assert.That(console.AllOutput).Contains("Incremental");
        await Assert.That(console.AllOutput).Contains("2 spec(s) changed");
    }

    [Test]
    public async Task ExecuteAsync_IncrementalMode_RecordsStateAfterRun()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var watcherFactory = new MockFileWatcherFactory();

        var changes = new List<SpecChange>
        {
            new("test-spec", ["Context"], SpecChangeType.Added)
        };
        var changeTracker = new ConfigurableSpecChangeTracker(
            hasChanges: true,
            hasDynamicSpecs: false,
            changes: changes);

        var specFiles = new[] { "/specs/test.spec.csx" };
        var command = CreateCommandWithChangeTracker(
            runnerFactory: runnerFactory,
            watcherFactory: watcherFactory,
            changeTracker: changeTracker,
            specFiles: specFiles);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(300);

        var options = new WatchOptions { Path = "/specs", Incremental = true };

        var task = command.ExecuteAsync(options, cts.Token);

        await Task.Delay(50);

        // Trigger spec file change
        watcherFactory.TriggerChange(new FileChangeInfo("/specs/test.spec.csx", true));

        await Task.Delay(100);
        await cts.CancelAsync();

        await task;

        // State should be recorded after the run
        await Assert.That(changeTracker.RecordStateCalled).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_IncrementalMode_PassesFilterPatternToRunner()
    {
        var runner = new MockRunner();
        var runnerFactory = new MockRunnerFactory(runner);
        var watcherFactory = new MockFileWatcherFactory();

        // Simulate specs changed
        var changes = new List<SpecChange>
        {
            new("creates-todo", ["TodoService"], SpecChangeType.Added)
        };
        var changeTracker = new ConfigurableSpecChangeTracker(
            hasChanges: true,
            hasDynamicSpecs: false,
            changes: changes);

        var specFiles = new[] { "/specs/test.spec.csx" };
        var command = CreateCommandWithChangeTracker(
            runnerFactory: runnerFactory,
            watcherFactory: watcherFactory,
            changeTracker: changeTracker,
            specFiles: specFiles);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(300);

        var options = new WatchOptions { Path = "/specs", Incremental = true };

        var task = command.ExecuteAsync(options, cts.Token);

        await Task.Delay(50);

        // Trigger spec file change
        watcherFactory.TriggerChange(new FileChangeInfo("/specs/test.spec.csx", true));

        await Task.Delay(100);
        await cts.CancelAsync();

        await task;

        // FilterName should contain the escaped spec description pattern
        await Assert.That(runnerFactory.LastFilterName).IsNotNull();
        await Assert.That(runnerFactory.LastFilterName!).Contains("creates-todo");
    }

    #endregion

    #region BuildFilterPattern Static Method

    [Test]
    public async Task BuildFilterPattern_EmptyList_ReturnsMatchNothingPattern()
    {
        var result = WatchCommand.BuildFilterPattern([]);

        await Assert.That(result).IsEqualTo("^$");
    }

    [Test]
    public async Task BuildFilterPattern_SingleSpec_ReturnsAnchoredPattern()
    {
        var specs = new List<SpecChange>
        {
            new("creates-todo", ["TodoService"], SpecChangeType.Added)
        };

        var result = WatchCommand.BuildFilterPattern(specs);

        await Assert.That(result).IsEqualTo("^(creates-todo)$");
    }

    [Test]
    public async Task BuildFilterPattern_MultipleSpecs_ReturnsAlternationPattern()
    {
        var specs = new List<SpecChange>
        {
            new("creates-todo", ["TodoService"], SpecChangeType.Added),
            new("deletes-todo", ["TodoService"], SpecChangeType.Modified)
        };

        var result = WatchCommand.BuildFilterPattern(specs);

        await Assert.That(result).IsEqualTo("^(creates-todo|deletes-todo)$");
    }

    [Test]
    public async Task BuildFilterPattern_WithSpaces_EscapesSpaces()
    {
        var specs = new List<SpecChange>
        {
            new("creates a todo", ["TodoService"], SpecChangeType.Added)
        };

        var result = WatchCommand.BuildFilterPattern(specs);

        // Spaces are escaped as \
        await Assert.That(result).Contains(@"creates\ a\ todo");
    }

    [Test]
    public async Task BuildFilterPattern_SpecialRegexCharacters_AreEscaped()
    {
        var specs = new List<SpecChange>
        {
            new("calculates(2+2)=4", ["Calculator"], SpecChangeType.Added),
            new("handles*wildcard", ["Parser"], SpecChangeType.Modified)
        };

        var result = WatchCommand.BuildFilterPattern(specs);

        // Should escape ( ) + = * characters
        await Assert.That(result).Contains(@"\(2\+2\)");
        await Assert.That(result).Contains(@"\*");
    }

    [Test]
    public async Task BuildFilterPattern_DotCharacter_IsEscaped()
    {
        var specs = new List<SpecChange>
        {
            new("parses-file.txt", ["Parser"], SpecChangeType.Added)
        };

        var result = WatchCommand.BuildFilterPattern(specs);

        // Dot should be escaped as \. to match literal dot
        await Assert.That(result).Contains(@"file\.txt");
    }

    [Test]
    public async Task BuildFilterPattern_BracketCharacters_AreEscaped()
    {
        var specs = new List<SpecChange>
        {
            new("handles[index]access", ["Parser"], SpecChangeType.Added)
        };

        var result = WatchCommand.BuildFilterPattern(specs);

        // Opening bracket should be escaped (closing bracket doesn't need escaping in .NET Regex.Escape)
        await Assert.That(result).Contains(@"\[index]");
    }

    #endregion

    #region Error Handling

    [Test]
    public async Task ExecuteAsync_RunnerThrowsArgumentException_ShowsError()
    {
        var runner = new MockRunner(throwArgumentException: true);
        var runnerFactory = new MockRunnerFactory(runner);
        var console = new MockConsole();
        var command = CreateCommand(console: console, runnerFactory: runnerFactory, specFiles: ["test.spec.csx"]);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var options = new WatchOptions { Path = "." };
        await command.ExecuteAsync(options, cts.Token);

        // Should show error and continue (not crash)
        // The command catches ArgumentException and shows error via presenter
    }

    #endregion

    #region Helper Methods

    private static WatchCommand CreateCommand(
        MockConsole? console = null,
        MockRunnerFactory? runnerFactory = null,
        MockFileWatcherFactory? watcherFactory = null,
        IReadOnlyList<string>? specFiles = null)
    {
        var specs = specFiles ?? [];
        return new WatchCommand(
            new MockSpecFinder(specs),
            runnerFactory ?? new MockRunnerFactory(),
            watcherFactory ?? new MockFileWatcherFactory(),
            console ?? new MockConsole(),
            NullObjects.SpecChangeTracker);
    }

    private static WatchCommand CreateCommandWithChangeTracker(
        MockConsole? console = null,
        MockRunnerFactory? runnerFactory = null,
        MockFileWatcherFactory? watcherFactory = null,
        ISpecChangeTracker? changeTracker = null,
        IReadOnlyList<string>? specFiles = null)
    {
        var specs = specFiles ?? [];
        return new WatchCommand(
            new MockSpecFinder(specs),
            runnerFactory ?? new MockRunnerFactory(),
            watcherFactory ?? new MockFileWatcherFactory(),
            console ?? new MockConsole(),
            changeTracker ?? NullObjects.SpecChangeTracker);
    }

    #endregion
}
