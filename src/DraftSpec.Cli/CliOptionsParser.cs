using DraftSpec.Cli.Options;
using DraftSpec.Cli.Parsing;

namespace DraftSpec.Cli;

/// <summary>
/// Parses command-line arguments into CliOptions.
/// Uses dictionary-based dispatch for option handling.
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

            // Try dictionary lookup first
            if (OptionRegistry.TryGet(arg, out var definition))
            {
                var result = definition.Handler(args, i, options);
                if (result.Error != null)
                {
                    options.Error = result.Error;
                    return options;
                }
                i += result.ConsumedArgs - 1;
            }
            else if (!arg.StartsWith('-'))
            {
                // Positional argument
                positional.Add(arg);
            }
            else
            {
                // Unknown option
                options.Error = $"Unknown option: {arg}";
                return options;
            }
        }

        // Process positional arguments
        if (positional.Count > 0)
            options.Command = positional[0].ToLowerInvariant();
        if (positional.Count > 1)
        {
            // For 'new' command, second arg is the spec name
            if (options.Command is "new")
                options.SpecName = positional[1];
            // For 'cache' command, second arg is the subcommand (stats, clear)
            else if (options.Command is "cache")
                options.CacheSubcommand = positional[1].ToLowerInvariant();
            // For 'coverage-map' command, second arg is the source path
            else if (options.Command is "coverage-map")
                options.CoverageMapSourcePath = positional[1];
            else
                options.Path = ParsePathWithLineNumbers(positional[1], options);
        }

        if (positional.Count > 2 && options.Command is "new")
            options.Path = positional[2];
        if (positional.Count > 2 && options.Command is "cache")
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
