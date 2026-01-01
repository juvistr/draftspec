using System.Text.Json;
using DraftSpec.Formatters;

namespace DraftSpec.Cli.History;

/// <summary>
/// Implementation of spec history service for flaky test detection.
/// </summary>
public sealed class SpecHistoryService : ISpecHistoryService
{
    private const string DraftSpecDirectory = ".draftspec";
    private const string HistoryFileName = "history.json";

    private readonly IFileSystem _fileSystem;
    private readonly IConsole _console;

    public SpecHistoryService(IFileSystem fileSystem, IConsole console)
    {
        _fileSystem = fileSystem;
        _console = console;
    }

    private static string GetHistoryPath(string projectPath)
        => Path.Combine(projectPath, DraftSpecDirectory, HistoryFileName);

    public async Task<SpecHistory> LoadAsync(string projectPath, CancellationToken ct = default)
    {
        var historyPath = GetHistoryPath(projectPath);

        if (!_fileSystem.FileExists(historyPath))
            return SpecHistory.Empty;

        try
        {
            var json = await _fileSystem.ReadAllTextAsync(historyPath, ct);
            return JsonSerializer.Deserialize<SpecHistory>(json, JsonOptionsProvider.Secure)
                   ?? SpecHistory.Empty;
        }
        catch (JsonException ex)
        {
            _console.WriteWarning($"Corrupt history file, starting fresh: {ex.Message}");
            return SpecHistory.Empty;
        }
        catch (IOException ex)
        {
            _console.WriteWarning($"Could not read history file: {ex.Message}");
            return SpecHistory.Empty;
        }
    }

    public async Task SaveAsync(string projectPath, SpecHistory history, CancellationToken ct = default)
    {
        var historyPath = GetHistoryPath(projectPath);
        var directoryPath = Path.GetDirectoryName(historyPath)!;
        var tempPath = historyPath + ".tmp";

        try
        {
            // Ensure directory exists
            if (!_fileSystem.DirectoryExists(directoryPath))
                _fileSystem.CreateDirectory(directoryPath);

            // Update timestamp
            history.UpdatedAt = DateTime.UtcNow;

            // Serialize
            var json = JsonSerializer.Serialize(history, JsonOptionsProvider.Default);

            // Write to temp file first
            await _fileSystem.WriteAllTextAsync(tempPath, json, ct);

            // Atomic rename
            _fileSystem.MoveFile(tempPath, historyPath, overwrite: true);
        }
        catch (IOException ex)
        {
            _console.WriteWarning($"Could not save history file: {ex.Message}");
            // Clean up temp file if it exists
            _fileSystem.DeleteFile(tempPath);
        }
    }

    public async Task RecordRunAsync(
        string projectPath,
        IReadOnlyList<SpecRunRecord> results,
        CancellationToken ct = default)
    {
        if (results.Count == 0)
            return;

        var history = await LoadAsync(projectPath, ct);
        var timestamp = DateTime.UtcNow;

        foreach (var result in results)
        {
            // Skip pending and skipped specs - they don't contribute to flakiness
            if (result.Status is "pending" or "skipped")
                continue;

            if (!history.Specs.TryGetValue(result.SpecId, out var entry))
            {
                entry = new SpecHistoryEntry { DisplayName = result.DisplayName };
                history.Specs[result.SpecId] = entry;
            }

            // Add new run at the beginning (most recent first)
            entry.Runs.Insert(0, new SpecRun
            {
                Timestamp = timestamp,
                Status = result.Status,
                DurationMs = result.DurationMs,
                ErrorMessage = result.ErrorMessage
            });

            // Trim to max runs
            if (entry.Runs.Count > SpecHistoryEntry.MaxRuns)
                entry.Runs.RemoveRange(SpecHistoryEntry.MaxRuns, entry.Runs.Count - SpecHistoryEntry.MaxRuns);
        }

        await SaveAsync(projectPath, history, ct);
    }

    public IReadOnlyList<FlakySpec> GetFlakySpecs(
        SpecHistory history,
        int minStatusChanges = 2,
        int windowSize = 10)
    {
        var flakySpecs = new List<FlakySpec>();

        foreach (var (specId, entry) in history.Specs)
        {
            // Only consider the last N runs
            var recentRuns = entry.Runs.Take(windowSize).ToList();

            if (recentRuns.Count < 2)
                continue; // Not enough data

            // Count status transitions (passed<->failed)
            var statusChanges = 0;
            for (var i = 1; i < recentRuns.Count; i++)
            {
                var prev = recentRuns[i - 1].Status;
                var curr = recentRuns[i].Status;

                // Only count transitions between passed and failed
                if ((prev == "passed" && curr == "failed") ||
                    (prev == "failed" && curr == "passed"))
                {
                    statusChanges++;
                }
            }

            if (statusChanges >= minStatusChanges)
            {
                var passedCount = recentRuns.Count(r => r.Status == "passed");
                flakySpecs.Add(new FlakySpec
                {
                    SpecId = specId,
                    DisplayName = entry.DisplayName,
                    StatusChanges = statusChanges,
                    TotalRuns = recentRuns.Count,
                    PassRate = passedCount / (double)recentRuns.Count,
                    LastSeen = recentRuns.FirstOrDefault()?.Timestamp
                });
            }
        }

        // Order by severity (most status changes first), then by recency
        return flakySpecs
            .OrderByDescending(f => f.StatusChanges)
            .ThenByDescending(f => f.LastSeen)
            .ToList();
    }

    public IReadOnlySet<string> GetQuarantinedSpecIds(
        SpecHistory history,
        int minStatusChanges = 2,
        int windowSize = 10)
    {
        var flakySpecs = GetFlakySpecs(history, minStatusChanges, windowSize);
        return flakySpecs.Select(f => f.SpecId).ToHashSet();
    }

    public async Task<bool> ClearSpecAsync(string projectPath, string specId, CancellationToken ct = default)
    {
        var history = await LoadAsync(projectPath, ct);

        if (!history.Specs.Remove(specId))
            return false;

        await SaveAsync(projectPath, history, ct);
        return true;
    }
}
