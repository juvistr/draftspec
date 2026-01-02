using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Parses CSX spec files using static syntax analysis (no execution).
/// </summary>
/// <remarks>
/// This parser discovers spec structure by analyzing Roslyn syntax trees,
/// allowing discovery even when files have compilation errors.
/// </remarks>
public sealed partial class StaticSpecParser
{
    private readonly string _baseDirectory;
    private readonly StaticParseResultCache? _cache;

    /// <summary>
    /// Regex to match #load directives for file includes.
    /// </summary>
    [GeneratedRegex(@"^\s*#load\s+""(?<path>[^""]+)""\s*$", RegexOptions.Multiline | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture)]
    private static partial Regex LoadDirectiveRegex();

    /// <summary>
    /// Regex to match #r directives (assembly references).
    /// </summary>
    [GeneratedRegex(@"^\s*#r\s+""[^""]+"".*$", RegexOptions.Multiline | RegexOptions.NonBacktracking)]
    private static partial Regex AssemblyReferenceRegex();

    /// <summary>
    /// Creates a new static spec parser.
    /// </summary>
    /// <param name="baseDirectory">Base directory for resolving relative paths.</param>
    /// <param name="useCache">Whether to use disk-based caching for parse results.</param>
    public StaticSpecParser(string baseDirectory, bool useCache = false)
    {
        _baseDirectory = baseDirectory;
        _cache = useCache ? new StaticParseResultCache(baseDirectory) : null;
    }

    /// <summary>
    /// Parses a CSX file and returns discovered specs.
    /// </summary>
    /// <param name="csxFilePath">Path to the CSX spec file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing discovered specs and any warnings.</returns>
    public async Task<StaticParseResult> ParseFileAsync(
        string csxFilePath,
        CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.GetFullPath(csxFilePath, _baseDirectory);

        if (!File.Exists(absolutePath))
        {
            return new StaticParseResult
            {
                Specs = [],
                Warnings = [$"File not found: {absolutePath}"],
                IsComplete = false
            };
        }

        try
        {
            // Resolve all source files (main + #load dependencies)
            var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sourceFiles = new List<string>();
            var codeBuilder = new StringBuilder();
            await ProcessFileAsync(absolutePath, processedFiles, sourceFiles, codeBuilder, cancellationToken).ConfigureAwait(false);
            var combinedSource = codeBuilder.ToString();

            // Try cache first
            if (_cache != null)
            {
                var (hit, cached) = await _cache.TryGetCachedAsync(absolutePath, sourceFiles, cancellationToken).ConfigureAwait(false);
                if (hit && cached != null)
                {
                    return cached;
                }
            }

            // Parse the syntax tree (without compilation)
            var syntaxTree = CSharpSyntaxTree.ParseText(
                combinedSource,
                CSharpParseOptions.Default.WithKind(Microsoft.CodeAnalysis.SourceCodeKind.Script),
                cancellationToken: cancellationToken);

            // Walk the tree to find specs
            var walker = new SpecSyntaxWalker();
            walker.Visit(syntaxTree.GetRoot(cancellationToken));

            var result = new StaticParseResult
            {
                Specs = walker.Specs,
                Warnings = walker.Warnings,
                IsComplete = walker.IsComplete
            };

            // Cache the result
            if (_cache != null)
            {
                await _cache.CacheAsync(absolutePath, sourceFiles, result, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
        catch (Exception ex)
        {
            return new StaticParseResult
            {
                Specs = [],
                Warnings = [$"Failed to parse: {ex.Message}"],
                IsComplete = false
            };
        }
    }

    /// <summary>
    /// Recursively processes a CSX file and its #load dependencies.
    /// </summary>
    /// <param name="filePath">Path to the file to process.</param>
    /// <param name="processedFiles">Set of already processed files to prevent circular dependencies.</param>
    /// <param name="sourceFiles">List to populate with all source file paths in processing order.</param>
    /// <param name="codeBuilder">StringBuilder to accumulate the combined source code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ProcessFileAsync(
        string filePath,
        HashSet<string> processedFiles,
        List<string> sourceFiles,
        StringBuilder codeBuilder,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.GetFullPath(filePath);

        // Prevent circular dependencies
        if (!processedFiles.Add(absolutePath))
        {
            return;
        }

        // Track this source file
        sourceFiles.Add(absolutePath);

        string content;
        try
        {
            content = await File.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Skip files that can't be read
            return;
        }

        var fileDirectory = Path.GetDirectoryName(absolutePath)!;

        // Process #load directives first (inline the loaded files)
        var loadMatches = LoadDirectiveRegex().Matches(content);
        foreach (Match match in loadMatches)
        {
            var loadPath = match.Groups["path"].Value;
            var loadAbsolutePath = Path.GetFullPath(loadPath, fileDirectory);

            // Recursively process the loaded file
            await ProcessFileAsync(loadAbsolutePath, processedFiles, sourceFiles, codeBuilder, cancellationToken).ConfigureAwait(false);
        }

        // Remove #load and #r directives from code (they're not valid in parsed source)
        var cleanedCode = LoadDirectiveRegex().Replace(content, "");
        cleanedCode = AssemblyReferenceRegex().Replace(cleanedCode, "");

        // Append the code (keep usings and other directives for proper parsing)
        codeBuilder.AppendLine(cleanedCode);
    }
}
