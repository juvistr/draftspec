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
internal sealed partial class StaticSpecParser
{
    private readonly string _baseDirectory;

    /// <summary>
    /// Regex to match #load directives for file includes.
    /// </summary>
    [GeneratedRegex(@"^\s*#load\s+""([^""]+)""\s*$", RegexOptions.Multiline)]
    private static partial Regex LoadDirectiveRegex();

    /// <summary>
    /// Creates a new static spec parser.
    /// </summary>
    /// <param name="baseDirectory">Base directory for resolving relative paths.</param>
    public StaticSpecParser(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
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
            // Get combined source with #load files inlined
            var combinedSource = await GetCombinedSourceAsync(absolutePath, cancellationToken);

            // Parse the syntax tree (without compilation)
            var syntaxTree = CSharpSyntaxTree.ParseText(
                combinedSource,
                CSharpParseOptions.Default.WithKind(Microsoft.CodeAnalysis.SourceCodeKind.Script),
                cancellationToken: cancellationToken);

            // Walk the tree to find specs
            var walker = new SpecSyntaxWalker();
            walker.Visit(syntaxTree.GetRoot(cancellationToken));

            return new StaticParseResult
            {
                Specs = walker.Specs,
                Warnings = walker.Warnings,
                IsComplete = walker.IsComplete
            };
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
    /// Gets the combined source with #load files inlined.
    /// </summary>
    private async Task<string> GetCombinedSourceAsync(
        string absolutePath,
        CancellationToken cancellationToken)
    {
        var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var codeBuilder = new StringBuilder();

        await ProcessFileAsync(absolutePath, processedFiles, codeBuilder, cancellationToken);

        return codeBuilder.ToString();
    }

    /// <summary>
    /// Recursively processes a CSX file and its #load dependencies.
    /// </summary>
    private async Task ProcessFileAsync(
        string filePath,
        HashSet<string> processedFiles,
        StringBuilder codeBuilder,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.GetFullPath(filePath);

        // Prevent circular dependencies
        if (!processedFiles.Add(absolutePath))
        {
            return;
        }

        string content;
        try
        {
            content = await File.ReadAllTextAsync(absolutePath, cancellationToken);
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
            var loadPath = match.Groups[1].Value;
            var loadAbsolutePath = Path.GetFullPath(loadPath, fileDirectory);

            // Recursively process the loaded file
            await ProcessFileAsync(loadAbsolutePath, processedFiles, codeBuilder, cancellationToken);
        }

        // Remove #load and #r directives from code (they're not valid in parsed source)
        var cleanedCode = LoadDirectiveRegex().Replace(content, "");
        cleanedCode = Regex.Replace(cleanedCode, @"^\s*#r\s+""[^""]+"".*$", "", RegexOptions.Multiline);

        // Append the code (keep usings and other directives for proper parsing)
        codeBuilder.AppendLine(cleanedCode);
    }
}
