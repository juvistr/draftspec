using DraftSpec.Cli.History;

namespace DraftSpec.Cli.Pipeline.Phases.Estimate;

/// <summary>
/// Calculates and displays runtime estimates based on historical execution data.
/// </summary>
/// <remarks>
/// Requires: <see cref="ContextKeys.History"/>, <see cref="ContextKeys.Percentile"/>
/// Optional: <see cref="ContextKeys.OutputSeconds"/>
/// </remarks>
public class EstimateOutputPhase : ICommandPhase
{
    private readonly IRuntimeEstimator _estimator;

    public EstimateOutputPhase(IRuntimeEstimator estimator)
    {
        _estimator = estimator;
    }

    public Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var history = context.Get<SpecHistory>(ContextKeys.History)!;
        var percentile = context.Get<int>(ContextKeys.Percentile);
        var outputSeconds = context.Get<bool>(ContextKeys.OutputSeconds);
        var console = context.Console;

        if (history.Specs.Count == 0)
        {
            console.WriteLine("No test history found. Run specs first to collect data.");
            console.WriteLine();
            console.ForegroundColor = ConsoleColor.DarkGray;
            console.WriteLine("  draftspec run");
            console.ResetColor();
            return Task.FromResult(0);
        }

        // Calculate estimates
        var estimate = _estimator.Calculate(history, percentile);

        if (estimate.SpecCount == 0)
        {
            console.WriteLine("No timing data available. Run specs to collect duration data.");
            return Task.FromResult(0);
        }

        // Output in machine-readable format if requested
        if (outputSeconds)
        {
            console.WriteLine($"{estimate.TotalEstimateMs / 1000:F1}");
            return Task.FromResult(0);
        }

        // Human-readable output
        ShowEstimate(console, estimate, percentile);

        return Task.FromResult(0);
    }

    private static void ShowEstimate(IConsole console, RuntimeEstimate estimate, int percentile)
    {
        console.WriteLine($"Runtime Estimate (based on {estimate.SampleSize} historical runs):");
        console.WriteLine();

        // Show P50, P95, and Max
        console.ForegroundColor = ConsoleColor.White;
        console.WriteLine($"  P50 (median):      {FormatDuration(estimate.P50Ms)}");
        console.WriteLine($"  P95:               {FormatDuration(estimate.P95Ms)}");
        console.ForegroundColor = ConsoleColor.DarkGray;
        console.WriteLine($"  Max observed:      {FormatDuration(estimate.MaxMs)}");
        console.ResetColor();
        console.WriteLine();

        // Show slowest specs
        if (estimate.SlowestSpecs.Count > 0)
        {
            console.WriteLine($"Slowest specs (P{percentile}):");
            var index = 1;
            foreach (var spec in estimate.SlowestSpecs)
            {
                console.ForegroundColor = ConsoleColor.Yellow;
                console.Write($"  {index}. ");
                console.ResetColor();
                console.WriteLine($"{spec.DisplayName} ({FormatDuration(spec.EstimateMs)})");
                index++;
            }
            console.WriteLine();
        }

        // Recommended CI timeout
        var recommendedTimeout = estimate.P95Ms * 2;
        console.ForegroundColor = ConsoleColor.Green;
        console.WriteLine($"Recommended CI timeout: {FormatDuration(recommendedTimeout)} (2x P95)");
        console.ResetColor();
    }

    internal static string FormatDuration(double ms)
    {
        var span = TimeSpan.FromMilliseconds(ms);

        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}h {span.Minutes:D2}m {span.Seconds:D2}s";

        if (span.TotalMinutes >= 1)
            return $"{(int)span.TotalMinutes}m {span.Seconds:D2}s";

        if (span.TotalSeconds >= 1)
            return $"{span.TotalSeconds:F1}s";

        return $"{ms:F0}ms";
    }
}
