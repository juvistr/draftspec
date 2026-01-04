using System.Collections.Frozen;

namespace DraftSpec.Cli.Parsing;

/// <summary>
/// Registry of all supported command-line options with fast lookup by name.
/// </summary>
public static class OptionRegistry
{
    private static readonly FrozenDictionary<string, OptionDefinition> Options;

    static OptionRegistry()
    {
        var definitions = new OptionDefinition[]
        {
            // Help
            new(["--help", "-h", "help"], OptionHandlers.HandleHelp),

            // Format options
            new(["--format", "-f"], OptionHandlers.HandleFormat),
            new(["--output", "-o"], OptionHandlers.HandleOutput),
            new(["--css-url"], OptionHandlers.HandleCssUrl),

            // Run flags
            new(["--force"], OptionHandlers.HandleForce),
            new(["--parallel", "-p"], OptionHandlers.HandleParallel),
            new(["--no-cache"], OptionHandlers.HandleNoCache),
            new(["--bail", "-b"], OptionHandlers.HandleBail),

            // Filter options
            new(["--filter-tags", "-t"], OptionHandlers.HandleFilterTags),
            new(["--exclude-tags", "-x"], OptionHandlers.HandleExcludeTags),
            new(["--filter-name", "-n"], OptionHandlers.HandleFilterName),
            new(["--exclude-name"], OptionHandlers.HandleExcludeName),
            new(["--context", "-c"], OptionHandlers.HandleContext),
            new(["--exclude-context"], OptionHandlers.HandleExcludeContext),

            // Coverage options
            new(["--coverage"], OptionHandlers.HandleCoverage),
            new(["--coverage-output"], OptionHandlers.HandleCoverageOutput),
            new(["--coverage-format"], OptionHandlers.HandleCoverageFormat),
            new(["--coverage-report-formats"], OptionHandlers.HandleCoverageReportFormats),

            // List command options
            new(["--list-format"], OptionHandlers.HandleListFormat),
            new(["--show-line-numbers"], OptionHandlers.HandleShowLineNumbers),
            new(["--no-line-numbers"], OptionHandlers.HandleNoLineNumbers),
            new(["--focused-only"], OptionHandlers.HandleFocusedOnly),
            new(["--pending-only"], OptionHandlers.HandlePendingOnly),
            new(["--skipped-only"], OptionHandlers.HandleSkippedOnly),

            // Validate command options
            new(["--static"], OptionHandlers.HandleStatic),
            new(["--strict"], OptionHandlers.HandleStrict),
            new(["--quiet", "-q"], OptionHandlers.HandleQuiet),
            new(["--files"], OptionHandlers.HandleFiles),

            // Statistics options
            new(["--no-stats"], OptionHandlers.HandleNoStats),
            new(["--stats-only"], OptionHandlers.HandleStatsOnly),

            // Partition options
            new(["--partition"], OptionHandlers.HandlePartition),
            new(["--partition-index"], OptionHandlers.HandlePartitionIndex),
            new(["--partition-strategy"], OptionHandlers.HandlePartitionStrategy),

            // Watch command options
            new(["--incremental", "-i"], OptionHandlers.HandleIncremental),

            // Impact analysis options
            new(["--affected-by"], OptionHandlers.HandleAffectedBy),
            new(["--dry-run"], OptionHandlers.HandleDryRun),

            // Flaky test options
            new(["--quarantine"], OptionHandlers.HandleQuarantine),
            new(["--no-history"], OptionHandlers.HandleNoHistory),
            new(["--interactive", "-I"], OptionHandlers.HandleInteractive),
            new(["--min-changes"], OptionHandlers.HandleMinChanges),
            new(["--window-size"], OptionHandlers.HandleWindowSize),
            new(["--clear"], OptionHandlers.HandleClear),

            // Estimate command options
            new(["--percentile"], OptionHandlers.HandlePercentile),
            new(["--output-seconds"], OptionHandlers.HandleOutputSeconds),

            // Docs command options
            new(["--docs-format"], OptionHandlers.HandleDocsFormat),
            new(["--docs-context"], OptionHandlers.HandleDocsContext),
            new(["--with-results"], OptionHandlers.HandleWithResults),
            new(["--results-file"], OptionHandlers.HandleResultsFile),

            // Coverage-map command options
            new(["--gaps-only"], OptionHandlers.HandleGapsOnly),
            new(["--specs"], OptionHandlers.HandleSpecs),
            new(["--namespace"], OptionHandlers.HandleNamespace),
            new(["--coverage-map-format"], OptionHandlers.HandleCoverageMapFormat),
        };

        // Build lookup dictionary: each name points to its definition
        var dict = new Dictionary<string, OptionDefinition>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            foreach (var name in definition.Names)
            {
                dict[name] = definition;
            }
        }

        Options = dict.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>
    /// Attempts to get an option definition by name.
    /// </summary>
    /// <param name="name">Option name (e.g., "--format" or "-f").</param>
    /// <param name="definition">The definition if found.</param>
    /// <returns>True if the option was found; otherwise false.</returns>
    public static bool TryGet(string name, out OptionDefinition definition)
    {
        return Options.TryGetValue(name, out definition!);
    }
}
