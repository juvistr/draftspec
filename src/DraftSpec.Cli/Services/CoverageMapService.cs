using DraftSpec.Cli.CoverageMap;
using Microsoft.CodeAnalysis.CSharp;

namespace DraftSpec.Cli.Services;

/// <summary>
/// Computes coverage mapping between source methods and specs.
/// </summary>
public sealed class CoverageMapService : ICoverageMapService
{
    /// <inheritdoc />
    public async Task<CoverageMapResult> ComputeCoverageAsync(
        IReadOnlyList<string> sourceFiles,
        IReadOnlyList<string> specFiles,
        string projectPath,
        string? sourcePath = null,
        string? specPath = null,
        string? namespaceFilter = null,
        CancellationToken ct = default)
    {
        // 1. Parse source files for methods
        var methods = await ParseSourceMethodsAsync(sourceFiles, ct);

        // 2. Apply namespace filter if specified
        if (!string.IsNullOrEmpty(namespaceFilter))
        {
            var filters = namespaceFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            methods = methods
                .Where(m => filters.Any(f => m.Namespace.StartsWith(f, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        // 3. Parse spec files for references
        var specReferences = await ParseSpecReferencesAsync(specFiles, projectPath, ct);

        // 4. Compute coverage mapping
        var mapper = new CoverageMapper();
        return mapper.Map(methods, specReferences, sourcePath, specPath);
    }

    private static async Task<List<SourceMethod>> ParseSourceMethodsAsync(
        IReadOnlyList<string> sourceFiles,
        CancellationToken ct)
    {
        var allMethods = new List<SourceMethod>();

        foreach (var sourceFile in sourceFiles)
        {
            ct.ThrowIfCancellationRequested();

            var sourceText = await File.ReadAllTextAsync(sourceFile, ct).ConfigureAwait(false);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, cancellationToken: ct);
            var root = await syntaxTree.GetRootAsync(ct);

            var walker = new SourceMethodWalker(sourceFile);
            walker.Visit(root);

            allMethods.AddRange(walker.Methods);
        }

        return allMethods;
    }

    private static async Task<List<SpecReference>> ParseSpecReferencesAsync(
        IReadOnlyList<string> specFiles,
        string projectPath,
        CancellationToken ct)
    {
        var allReferences = new List<SpecReference>();

        foreach (var specFile in specFiles)
        {
            ct.ThrowIfCancellationRequested();

            var sourceText = await File.ReadAllTextAsync(specFile, ct).ConfigureAwait(false);

            // Strip #load and #r directives for parsing (they're not valid C# syntax)
            sourceText = StripDirectives(sourceText);

            var parseOptions = CSharpParseOptions.Default
                .WithKind(Microsoft.CodeAnalysis.SourceCodeKind.Script);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, parseOptions, cancellationToken: ct);
            var root = await syntaxTree.GetRootAsync(ct);

            var analyzer = new SpecBodyAnalyzer(specFile, projectPath);
            analyzer.Visit(root);

            allReferences.AddRange(analyzer.SpecReferences);
        }

        return allReferences;
    }

    private static string StripDirectives(string sourceText)
    {
        // Remove #load and #r directives (line by line)
        var lines = sourceText.Split('\n');
        var filteredLines = lines.Where(line =>
        {
            var trimmed = line.TrimStart();
            return !trimmed.StartsWith("#load", StringComparison.Ordinal) &&
                   !trimmed.StartsWith("#r", StringComparison.Ordinal);
        });
        return string.Join('\n', filteredLines);
    }
}
