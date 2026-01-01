namespace DraftSpec.Cli;

/// <summary>
/// Writes CLI usage/help information using IConsole.
/// </summary>
public class UsageWriter : IUsageWriter
{
    private readonly IConsole _console;

    public UsageWriter(IConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <summary>
    /// Shows usage information with optional error message.
    /// </summary>
    public int Show(string? errorMessage = null)
    {
        if (errorMessage != null)
        {
            _console.WriteError($"Error: {errorMessage}");
            _console.WriteLine();
        }

        _console.WriteLine("DraftSpec - RSpec-style testing for .NET");
        _console.WriteLine();
        _console.WriteLine("Usage:");
        _console.WriteLine("  draftspec run <path> [options]   Run specs once and exit");
        _console.WriteLine("  draftspec watch <path> [options] Watch for changes and re-run");
        _console.WriteLine("  draftspec list <path> [options]  List specs without running them");
        _console.WriteLine("  draftspec flaky [path] [options] Show flaky test detection report");
        _console.WriteLine("  draftspec estimate [path]        Estimate runtime based on history");
        _console.WriteLine("  draftspec cache <subcommand>     Manage cached data (stats, clear)");
        _console.WriteLine("  draftspec init [path]            Initialize spec infrastructure");
        _console.WriteLine("  draftspec new <name> [path]      Create a new spec file");
        _console.WriteLine();
        _console.WriteLine("Options:");
        _console.WriteLine("  --format, -f <format>  Output format: console, json, markdown, html");
        _console.WriteLine("  --output, -o <file>    Write output to file instead of stdout");
        _console.WriteLine("  --parallel, -p         Run spec files in parallel");
        _console.WriteLine("  --css-url <url>        Custom CSS URL for HTML output");
        _console.WriteLine("  --force                Overwrite existing files (for init)");
        _console.WriteLine("  --no-cache             Disable dotnet-script caching");
        _console.WriteLine();
        _console.WriteLine("Coverage Options:");
        _console.WriteLine("  --coverage             Enable code coverage collection");
        _console.WriteLine("  --coverage-output <dir>  Coverage output directory (default: ./coverage)");
        _console.WriteLine("  --coverage-format <fmt>  Coverage format: cobertura, xml (default: cobertura)");
        _console.WriteLine("  --coverage-report-formats <formats>  Additional report formats: html, json");
        _console.WriteLine();
        _console.WriteLine("List Options:");
        _console.WriteLine("  --list-format <format>  Output format: tree (default), flat, json");
        _console.WriteLine("  --show-line-numbers     Show line numbers (default: true)");
        _console.WriteLine("  --no-line-numbers       Hide line numbers");
        _console.WriteLine("  --focused-only          Show only focused specs (fit)");
        _console.WriteLine("  --pending-only          Show only pending specs");
        _console.WriteLine("  --skipped-only          Show only skipped specs (xit)");
        _console.WriteLine();
        _console.WriteLine("Watch Options:");
        _console.WriteLine("  --incremental, -i       Only re-run changed specs (not entire files)");
        _console.WriteLine();
        _console.WriteLine("Flaky Test Options (run command):");
        _console.WriteLine("  --quarantine            Skip known flaky tests during execution");
        _console.WriteLine("  --no-history            Disable recording results to history");
        _console.WriteLine();
        _console.WriteLine("Flaky Command Options:");
        _console.WriteLine("  --min-changes <n>       Minimum status changes to be flaky (default: 2)");
        _console.WriteLine("  --window-size <n>       Number of recent runs to analyze (default: 10)");
        _console.WriteLine("  --clear <spec-id>       Clear a specific spec from history");
        _console.WriteLine();
        _console.WriteLine("Estimate Command Options:");
        _console.WriteLine("  --percentile <n>        Percentile for estimation (1-99, default: 50)");
        _console.WriteLine("  --output-seconds        Output estimate in seconds (for scripting)");
        _console.WriteLine();
        _console.WriteLine("Cache Command:");
        _console.WriteLine("  draftspec cache stats [path]   Show cache statistics");
        _console.WriteLine("  draftspec cache clear [path]   Clear all cached data");
        _console.WriteLine();
        _console.WriteLine("Path can be:");
        _console.WriteLine("  - A directory (runs all *.spec.csx files recursively)");
        _console.WriteLine("  - A single .spec.csx file");
        _console.WriteLine("  - Omitted (defaults to current directory)");
        _console.WriteLine();
        _console.WriteLine("Examples:");
        _console.WriteLine("  draftspec init");
        _console.WriteLine("  draftspec new Calculator");
        _console.WriteLine("  draftspec run ./specs");
        _console.WriteLine("  draftspec run ./specs --format markdown -o report.md");
        _console.WriteLine("  draftspec run ./specs --coverage");
        _console.WriteLine("  draftspec watch .");
        _console.WriteLine("  draftspec watch . --incremental");
        _console.WriteLine("  draftspec list . --list-format json -o specs.json");
        _console.WriteLine("  draftspec list . --focused-only");
        _console.WriteLine("  draftspec flaky                     # Show flaky test report");
        _console.WriteLine("  draftspec run . --quarantine        # Skip known flaky tests");
        _console.WriteLine("  draftspec estimate                  # Show runtime estimate");
        _console.WriteLine("  draftspec estimate --percentile 95  # P95 estimate for CI timeout");
        _console.WriteLine("  draftspec cache stats               # Show cache statistics");
        _console.WriteLine("  draftspec cache clear               # Clear all cached data");

        return errorMessage != null ? 1 : 0;
    }
}
