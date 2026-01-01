using DraftSpec.Cli;
using DraftSpec.Cli.History;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.History;

/// <summary>
/// Tests for the SpecHistoryService that tracks test execution history
/// for flaky test detection.
/// </summary>
public class SpecHistoryServiceTests
{
    #region LoadAsync Tests

    [Test]
    public async Task LoadAsync_returns_empty_history_when_file_does_not_exist()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var console = new MockConsole();
        var service = new SpecHistoryService(fileSystem, console);

        // Act
        var history = await service.LoadAsync("/project");

        // Assert
        await Assert.That(history.Specs).IsEmpty();
        await Assert.That(history.Version).IsEqualTo(1);
    }

    [Test]
    public async Task LoadAsync_returns_parsed_history_when_file_exists()
    {
        // Arrange
        var json = """
        {
            "version": 1,
            "updatedAt": "2025-01-15T10:00:00Z",
            "specs": {
                "test.spec.csx:Context/spec1": {
                    "displayName": "Context > spec1",
                    "runs": [
                        { "timestamp": "2025-01-15T10:00:00Z", "status": "passed", "durationMs": 42.5 }
                    ]
                }
            }
        }
        """;
        var fileSystem = new MockFileSystem()
            .AddDirectory("/project/.draftspec")
            .AddFile("/project/.draftspec/history.json", json);
        var console = new MockConsole();
        var service = new SpecHistoryService(fileSystem, console);

        // Act
        var history = await service.LoadAsync("/project");

        // Assert
        await Assert.That(history.Specs).Count().IsEqualTo(1);
        await Assert.That(history.Specs.ContainsKey("test.spec.csx:Context/spec1")).IsTrue();
        await Assert.That(history.Specs["test.spec.csx:Context/spec1"].DisplayName).IsEqualTo("Context > spec1");
    }

    #endregion

    #region SaveAsync Tests

    [Test]
    public async Task SaveAsync_creates_directory_if_not_exists()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var console = new MockConsole();
        var service = new SpecHistoryService(fileSystem, console);
        var history = SpecHistory.Empty;

        // Act
        await service.SaveAsync("/project", history);

        // Assert
        await Assert.That(fileSystem.CreateDirectoryCalls).IsGreaterThan(0);
    }

    [Test]
    public async Task SaveAsync_writes_history_to_file()
    {
        // Arrange
        var fileSystem = new MockFileSystem().AddDirectory("/project/.draftspec");
        var console = new MockConsole();
        var service = new SpecHistoryService(fileSystem, console);
        var now = DateTime.UtcNow;

        var history = new SpecHistory
        {
            Version = 1,
            UpdatedAt = now,
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test.spec.csx:Context/spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "Context > spec1",
                    Runs = [new SpecRun { Timestamp = now, Status = "passed", DurationMs = 42.5 }]
                }
            }
        };

        // Act
        await service.SaveAsync("/project", history);

        // Assert
        await Assert.That(fileSystem.WrittenFiles.ContainsKey(Path.GetFullPath("/project/.draftspec/history.json"))).IsTrue();
    }

    #endregion

    #region RecordRunAsync Tests

    [Test]
    public async Task RecordRunAsync_adds_new_spec_to_history()
    {
        // Arrange
        var fileSystem = new MockFileSystem().AddDirectory("/project/.draftspec");
        var console = new MockConsole();
        var service = new SpecHistoryService(fileSystem, console);

        var results = new List<SpecRunRecord>
        {
            new() { SpecId = "test.spec.csx:Context/spec1", DisplayName = "Context > spec1", Status = "passed", DurationMs = 42.5 }
        };

        // Act
        await service.RecordRunAsync("/project", results);

        // Assert
        var history = await service.LoadAsync("/project");
        await Assert.That(history.Specs).Count().IsEqualTo(1);
        await Assert.That(history.Specs["test.spec.csx:Context/spec1"].Runs).Count().IsEqualTo(1);
        await Assert.That(history.Specs["test.spec.csx:Context/spec1"].Runs[0].Status).IsEqualTo("passed");
    }

    [Test]
    public async Task RecordRunAsync_appends_to_existing_spec_history()
    {
        // Arrange
        var json = """
        {
            "version": 1,
            "updatedAt": "2025-01-14T10:00:00Z",
            "specs": {
                "test.spec.csx:Context/spec1": {
                    "displayName": "Context > spec1",
                    "runs": [
                        { "timestamp": "2025-01-14T10:00:00Z", "status": "failed", "durationMs": 38.2 }
                    ]
                }
            }
        }
        """;
        var fileSystem = new MockFileSystem()
            .AddDirectory("/project/.draftspec")
            .AddFile("/project/.draftspec/history.json", json);
        var console = new MockConsole();
        var service = new SpecHistoryService(fileSystem, console);

        var results = new List<SpecRunRecord>
        {
            new() { SpecId = "test.spec.csx:Context/spec1", DisplayName = "Context > spec1", Status = "passed", DurationMs = 42.5 }
        };

        // Act
        await service.RecordRunAsync("/project", results);

        // Assert
        var history = await service.LoadAsync("/project");
        await Assert.That(history.Specs["test.spec.csx:Context/spec1"].Runs).Count().IsEqualTo(2);
        // Most recent run should be first
        await Assert.That(history.Specs["test.spec.csx:Context/spec1"].Runs[0].Status).IsEqualTo("passed");
    }

    [Test]
    public async Task RecordRunAsync_limits_runs_to_max_entries()
    {
        // Arrange
        var existingRuns = Enumerable.Range(0, 50)
            .Select(i => new SpecRun
            {
                Timestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                Status = "passed",
                DurationMs = 10
            })
            .ToList();

        var fileSystem = new MockFileSystem().AddDirectory("/project/.draftspec");
        var console = new MockConsole();
        var service = new SpecHistoryService(fileSystem, console);

        var history = new SpecHistory
        {
            Version = 1,
            UpdatedAt = DateTime.UtcNow,
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test.spec.csx:Context/spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "Context > spec1",
                    Runs = existingRuns
                }
            }
        };
        await service.SaveAsync("/project", history);

        var results = new List<SpecRunRecord>
        {
            new() { SpecId = "test.spec.csx:Context/spec1", DisplayName = "Context > spec1", Status = "failed", DurationMs = 42.5 }
        };

        // Act
        await service.RecordRunAsync("/project", results);

        // Assert
        var updatedHistory = await service.LoadAsync("/project");
        await Assert.That(updatedHistory.Specs["test.spec.csx:Context/spec1"].Runs).Count().IsEqualTo(SpecHistoryEntry.MaxRuns);
        // Newest run should be first
        await Assert.That(updatedHistory.Specs["test.spec.csx:Context/spec1"].Runs[0].Status).IsEqualTo("failed");
    }

    #endregion

    #region GetFlakySpecs Tests

    [Test]
    public async Task GetFlakySpecs_returns_empty_when_no_flaky_specs()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var console = new MockConsole();
        var service = new SpecHistoryService(fileSystem, console);

        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test.spec.csx:Context/spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "Context > spec1",
                    Runs = [
                        new SpecRun { Status = "passed" },
                        new SpecRun { Status = "passed" },
                        new SpecRun { Status = "passed" }
                    ]
                }
            }
        };

        // Act
        var flakySpecs = service.GetFlakySpecs(history);

        // Assert
        await Assert.That(flakySpecs).IsEmpty();
    }

    [Test]
    public async Task GetFlakySpecs_detects_spec_with_status_changes()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var console = new MockConsole();
        var service = new SpecHistoryService(fileSystem, console);

        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test.spec.csx:Context/spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "Context > spec1",
                    Runs = [
                        new SpecRun { Status = "passed" },
                        new SpecRun { Status = "failed" },
                        new SpecRun { Status = "passed" },
                        new SpecRun { Status = "failed" }
                    ]
                }
            }
        };

        // Act
        var flakySpecs = service.GetFlakySpecs(history);

        // Assert
        await Assert.That(flakySpecs).Count().IsEqualTo(1);
        await Assert.That(flakySpecs[0].SpecId).IsEqualTo("test.spec.csx:Context/spec1");
        await Assert.That(flakySpecs[0].StatusChanges).IsEqualTo(3);
    }

    [Test]
    public async Task GetFlakySpecs_respects_window_size()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var console = new MockConsole();
        var service = new SpecHistoryService(fileSystem, console);

        // Create 20 runs: first 10 alternate, last 10 are stable
        var runs = Enumerable.Range(0, 10).Select(i => new SpecRun { Status = i % 2 == 0 ? "failed" : "passed" })
            .Concat(Enumerable.Range(0, 10).Select(_ => new SpecRun { Status = "passed" }))
            .ToList();

        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test.spec.csx:Context/spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "Context > spec1",
                    Runs = runs
                }
            }
        };

        // Act - with window size 5, should only look at first 5 runs (alternating)
        var flakySpecs = service.GetFlakySpecs(history, minStatusChanges: 2, windowSize: 5);

        // Assert - should detect flakiness in the first 5 runs
        await Assert.That(flakySpecs).Count().IsEqualTo(1);
    }

    [Test]
    public async Task GetFlakySpecs_respects_min_status_changes_threshold()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var console = new MockConsole();
        var service = new SpecHistoryService(fileSystem, console);

        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test.spec.csx:Context/spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "Context > spec1",
                    Runs = [
                        new SpecRun { Status = "passed" },
                        new SpecRun { Status = "failed" },
                        new SpecRun { Status = "passed" } // 2 status changes
                    ]
                }
            }
        };

        // Act - require 3 status changes
        var flakySpecs = service.GetFlakySpecs(history, minStatusChanges: 3, windowSize: 10);

        // Assert
        await Assert.That(flakySpecs).IsEmpty();
    }

    #endregion

    #region GetQuarantinedSpecIds Tests

    [Test]
    public async Task GetQuarantinedSpecIds_returns_flaky_spec_ids()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var console = new MockConsole();
        var service = new SpecHistoryService(fileSystem, console);

        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test.spec.csx:Context/spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "Context > spec1",
                    Runs = [
                        new SpecRun { Status = "passed" },
                        new SpecRun { Status = "failed" },
                        new SpecRun { Status = "passed" },
                        new SpecRun { Status = "failed" }
                    ]
                },
                ["test.spec.csx:Context/spec2"] = new SpecHistoryEntry
                {
                    DisplayName = "Context > spec2",
                    Runs = [
                        new SpecRun { Status = "passed" },
                        new SpecRun { Status = "passed" }
                    ]
                }
            }
        };

        // Act
        var quarantinedIds = service.GetQuarantinedSpecIds(history);

        // Assert
        await Assert.That(quarantinedIds).Count().IsEqualTo(1);
        await Assert.That(quarantinedIds.Contains("test.spec.csx:Context/spec1")).IsTrue();
    }

    #endregion

    #region ClearSpecAsync Tests

    [Test]
    public async Task ClearSpecAsync_removes_spec_from_history()
    {
        // Arrange
        var json = """
        {
            "version": 1,
            "updatedAt": "2025-01-15T10:00:00Z",
            "specs": {
                "test.spec.csx:Context/spec1": {
                    "displayName": "Context > spec1",
                    "runs": [
                        { "timestamp": "2025-01-15T10:00:00Z", "status": "passed", "durationMs": 42.5 }
                    ]
                }
            }
        }
        """;
        var fileSystem = new MockFileSystem()
            .AddDirectory("/project/.draftspec")
            .AddFile("/project/.draftspec/history.json", json);
        var console = new MockConsole();
        var service = new SpecHistoryService(fileSystem, console);

        // Act
        var result = await service.ClearSpecAsync("/project", "test.spec.csx:Context/spec1");

        // Assert
        await Assert.That(result).IsTrue();
        var history = await service.LoadAsync("/project");
        await Assert.That(history.Specs).IsEmpty();
    }

    [Test]
    public async Task ClearSpecAsync_returns_false_when_spec_not_found()
    {
        // Arrange
        var json = """
        {
            "version": 1,
            "updatedAt": "2025-01-15T10:00:00Z",
            "specs": {}
        }
        """;
        var fileSystem = new MockFileSystem()
            .AddDirectory("/project/.draftspec")
            .AddFile("/project/.draftspec/history.json", json);
        var console = new MockConsole();
        var service = new SpecHistoryService(fileSystem, console);

        // Act
        var result = await service.ClearSpecAsync("/project", "nonexistent");

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion
}
