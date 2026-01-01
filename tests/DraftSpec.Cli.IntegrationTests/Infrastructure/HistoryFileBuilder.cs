using System.Text.Json;

namespace DraftSpec.Cli.IntegrationTests.Infrastructure;

/// <summary>
/// Fluent builder for creating .draftspec/history.json files for integration tests.
/// </summary>
public class HistoryFileBuilder
{
    private readonly string _projectDir;
    private readonly Dictionary<string, SpecEntryData> _specs = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public HistoryFileBuilder(string projectDir)
    {
        _projectDir = projectDir;
    }

    /// <summary>
    /// Adds a spec with consistent passing history.
    /// </summary>
    public HistoryFileBuilder WithStableSpec(string specId, string displayName, int runCount = 5)
    {
        var runs = Enumerable.Range(0, runCount)
            .Select(i => new RunData
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-i),
                Status = "passed",
                DurationMs = 100.0 + i * 10
            })
            .ToList();

        _specs[specId] = new SpecEntryData { DisplayName = displayName, Runs = runs };
        return this;
    }

    /// <summary>
    /// Adds a spec with alternating pass/fail history (flaky pattern).
    /// </summary>
    public HistoryFileBuilder WithFlakySpec(string specId, string displayName, int statusChanges = 3)
    {
        // Create runs with alternating status to produce the specified number of changes
        var runs = new List<RunData>();
        var currentStatus = "passed";

        for (var i = 0; i <= statusChanges + 2; i++)
        {
            runs.Add(new RunData
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-i),
                Status = currentStatus,
                DurationMs = 100.0 + i * 10
            });

            // Toggle status to create a change
            if (i < statusChanges)
            {
                currentStatus = currentStatus == "passed" ? "failed" : "passed";
            }
        }

        _specs[specId] = new SpecEntryData { DisplayName = displayName, Runs = runs };
        return this;
    }

    /// <summary>
    /// Adds a spec with custom run history (e.g., "passed", "failed", "passed").
    /// </summary>
    public HistoryFileBuilder WithSpec(string specId, string displayName, params string[] statuses)
    {
        var runs = statuses.Select((status, i) => new RunData
        {
            Timestamp = DateTime.UtcNow.AddMinutes(-i),
            Status = status,
            DurationMs = 100.0 + i * 10
        }).ToList();

        _specs[specId] = new SpecEntryData { DisplayName = displayName, Runs = runs };
        return this;
    }

    /// <summary>
    /// Adds a spec with specific timing data for estimate tests.
    /// </summary>
    public HistoryFileBuilder WithTimedSpec(string specId, string displayName, params int[] durationsMs)
    {
        var runs = durationsMs.Select((duration, i) => new RunData
        {
            Timestamp = DateTime.UtcNow.AddMinutes(-i),
            Status = "passed",
            DurationMs = duration
        }).ToList();

        _specs[specId] = new SpecEntryData { DisplayName = displayName, Runs = runs };
        return this;
    }

    /// <summary>
    /// Builds the history file and returns the project directory path.
    /// </summary>
    public string Build()
    {
        var draftspecDir = Path.Combine(_projectDir, ".draftspec");
        Directory.CreateDirectory(draftspecDir);

        var history = new HistoryData
        {
            Version = 1,
            UpdatedAt = DateTime.UtcNow,
            Specs = _specs
        };

        var json = JsonSerializer.Serialize(history, JsonOptions);
        File.WriteAllText(Path.Combine(draftspecDir, "history.json"), json);

        return _projectDir;
    }

    // Internal data classes matching the SpecHistory JSON schema
    private sealed class HistoryData
    {
        public int Version { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Dictionary<string, SpecEntryData> Specs { get; set; } = new();
    }

    private sealed class SpecEntryData
    {
        public string DisplayName { get; set; } = "";
        public List<RunData> Runs { get; set; } = new();
    }

    private sealed class RunData
    {
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = "";
        public double DurationMs { get; set; }
    }
}
