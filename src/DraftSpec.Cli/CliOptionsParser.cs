using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Cli;

/// <summary>
/// Parses command-line arguments into CliOptions.
/// Extracted for testability.
/// </summary>
public static class CliOptionsParser
{
    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();
        var positional = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg is "--help" or "-h" or "help")
            {
                options.ShowHelp = true;
            }
            else if (arg is "--format" or "-f")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--format requires a value (console, json, markdown, html, junit)";
                    return options;
                }

                var formatValue = args[++i];
                if (!formatValue.TryParseOutputFormat(out var format))
                {
                    options.Error = $"Unknown format: '{formatValue}'. Valid options: console, json, markdown, html, junit";
                    return options;
                }
                options.Format = format;
                options.ExplicitlySet.Add(nameof(CliOptions.Format));
            }
            else if (arg is "--output" or "-o")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--output requires a file path";
                    return options;
                }

                options.OutputFile = args[++i];
                options.ExplicitlySet.Add(nameof(CliOptions.OutputFile));
            }
            else if (arg == "--css-url")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--css-url requires a URL";
                    return options;
                }

                options.CssUrl = args[++i];
            }
            else if (arg == "--force")
            {
                options.Force = true;
            }
            else if (arg is "--parallel" or "-p")
            {
                options.Parallel = true;
                options.ExplicitlySet.Add(nameof(CliOptions.Parallel));
            }
            else if (arg == "--no-cache")
            {
                options.NoCache = true;
                options.ExplicitlySet.Add(nameof(CliOptions.NoCache));
            }
            else if (arg is "--bail" or "-b")
            {
                options.Bail = true;
                options.ExplicitlySet.Add(nameof(CliOptions.Bail));
            }
            else if (arg is "--filter-tags" or "-t")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--filter-tags requires a value (comma-separated tags)";
                    return options;
                }

                options.FilterTags = args[++i];
                options.ExplicitlySet.Add(nameof(CliOptions.FilterTags));
            }
            else if (arg is "--exclude-tags" or "-x")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--exclude-tags requires a value (comma-separated tags)";
                    return options;
                }

                options.ExcludeTags = args[++i];
                options.ExplicitlySet.Add(nameof(CliOptions.ExcludeTags));
            }
            else if (arg is "--filter-name" or "-n")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--filter-name requires a value (regex pattern)";
                    return options;
                }

                options.FilterName = args[++i];
                options.ExplicitlySet.Add(nameof(CliOptions.FilterName));
            }
            else if (arg == "--exclude-name")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--exclude-name requires a value (regex pattern)";
                    return options;
                }

                options.ExcludeName = args[++i];
                options.ExplicitlySet.Add(nameof(CliOptions.ExcludeName));
            }
            else if (arg is "--context" or "-c")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--context requires a value (context pattern with / separator)";
                    return options;
                }

                options.FilterContext ??= [];
                options.FilterContext.Add(args[++i]);
                options.ExplicitlySet.Add(nameof(CliOptions.FilterContext));
            }
            else if (arg == "--exclude-context")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--exclude-context requires a value (context pattern with / separator)";
                    return options;
                }

                options.ExcludeContext ??= [];
                options.ExcludeContext.Add(args[++i]);
                options.ExplicitlySet.Add(nameof(CliOptions.ExcludeContext));
            }
            else if (arg == "--coverage")
            {
                options.Coverage = true;
                options.ExplicitlySet.Add(nameof(CliOptions.Coverage));
            }
            else if (arg == "--coverage-output")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--coverage-output requires a directory path";
                    return options;
                }

                options.CoverageOutput = args[++i];
                options.ExplicitlySet.Add(nameof(CliOptions.CoverageOutput));
            }
            else if (arg == "--coverage-format")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--coverage-format requires a value (cobertura, xml, coverage)";
                    return options;
                }

                var coverageFormatValue = args[++i];
                if (!coverageFormatValue.TryParseCoverageFormat(out var coverageFormat))
                {
                    options.Error = $"Unknown coverage format: '{coverageFormatValue}'. Valid options: cobertura, xml, coverage";
                    return options;
                }
                options.CoverageFormat = coverageFormat;
                options.ExplicitlySet.Add(nameof(CliOptions.CoverageFormat));
            }
            else if (arg == "--coverage-report-formats")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--coverage-report-formats requires a value (comma-separated: html, json)";
                    return options;
                }

                options.CoverageReportFormats = args[++i].ToLowerInvariant();
                options.ExplicitlySet.Add(nameof(CliOptions.CoverageReportFormats));
            }
            // List command options
            else if (arg == "--list-format")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--list-format requires a value (tree, flat, json)";
                    return options;
                }

                var listFormatValue = args[++i];
                if (!listFormatValue.TryParseListFormat(out var listFormat))
                {
                    options.Error = $"Unknown list format: '{listFormatValue}'. Valid options: tree, flat, json";
                    return options;
                }
                options.ListFormat = listFormat;
                options.ExplicitlySet.Add(nameof(CliOptions.ListFormat));
            }
            else if (arg == "--show-line-numbers")
            {
                options.ShowLineNumbers = true;
                options.ExplicitlySet.Add(nameof(CliOptions.ShowLineNumbers));
            }
            else if (arg == "--no-line-numbers")
            {
                options.ShowLineNumbers = false;
                options.ExplicitlySet.Add(nameof(CliOptions.ShowLineNumbers));
            }
            else if (arg == "--focused-only")
            {
                options.FocusedOnly = true;
                options.ExplicitlySet.Add(nameof(CliOptions.FocusedOnly));
            }
            else if (arg == "--pending-only")
            {
                options.PendingOnly = true;
                options.ExplicitlySet.Add(nameof(CliOptions.PendingOnly));
            }
            else if (arg == "--skipped-only")
            {
                options.SkippedOnly = true;
                options.ExplicitlySet.Add(nameof(CliOptions.SkippedOnly));
            }
            // Validate command options
            else if (arg == "--static")
            {
                options.Static = true;
                options.ExplicitlySet.Add(nameof(CliOptions.Static));
            }
            else if (arg == "--strict")
            {
                options.Strict = true;
                options.ExplicitlySet.Add(nameof(CliOptions.Strict));
            }
            else if (arg is "--quiet" or "-q")
            {
                options.Quiet = true;
                options.ExplicitlySet.Add(nameof(CliOptions.Quiet));
            }
            else if (arg == "--files")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--files requires a value (comma-separated file paths)";
                    return options;
                }

                var filesArg = args[++i];
                options.Files = filesArg.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim())
                    .ToList();
                options.ExplicitlySet.Add(nameof(CliOptions.Files));
            }
            // Run command statistics options
            else if (arg == "--no-stats")
            {
                options.NoStats = true;
                options.ExplicitlySet.Add(nameof(CliOptions.NoStats));
            }
            else if (arg == "--stats-only")
            {
                options.StatsOnly = true;
                options.ExplicitlySet.Add(nameof(CliOptions.StatsOnly));
            }
            // Partitioning options for CI parallelism
            else if (arg == "--partition")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--partition requires a value (total number of partitions)";
                    return options;
                }

                if (!int.TryParse(args[++i], out var partition) || partition < 1)
                {
                    options.Error = "--partition must be a positive integer";
                    return options;
                }

                options.Partition = partition;
                options.ExplicitlySet.Add(nameof(CliOptions.Partition));
            }
            else if (arg == "--partition-index")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--partition-index requires a value (0-based index)";
                    return options;
                }

                if (!int.TryParse(args[++i], out var index) || index < 0)
                {
                    options.Error = "--partition-index must be a non-negative integer";
                    return options;
                }

                options.PartitionIndex = index;
                options.ExplicitlySet.Add(nameof(CliOptions.PartitionIndex));
            }
            else if (arg == "--partition-strategy")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--partition-strategy requires a value (file, spec-count)";
                    return options;
                }

                var strategyValue = args[++i];
                if (!strategyValue.TryParsePartitionStrategy(out var strategy))
                {
                    options.Error = $"Unknown partition strategy: '{strategyValue}'. Valid options: file, spec-count";
                    return options;
                }

                options.PartitionStrategy = strategy;
                options.ExplicitlySet.Add(nameof(CliOptions.PartitionStrategy));
            }
            // Watch command options
            else if (arg is "--incremental" or "-i")
            {
                options.Incremental = true;
                options.ExplicitlySet.Add(nameof(CliOptions.Incremental));
            }
            // Test impact analysis options
            else if (arg == "--affected-by")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--affected-by requires a value (commit ref, 'staged', or file path)";
                    return options;
                }

                options.AffectedBy = args[++i];
                options.ExplicitlySet.Add(nameof(CliOptions.AffectedBy));
            }
            else if (arg == "--dry-run")
            {
                options.DryRun = true;
                options.ExplicitlySet.Add(nameof(CliOptions.DryRun));
            }
            // Flaky test detection options
            else if (arg == "--quarantine")
            {
                options.Quarantine = true;
                options.ExplicitlySet.Add(nameof(CliOptions.Quarantine));
            }
            else if (arg == "--no-history")
            {
                options.NoHistory = true;
                options.ExplicitlySet.Add(nameof(CliOptions.NoHistory));
            }
            // Flaky command options
            else if (arg == "--min-changes")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--min-changes requires a value (minimum status changes)";
                    return options;
                }

                if (!int.TryParse(args[++i], out var minChanges) || minChanges < 1)
                {
                    options.Error = "--min-changes must be a positive integer";
                    return options;
                }

                options.MinStatusChanges = minChanges;
                options.ExplicitlySet.Add(nameof(CliOptions.MinStatusChanges));
            }
            else if (arg == "--window-size")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--window-size requires a value (number of runs to analyze)";
                    return options;
                }

                if (!int.TryParse(args[++i], out var windowSize) || windowSize < 2)
                {
                    options.Error = "--window-size must be at least 2";
                    return options;
                }

                options.WindowSize = windowSize;
                options.ExplicitlySet.Add(nameof(CliOptions.WindowSize));
            }
            else if (arg == "--clear")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--clear requires a spec ID to clear";
                    return options;
                }

                options.Clear = args[++i];
                options.ExplicitlySet.Add(nameof(CliOptions.Clear));
            }
            // Estimate command options
            else if (arg == "--percentile")
            {
                if (i + 1 >= args.Length)
                {
                    options.Error = "--percentile requires a value (1-99)";
                    return options;
                }

                if (!int.TryParse(args[++i], out var percentile) || percentile < 1 || percentile > 99)
                {
                    options.Error = "--percentile must be between 1 and 99";
                    return options;
                }

                options.Percentile = percentile;
                options.ExplicitlySet.Add(nameof(CliOptions.Percentile));
            }
            else if (arg == "--output-seconds")
            {
                options.OutputSeconds = true;
                options.ExplicitlySet.Add(nameof(CliOptions.OutputSeconds));
            }
            else if (!arg.StartsWith('-'))
            {
                positional.Add(arg);
            }
            else
            {
                options.Error = $"Unknown option: {arg}";
                return options;
            }
        }

        if (positional.Count > 0)
            options.Command = positional[0].ToLowerInvariant();
        if (positional.Count > 1)
        {
            // For 'new' command, second arg is the spec name
            if (options.Command == "new")
                options.SpecName = positional[1];
            else
                options.Path = ParsePathWithLineNumbers(positional[1], options);
        }

        if (positional.Count > 2 && options.Command == "new")
            options.Path = positional[2];

        // Cross-validate partition options: both must be specified together
        if (options.Partition.HasValue != options.PartitionIndex.HasValue)
        {
            options.Error = "--partition and --partition-index must be used together";
            return options;
        }

        // Validate partition-index is within valid range
        if (options.Partition.HasValue && options.PartitionIndex >= options.Partition)
        {
            options.Error = $"--partition-index ({options.PartitionIndex}) must be less than --partition ({options.Partition})";
            return options;
        }

        return options;
    }

    /// <summary>
    /// Parses a path that may include line numbers (e.g., "file.spec.csx:15,23").
    /// Returns the file path and populates LineFilters if line numbers are present.
    /// </summary>
    internal static string ParsePathWithLineNumbers(string pathArg, CliOptions options)
    {
        // Check for file:line syntax
        // Must handle Windows paths (C:\...) by checking if colon is followed by digits
        var colonIndex = pathArg.LastIndexOf(':');

        // No colon, or colon is at position 1 (Windows drive letter like C:)
        if (colonIndex <= 0 || (colonIndex == 1 && char.IsLetter(pathArg[0])))
            return pathArg;

        // Check if what follows the colon looks like line numbers (digits and commas)
        var afterColon = pathArg[(colonIndex + 1)..];
        if (string.IsNullOrEmpty(afterColon) || !afterColon.All(c => char.IsDigit(c) || c == ','))
            return pathArg;

        // Parse line numbers
        var filePath = pathArg[..colonIndex];
        var lineNumbers = afterColon
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .Where(n => n > 0)
            .ToArray();

        if (lineNumbers.Length > 0)
        {
            options.LineFilters ??= [];
            options.LineFilters.Add(new LineFilter(filePath, lineNumbers));
        }

        return filePath;
    }
}
