using DraftSpec.Cli.History;
using DraftSpec.Cli.Options;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Estimates runtime based on historical execution data.
/// </summary>
public class EstimateCommand : ICommand<EstimateOptions>
{
    private readonly IRuntimeEstimator _estimator;
    private readonly ISpecHistoryService _historyService;
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public EstimateCommand(
        IRuntimeEstimator estimator,
        ISpecHistoryService historyService,
        IConsole console,
        IFileSystem fileSystem)
    {
        _estimator = estimator;
        _historyService = historyService;
        _console = console;
        _fileSystem = fileSystem;
    }

    public async Task<int> ExecuteAsync(EstimateOptions options, CancellationToken ct = default)
    {
        var projectPath = Path.GetFullPath(options.Path);

        if (!_fileSystem.DirectoryExists(projectPath))
            throw new ArgumentException($"Directory not found: {projectPath}");

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

        // Calculate estimates
        var estimate = _estimator.Calculate(history, options.Percentile);

        if (estimate.SpecCount == 0)
        {
            _console.WriteLine("No timing data available. Run specs to collect duration data.");
            return 0;
        }

        // Output in machine-readable format if requested
        if (options.OutputSeconds)
        {
            _console.WriteLine($"{estimate.TotalEstimateMs / 1000:F1}");
            return 0;
        }

        // Human-readable output
        ShowEstimate(estimate, options);

        return 0;
    }

    private void ShowEstimate(RuntimeEstimate estimate, EstimateOptions options)
    {
        _console.WriteLine($"Runtime Estimate (based on {estimate.SampleSize} historical runs):");
        _console.WriteLine();

        // Show P50, P95, and Max
        _console.ForegroundColor = ConsoleColor.White;
        _console.WriteLine($"  P50 (median):      {FormatDuration(estimate.P50Ms)}");
        _console.WriteLine($"  P95:               {FormatDuration(estimate.P95Ms)}");
        _console.ForegroundColor = ConsoleColor.DarkGray;
        _console.WriteLine($"  Max observed:      {FormatDuration(estimate.MaxMs)}");
        _console.ResetColor();
        _console.WriteLine();

        // Show slowest specs
        if (estimate.SlowestSpecs.Count > 0)
        {
            _console.WriteLine($"Slowest specs (P{options.Percentile}):");
            var index = 1;
            foreach (var spec in estimate.SlowestSpecs)
            {
                _console.ForegroundColor = ConsoleColor.Yellow;
                _console.Write($"  {index}. ");
                _console.ResetColor();
                _console.WriteLine($"{spec.DisplayName} ({FormatDuration(spec.EstimateMs)})");
                index++;
            }
            _console.WriteLine();
        }

        // Recommended CI timeout
        var recommendedTimeout = estimate.P95Ms * 2;
        _console.ForegroundColor = ConsoleColor.Green;
        _console.WriteLine($"Recommended CI timeout: {FormatDuration(recommendedTimeout)} (2x P95)");
        _console.ResetColor();
    }

    private static string FormatDuration(double ms)
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
