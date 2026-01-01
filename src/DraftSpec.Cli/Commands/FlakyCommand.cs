using DraftSpec.Cli.History;
using DraftSpec.Cli.Options;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Lists detected flaky specs based on execution history.
/// </summary>
public class FlakyCommand : ICommand<FlakyOptions>
{
    private readonly ISpecHistoryService _historyService;
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public FlakyCommand(
        ISpecHistoryService historyService,
        IConsole console,
        IFileSystem fileSystem)
    {
        _historyService = historyService;
        _console = console;
        _fileSystem = fileSystem;
    }

    public async Task<int> ExecuteAsync(FlakyOptions options, CancellationToken ct = default)
    {
        var projectPath = Path.GetFullPath(options.Path);

        if (!_fileSystem.DirectoryExists(projectPath))
            throw new ArgumentException($"Directory not found: {projectPath}");

        // Handle clear operation
        if (!string.IsNullOrEmpty(options.Clear))
        {
            var cleared = await _historyService.ClearSpecAsync(projectPath, options.Clear, ct);
            if (cleared)
            {
                _console.WriteSuccess($"Cleared history for: {options.Clear}");
                return 0;
            }
            else
            {
                _console.WriteWarning($"No history found for: {options.Clear}");
                return 1;
            }
        }

        // Load history
        var history = await _historyService.LoadAsync(projectPath, ct);

        if (history.Specs.Count == 0)
        {
            _console.WriteLine("No test history found. Run specs first to collect data.");
            _console.WriteLine();
            _console.ForegroundColor = ConsoleColor.DarkGray;
            _console.WriteLine("  draftspec run");
            _console.ResetColor();
            return 0;
        }

        // Detect flaky specs
        var flakySpecs = _historyService.GetFlakySpecs(
            history,
            options.MinStatusChanges,
            options.WindowSize);

        if (flakySpecs.Count == 0)
        {
            _console.WriteSuccess("No flaky specs detected.");
            _console.WriteLine();
            _console.ForegroundColor = ConsoleColor.DarkGray;
            _console.WriteLine($"  Analyzed {history.Specs.Count} specs with {options.MinStatusChanges}+ status changes threshold");
            _console.ResetColor();
            return 0;
        }

        // Display results
        ShowFlakySpecs(flakySpecs, options, history.Specs.Count);

        return 0;
    }

    private void ShowFlakySpecs(
        IReadOnlyList<FlakySpec> specs,
        FlakyOptions options,
        int totalSpecs)
    {
        _console.WriteLine($"Analyzing test history (last {options.WindowSize} runs per spec)...");
        _console.WriteLine();

        _console.ForegroundColor = ConsoleColor.Yellow;
        _console.WriteLine($"Flaky Tests Detected: {specs.Count}");
        _console.ResetColor();
        _console.WriteLine();

        var index = 1;
        foreach (var spec in specs)
        {
            // Spec name
            _console.ForegroundColor = ConsoleColor.White;
            _console.WriteLine($"{index}. {spec.DisplayName}");
            _console.ResetColor();

            // History visualization
            var history = GetHistoryVisualization(spec);
            _console.WriteLine($"   History: {history} ({spec.PassRate:P0} pass rate)");

            // Severity
            var severityColor = spec.Severity switch
            {
                "HIGH" => ConsoleColor.Red,
                "MEDIUM" => ConsoleColor.Yellow,
                _ => ConsoleColor.DarkGray
            };
            _console.ForegroundColor = severityColor;
            _console.Write($"   Flakiness: {spec.Severity}");
            _console.ResetColor();
            _console.WriteLine($" - {spec.StatusChanges} status change(s) in last {spec.TotalRuns} runs");

            _console.WriteLine();
            index++;
        }

        // Recommendations
        _console.ForegroundColor = ConsoleColor.DarkGray;
        _console.WriteLine("Recommendations:");
        _console.WriteLine("  - Fix or quarantine HIGH flakiness tests");
        _console.WriteLine("  - Use --quarantine flag to skip flaky specs during runs");
        _console.WriteLine("  - Use --clear \"<spec-id>\" to reset history for a spec");
        _console.ResetColor();
    }

    private static string GetHistoryVisualization(FlakySpec spec)
    {
        // This is a simplified visualization - we don't have the full run history here
        // In a real implementation, we'd pass the runs array to show the actual pattern
        var passCount = (int)(spec.TotalRuns * spec.PassRate);
        var failCount = spec.TotalRuns - passCount;

        // Create a simple representation
        var chars = new List<char>();
        for (var i = 0; i < passCount; i++) chars.Add('\u2713'); // checkmark
        for (var i = 0; i < failCount; i++) chars.Add('\u2717'); // x mark

        // Shuffle to show alternation (this is approximate)
        return string.Join("", chars.Take(spec.TotalRuns));
    }
}
