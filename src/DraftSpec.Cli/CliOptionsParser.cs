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
                    options.Error = "--format requires a value (console, json, markdown, html)";
                    return options;
                }

                options.Format = args[++i].ToLowerInvariant();
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

                options.CoverageFormat = args[++i].ToLowerInvariant();
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

                options.ListFormat = args[++i].ToLowerInvariant();
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

        return options;
    }

    /// <summary>
    /// Parses a path that may include line numbers (e.g., "file.spec.csx:15,23").
    /// Returns the file path and populates LineFilters if line numbers are present.
    /// </summary>
    private static string ParsePathWithLineNumbers(string pathArg, CliOptions options)
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
