using DraftSpec.Cli.History;

namespace DraftSpec.Cli.Pipeline.Phases.Flaky;

/// <summary>
/// Detects and displays flaky specs based on historical execution data.
/// Also handles the --clear operation to reset history for a specific spec.
/// </summary>
/// <remarks>
/// Requires: <see cref="ContextKeys.ProjectPath"/>, <see cref="ContextKeys.History"/>
/// Optional: <see cref="ContextKeys.MinStatusChanges"/>, <see cref="ContextKeys.WindowSize"/>, <see cref="ContextKeys.Clear"/>
/// </remarks>
public class FlakyOutputPhase : ICommandPhase
{
    private readonly ISpecHistoryService _historyService;

    public FlakyOutputPhase(ISpecHistoryService historyService)
    {
        _historyService = historyService;
    }

    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var projectPath = context.Get<string>(ContextKeys.ProjectPath)!;
        var history = context.Get<SpecHistory>(ContextKeys.History)!;
        var minStatusChanges = context.Get<int>(ContextKeys.MinStatusChanges);
        var windowSize = context.Get<int>(ContextKeys.WindowSize);
        var clear = context.Get<string>(ContextKeys.Clear);
        var console = context.Console;

        // Handle clear operation first
        if (!string.IsNullOrEmpty(clear))
        {
            var cleared = await _historyService.ClearSpecAsync(projectPath, clear, ct);
            if (cleared)
            {
                console.WriteSuccess($"Cleared history for: {clear}");
                return 0;
            }
            else
            {
                console.WriteWarning($"No history found for: {clear}");
                return 1;
            }
        }

        if (history.Specs.Count == 0)
        {
            console.WriteLine("No test history found. Run specs first to collect data.");
            console.WriteLine();
            console.ForegroundColor = ConsoleColor.DarkGray;
            console.WriteLine("  draftspec run");
            console.ResetColor();
            return 0;
        }

        // Detect flaky specs
        var flakySpecs = _historyService.GetFlakySpecs(history, minStatusChanges, windowSize);

        if (flakySpecs.Count == 0)
        {
            console.WriteSuccess("No flaky specs detected.");
            console.WriteLine();
            console.ForegroundColor = ConsoleColor.DarkGray;
            console.WriteLine($"  Analyzed {history.Specs.Count} specs with {minStatusChanges}+ status changes threshold");
            console.ResetColor();
            return 0;
        }

        // Display results
        ShowFlakySpecs(console, flakySpecs, minStatusChanges, windowSize, history.Specs.Count);

        return 0;
    }

    private static void ShowFlakySpecs(
        IConsole console,
        IReadOnlyList<FlakySpec> specs,
        int minStatusChanges,
        int windowSize,
        int totalSpecs)
    {
        console.WriteLine($"Analyzing test history (last {windowSize} runs per spec)...");
        console.WriteLine();

        console.ForegroundColor = ConsoleColor.Yellow;
        console.WriteLine($"Flaky Tests Detected: {specs.Count}");
        console.ResetColor();
        console.WriteLine();

        var index = 1;
        foreach (var spec in specs)
        {
            // Spec name
            console.ForegroundColor = ConsoleColor.White;
            console.WriteLine($"{index}. {spec.DisplayName}");
            console.ResetColor();

            // History visualization
            var history = GetHistoryVisualization(spec);
            console.WriteLine($"   History: {history} ({spec.PassRate:P0} pass rate)");

            // Severity
            var severityColor = spec.Severity switch
            {
                "HIGH" => ConsoleColor.Red,
                "MEDIUM" => ConsoleColor.Yellow,
                _ => ConsoleColor.DarkGray
            };
            console.ForegroundColor = severityColor;
            console.Write($"   Flakiness: {spec.Severity}");
            console.ResetColor();
            console.WriteLine($" - {spec.StatusChanges} status change(s) in last {spec.TotalRuns} runs");

            console.WriteLine();
            index++;
        }

        // Recommendations
        console.ForegroundColor = ConsoleColor.DarkGray;
        console.WriteLine("Recommendations:");
        console.WriteLine("  - Fix or quarantine HIGH flakiness tests");
        console.WriteLine("  - Use --quarantine flag to skip flaky specs during runs");
        console.WriteLine("  - Use --clear \"<spec-id>\" to reset history for a spec");
        console.ResetColor();
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
