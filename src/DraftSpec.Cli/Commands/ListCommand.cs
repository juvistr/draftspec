using System.Text.RegularExpressions;
using DraftSpec.Cli.Formatters;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Services;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Lists discovered specs without executing them.
/// Uses static parsing to discover spec structure from CSX files.
/// </summary>
public class ListCommand : ICommand
{
    /// <summary>
    /// Timeout for regex operations to prevent ReDoS attacks.
    /// </summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;
    private readonly ISpecPartitioner _partitioner;

    public ListCommand(IConsole console, IFileSystem fileSystem, ISpecPartitioner partitioner)
    {
        _console = console;
        _fileSystem = fileSystem;
        _partitioner = partitioner;
    }

    public async Task<int> ExecuteAsync(CliOptions options, CancellationToken ct = default)
    {
        // 1. Resolve path
        var projectPath = Path.GetFullPath(options.Path);

        if (!_fileSystem.DirectoryExists(projectPath) && !_fileSystem.FileExists(projectPath))
            throw new ArgumentException($"Path not found: {projectPath}");

        // 2. Find spec files
        var specFiles = GetSpecFiles(projectPath);

        if (specFiles.Count == 0)
        {
            _console.WriteLine("No spec files found.");
            return 0;
        }

        // 3. Apply partitioning if specified
        if (options.Partition.HasValue && options.PartitionIndex.HasValue)
        {
            var partitionResult = await _partitioner.PartitionAsync(
                specFiles,
                options.Partition.Value,
                options.PartitionIndex.Value,
                options.PartitionStrategy,
                projectPath,
                ct);

            _console.ForegroundColor = ConsoleColor.DarkGray;
            _console.WriteLine($"Partition {options.PartitionIndex + 1} of {options.Partition}: {partitionResult.Files.Count} file(s)");
            _console.ResetColor();

            specFiles = partitionResult.Files.ToList();

            if (specFiles.Count == 0)
            {
                _console.WriteLine("No specs in this partition.");
                return 0;
            }
        }

        // 4. Use static parser to discover specs (no execution)
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

        // 5. Apply filters
        var filteredSpecs = ApplyFilters(allSpecs, options);

        // 6. Format output based on ListFormat
        var formatter = CreateFormatter(options);
        var output = formatter.Format(filteredSpecs, allErrors);

        // 7. Write to file or stdout
        if (!string.IsNullOrEmpty(options.OutputFile))
        {
            await _fileSystem.WriteAllTextAsync(options.OutputFile, output, ct);
            _console.WriteLine($"Wrote {filteredSpecs.Count} specs to {options.OutputFile}");
        }
        else
        {
            _console.WriteLine(output);
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
                var regex = new Regex(options.FilterName, RegexOptions.IgnoreCase, RegexTimeout);
                filtered = filtered.Where(s => regex.IsMatch(s.DisplayName));
            }
            catch (RegexParseException)
            {
                // Fall back to simple substring match
                var pattern = options.FilterName;
                filtered = filtered.Where(s =>
                    s.DisplayName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            }
            catch (RegexMatchTimeoutException)
            {
                // Pattern caused timeout - fall back to substring match
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
        return options.ListFormat switch
        {
            ListFormat.Tree => new TreeListFormatter(options.ShowLineNumbers),
            ListFormat.Flat => new FlatListFormatter(options.ShowLineNumbers),
            ListFormat.Json => new JsonListFormatter(),
            _ => throw new ArgumentOutOfRangeException(nameof(options.ListFormat), options.ListFormat, "Unknown list format")
        };
    }
}
