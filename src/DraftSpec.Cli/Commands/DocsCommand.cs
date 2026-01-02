using System.Text.Json;
using System.Text.RegularExpressions;
using DraftSpec.Cli.Formatters;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Services;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Generates living documentation from spec structure.
/// Uses static parsing to discover spec structure from CSX files.
/// </summary>
public class DocsCommand : ICommand<DocsOptions>
{
    /// <summary>
    /// Timeout for regex operations to prevent ReDoS attacks.
    /// </summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public DocsCommand(IConsole console, IFileSystem fileSystem)
    {
        _console = console;
        _fileSystem = fileSystem;
    }

    public async Task<int> ExecuteAsync(DocsOptions options, CancellationToken ct = default)
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

        // 3. Use static parser to discover specs (no execution)
        var parser = new StaticSpecParser(projectPath, useCache: true);
        var allSpecs = new List<DiscoveredSpec>();

        foreach (var specFile in specFiles)
        {
            var result = await parser.ParseFileAsync(specFile, ct);
            var relativePath = Path.GetRelativePath(projectPath, specFile);

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
        }

        // 4. Apply filters
        var filteredSpecs = ApplyFilters(allSpecs, options);

        // 5. Load results if --with-results
        IReadOnlyDictionary<string, string>? results = null;
        if (options.WithResults)
        {
            results = await LoadResultsAsync(options.ResultsFile, ct);
        }

        // 6. Format output
        var formatter = CreateFormatter(options.Format);
        var metadata = new DocsMetadata(
            DateTime.UtcNow,
            Path.GetRelativePath(Environment.CurrentDirectory, projectPath),
            results);
        var output = formatter.Format(filteredSpecs, metadata);

        // 7. Write output
        _console.WriteLine(output);

        return 0;
    }

    private List<string> GetSpecFiles(string path)
    {
        if (_fileSystem.FileExists(path) && path.EndsWith(".spec.csx", StringComparison.OrdinalIgnoreCase))
        {
            return [path];
        }

        if (_fileSystem.DirectoryExists(path))
        {
            return _fileSystem.EnumerateFiles(path, "*.spec.csx", SearchOption.AllDirectories).ToList();
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
        DocsOptions options)
    {
        var filtered = specs.AsEnumerable();

        // Context filter
        if (!string.IsNullOrEmpty(options.Context))
        {
            try
            {
                var regex = new Regex(options.Context, RegexOptions.IgnoreCase, RegexTimeout);
                filtered = filtered.Where(s =>
                    s.ContextPath.Any(c => regex.IsMatch(c)) ||
                    regex.IsMatch(s.DisplayName));
            }
            catch (RegexParseException)
            {
                var pattern = options.Context;
                filtered = filtered.Where(s =>
                    s.ContextPath.Any(c => c.Contains(pattern, StringComparison.OrdinalIgnoreCase)) ||
                    s.DisplayName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            }
            catch (RegexMatchTimeoutException)
            {
                var pattern = options.Context;
                filtered = filtered.Where(s =>
                    s.ContextPath.Any(c => c.Contains(pattern, StringComparison.OrdinalIgnoreCase)) ||
                    s.DisplayName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Pattern filter on name
        if (!string.IsNullOrEmpty(options.Filter.FilterName))
        {
            try
            {
                var regex = new Regex(options.Filter.FilterName, RegexOptions.IgnoreCase, RegexTimeout);
                filtered = filtered.Where(s => regex.IsMatch(s.DisplayName));
            }
            catch (RegexParseException)
            {
                var pattern = options.Filter.FilterName;
                filtered = filtered.Where(s =>
                    s.DisplayName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            }
            catch (RegexMatchTimeoutException)
            {
                var pattern = options.Filter.FilterName;
                filtered = filtered.Where(s =>
                    s.DisplayName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            }
        }

        return filtered.ToList();
    }

    private async Task<IReadOnlyDictionary<string, string>?> LoadResultsAsync(string? resultsFile, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(resultsFile))
        {
            _console.WriteError("--with-results requires --results-file to specify the JSON results file.");
            return null;
        }

        if (!_fileSystem.FileExists(resultsFile))
        {
            _console.WriteError($"Results file not found: {resultsFile}");
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(resultsFile, ct);
            var report = SpecReport.FromJson(json);

            // Flatten results to dictionary of ID -> status
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            FlattenResults(report.Contexts, [], results);
            return results;
        }
        catch (Exception ex)
        {
            _console.WriteError($"Failed to parse results file: {ex.Message}");
            return null;
        }
    }

    private static void FlattenResults(
        IList<SpecContextReport> contexts,
        List<string> path,
        Dictionary<string, string> results)
    {
        foreach (var context in contexts)
        {
            path.Add(context.Description);

            foreach (var spec in context.Specs)
            {
                // Generate ID matching the format used in discovery
                var contextPath = string.Join("/", path);
                var id = $":{contextPath}/{spec.Description}";
                results[id] = spec.Status;
            }

            FlattenResults(context.Contexts, path, results);
            path.RemoveAt(path.Count - 1);
        }
    }

    private static IDocsFormatter CreateFormatter(DocsFormat format)
    {
        return format switch
        {
            DocsFormat.Markdown => new MarkdownDocsFormatter(),
            DocsFormat.Html => new HtmlDocsFormatter(),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown docs format")
        };
    }
}
