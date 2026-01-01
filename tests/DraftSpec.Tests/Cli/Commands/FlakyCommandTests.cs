using DraftSpec.Cli.Commands;
using DraftSpec.Cli.History;
using DraftSpec.Cli.Options;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for FlakyCommand which lists detected flaky specs based on execution history.
/// </summary>
public class FlakyCommandTests
{
    private string _tempDir = null!;
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_flaky_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _console = new MockConsole();
        _fileSystem = new MockFileSystem().AddDirectory(_tempDir);
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region Directory Validation Tests

    [Test]
    public async Task ExecuteAsync_DirectoryNotFound_ThrowsArgumentException()
    {
        var historyService = new MockSpecHistoryService();
        var command = CreateCommand(historyService);

        var options = new FlakyOptions { Path = "/nonexistent/path" };

        await Assert.ThrowsAsync<ArgumentException>(async () => await command.ExecuteAsync(options));
    }

    #endregion

    #region Clear Option Tests

    [Test]
    public async Task ExecuteAsync_ClearOption_Success_ShowsSuccessMessage()
    {
        var historyService = new MockSpecHistoryService()
            .WithClearSpecResult(true);
        var command = CreateCommand(historyService);

        var options = new FlakyOptions
        {
            Path = _tempDir,
            Clear = "test.spec.csx:Context/spec1"
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("Cleared history for");
        await Assert.That(_console.Output).Contains("test.spec.csx:Context/spec1");
    }

    [Test]
    public async Task ExecuteAsync_ClearOption_NotFound_ShowsWarning()
    {
        var historyService = new MockSpecHistoryService()
            .WithClearSpecResult(false);
        var command = CreateCommand(historyService);

        var options = new FlakyOptions
        {
            Path = _tempDir,
            Clear = "nonexistent-spec"
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Output).Contains("No history found for");
        await Assert.That(_console.Output).Contains("nonexistent-spec");
    }

    #endregion

    #region Empty History Tests

    [Test]
    public async Task ExecuteAsync_NoHistory_ShowsRunSpecsFirstMessage()
    {
        var historyService = new MockSpecHistoryService()
            .WithHistory(SpecHistory.Empty);
        var command = CreateCommand(historyService);

        var options = new FlakyOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

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
        var historyService = new MockSpecHistoryService()
            .WithHistory(history);
        // Don't add flaky specs, so GetFlakySpecs returns empty
        var command = CreateCommand(historyService);

        var options = new FlakyOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

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
            .WithHistory(history)
            .WithFlakySpecs(flakySpec);
        var command = CreateCommand(historyService);

        var options = new FlakyOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("Flaky Tests Detected: 1");
        await Assert.That(_console.Output).Contains("Context > spec1");
        await Assert.That(_console.Output).Contains("60%");
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
            .WithHistory(history)
            .WithFlakySpecs(mediumFlaky, highFlaky);
        var command = CreateCommand(historyService);

        var options = new FlakyOptions { Path = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("MEDIUM");
        await Assert.That(_console.Output).Contains("HIGH");
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
            .WithHistory(history)
            .WithFlakySpecs(flakySpec);
        var command = CreateCommand(historyService);

        var options = new FlakyOptions { Path = _tempDir };

        await command.ExecuteAsync(options);

        await Assert.That(_console.Output).Contains("Recommendations");
        await Assert.That(_console.Output).Contains("--quarantine");
        await Assert.That(_console.Output).Contains("--clear");
    }

    #endregion

    #region Window Size and Min Changes Tests

    [Test]
    public async Task ExecuteAsync_UsesCustomWindowSize()
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
            .WithHistory(history)
            .WithFlakySpecs(flakySpec);
        var command = CreateCommand(historyService);

        var options = new FlakyOptions
        {
            Path = _tempDir,
            WindowSize = 20
        };

        await command.ExecuteAsync(options);

        await Assert.That(_console.Output).Contains("last 20 runs per spec");
    }

    #endregion

    #region Helper Methods

    private FlakyCommand CreateCommand(MockSpecHistoryService? historyService = null)
    {
        return new FlakyCommand(
            historyService ?? new MockSpecHistoryService(),
            _console,
            _fileSystem);
    }

    #endregion
}
