using System.Text.RegularExpressions;
using DraftSpec.Cli.Formatters;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Lists discovered specs without executing them.
/// Uses static parsing to discover spec structure from CSX files.
/// </summary>
public static class ListCommand
{
    public static async Task<int> ExecuteAsync(CliOptions options, CancellationToken ct = default)
    {
        // 1. Resolve path
        var projectPath = Path.GetFullPath(options.Path);

        if (!Directory.Exists(projectPath) && !File.Exists(projectPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Path not found: {projectPath}");
            Console.ResetColor();
            return 1;
        }

        // 2. Find spec files
        var specFiles = GetSpecFiles(projectPath);

        if (specFiles.Count == 0)
        {
            Console.WriteLine("No spec files found.");
            return 0;
        }

        // 3. Use static parser to discover specs (no execution)
        var parser = new StaticSpecParser(projectPath);
        var allSpecs = new List<DiscoveredSpec>();
        var allErrors = new List<DiscoveryError>();

        foreach (var specFile in specFiles)
        {
            var result = await parser.ParseFileAsync(specFile, ct);
            var relativePath = Path.GetRelativePath(projectPath, specFile);

            // Convert StaticSpec to DiscoveredSpec for formatter compatibility
            foreach (var staticSpec in result.Specs)
            {
                var id = GenerateId(relativePath, staticSpec.ContextPath, staticSpec.Description);
                var displayName = GenerateDisplayName(staticSpec.ContextPath, staticSpec.Description);

                allSpecs.Add(new DiscoveredSpec
                {
                    Id = id,
                    Description = staticSpec.Description,
                    DisplayName = displayName,
                    ContextPath = staticSpec.ContextPath,
                    SourceFile = specFile,
                    RelativeSourceFile = relativePath,
                    LineNumber = staticSpec.LineNumber,
                    IsPending = staticSpec.IsPending,
                    IsSkipped = staticSpec.Type == StaticSpecType.Skipped,
                    IsFocused = staticSpec.Type == StaticSpecType.Focused,
                    Tags = []
                });
            }

            // Report warnings as errors for files that couldn't be fully parsed
            if (!result.IsComplete)
            {
                foreach (var warning in result.Warnings)
                {
                    allErrors.Add(new DiscoveryError
                    {
                        SourceFile = specFile,
                        RelativeSourceFile = relativePath,
                        Message = warning
                    });
                }
            }
        }

        // 4. Apply filters
        var filteredSpecs = ApplyFilters(allSpecs, options);

        // 5. Format output based on ListFormat
        var formatter = CreateFormatter(options);
        var output = formatter.Format(filteredSpecs, allErrors);

        // 6. Write to file or stdout
        if (!string.IsNullOrEmpty(options.OutputFile))
        {
            await File.WriteAllTextAsync(options.OutputFile, output, ct);
            Console.WriteLine($"Wrote {filteredSpecs.Count} specs to {options.OutputFile}");
        }
        else
        {
            Console.WriteLine(output);
        }

        return 0;
    }

    private static List<string> GetSpecFiles(string path)
    {
        if (File.Exists(path) && path.EndsWith(".spec.csx", StringComparison.OrdinalIgnoreCase))
        {
            return [path];
        }

        if (Directory.Exists(path))
        {
            return Directory.EnumerateFiles(path, "*.spec.csx", SearchOption.AllDirectories).ToList();
        }

        return [];
    }

    private static string GenerateId(string relativePath, IReadOnlyList<string> contextPath, string description)
    {
        var path = string.Join("/", contextPath);
        return $"{relativePath}:{path}/{description}";
    }

    private static string GenerateDisplayName(IReadOnlyList<string> contextPath, string description)
    {
        if (contextPath.Count == 0)
            return description;

        return string.Join(" > ", contextPath) + " > " + description;
    }

    private static IReadOnlyList<DiscoveredSpec> ApplyFilters(
        IReadOnlyList<DiscoveredSpec> specs,
        CliOptions options)
    {
        var filtered = specs.AsEnumerable();

        // Status filters (OR'd together if multiple specified)
        var hasStatusFilter = options.FocusedOnly || options.PendingOnly || options.SkippedOnly;
        if (hasStatusFilter)
        {
            filtered = filtered.Where(s =>
                (options.FocusedOnly && s.IsFocused) ||
                (options.PendingOnly && s.IsPending) ||
                (options.SkippedOnly && s.IsSkipped));
        }

        // Pattern filter on name (AND'd with status filters)
        if (!string.IsNullOrEmpty(options.FilterName))
        {
            try
            {
                var regex = new Regex(options.FilterName, RegexOptions.IgnoreCase);
                filtered = filtered.Where(s => regex.IsMatch(s.DisplayName));
            }
            catch (RegexParseException)
            {
                // Fall back to simple substring match
                var pattern = options.FilterName;
                filtered = filtered.Where(s =>
                    s.DisplayName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Tag filter (AND'd with other filters)
        if (!string.IsNullOrEmpty(options.FilterTags))
        {
            var tags = options.FilterTags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            filtered = filtered.Where(s => s.Tags.Any(t => tags.Contains(t)));
        }

        return filtered.ToList();
    }

    private static IListFormatter CreateFormatter(CliOptions options)
    {
        return options.ListFormat.ToLowerInvariant() switch
        {
            "tree" => new TreeListFormatter(options.ShowLineNumbers),
            "flat" => new FlatListFormatter(options.ShowLineNumbers),
            "json" => new JsonListFormatter(),
            _ => throw new ArgumentException($"Unknown list format: {options.ListFormat}. Valid options: tree, flat, json")
        };
    }
}
