using System.Text.RegularExpressions;

namespace DraftSpec.Cli.DependencyGraph;

/// <summary>
/// Builds a dependency graph from spec files and source files.
/// Extracts #load dependencies and using directives from spec files,
/// and namespace definitions from .cs source files.
/// </summary>
public sealed partial class DependencyGraphBuilder : IDependencyGraphBuilder
{
    private readonly IPathComparer _pathComparer;

    public DependencyGraphBuilder(IPathComparer pathComparer)
    {
        _pathComparer = pathComparer;
    }
    /// <summary>
    /// Regex to match #load directives in CSX files.
    /// Captures the file path in group 1.
    /// </summary>
    [GeneratedRegex(@"^\s*#load\s+""(?<path>[^""]+)""\s*$", RegexOptions.Multiline | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture)]
    private static partial Regex LoadDirectiveRegex();

    /// <summary>
    /// Regex to match using directives.
    /// Captures the namespace in group 1 (after optional 'static' keyword).
    /// </summary>
    [GeneratedRegex(@"^\s*using\s+(?:static\s+)?(?<ns>[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*)\s*;", RegexOptions.Multiline | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture)]
    private static partial Regex UsingDirectiveRegex();

    /// <summary>
    /// Regex to match namespace declarations in C# files.
    /// Supports both traditional and file-scoped namespace syntax.
    /// Captures the namespace in group 1.
    /// </summary>
    [GeneratedRegex(@"^\s*namespace\s+(?<ns>[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*)\s*[;{]", RegexOptions.Multiline | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture)]
    private static partial Regex NamespaceDeclarationRegex();

    /// <summary>
    /// Builds a dependency graph from spec files in the specified directory.
    /// </summary>
    /// <param name="specDirectory">Directory containing .spec.csx files.</param>
    /// <param name="sourceDirectory">Optional directory containing .cs source files. If null, looks for adjacent project directories.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<DependencyGraph> BuildAsync(
        string specDirectory,
        string? sourceDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var graph = new DependencyGraph(_pathComparer);
        var baseDirectory = Path.GetFullPath(specDirectory);

        // Find all spec files
        var specFiles = Directory.EnumerateFiles(baseDirectory, "*.spec.csx", SearchOption.AllDirectories)
            .ToList();

        // Process each spec file
        foreach (var specFile in specFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dependency = await ParseSpecFileAsync(specFile, cancellationToken);
            graph.AddSpec(dependency);
        }

        // Build namespace mappings from source files
        var sourceDir = sourceDirectory ?? FindSourceDirectory(baseDirectory);
        if (sourceDir != null && Directory.Exists(sourceDir))
        {
            await BuildNamespaceMappingsAsync(graph, sourceDir, cancellationToken);
        }

        return graph;
    }

    /// <summary>
    /// Parses a single spec file to extract its dependencies.
    /// </summary>
    private async Task<SpecDependency> ParseSpecFileAsync(
        string specFilePath,
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(specFilePath, cancellationToken);
        var fileDirectory = Path.GetDirectoryName(specFilePath)!;

        // Collect all #load dependencies (including transitive)
        var loadDependencies = new HashSet<string>(_pathComparer.Comparer);
        var visitedFiles = new HashSet<string>(loadDependencies.Comparer);

        await CollectLoadDependenciesAsync(specFilePath, loadDependencies, visitedFiles, cancellationToken);

        // Extract using directives
        var namespaces = ExtractNamespaces(content);

        return new SpecDependency(
            Path.GetFullPath(specFilePath),
            loadDependencies.ToList(),
            namespaces);
    }

    /// <summary>
    /// Recursively collects #load dependencies from a spec file.
    /// </summary>
    private async Task CollectLoadDependenciesAsync(
        string filePath,
        HashSet<string> dependencies,
        HashSet<string> visitedFiles,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.GetFullPath(filePath);

        if (!visitedFiles.Add(absolutePath))
            return; // Already processed

        if (!File.Exists(absolutePath))
            return;

        var content = await File.ReadAllTextAsync(absolutePath, cancellationToken);
        var fileDirectory = Path.GetDirectoryName(absolutePath)!;

        var loadMatches = LoadDirectiveRegex().Matches(content);
        foreach (Match match in loadMatches)
        {
            var loadPath = match.Groups["path"].Value;
            var loadAbsolutePath = Path.GetFullPath(loadPath, fileDirectory);

            dependencies.Add(loadAbsolutePath);

            // Recursively process loaded files
            await CollectLoadDependenciesAsync(loadAbsolutePath, dependencies, visitedFiles, cancellationToken);
        }
    }

    /// <summary>
    /// Extracts namespace references from using directives.
    /// </summary>
    private static IReadOnlyList<string> ExtractNamespaces(string content)
    {
        var namespaces = new List<string>();
        var matches = UsingDirectiveRegex().Matches(content);

        foreach (Match match in matches)
        {
            var ns = match.Groups["ns"].Value;

            // Filter out system namespaces - we only care about project namespaces
            if (!ns.StartsWith("System", StringComparison.Ordinal) &&
                !ns.StartsWith("Microsoft", StringComparison.Ordinal))
            {
                namespaces.Add(ns);
            }
        }

        return namespaces.Distinct().ToList();
    }

    /// <summary>
    /// Builds namespace → file mappings from C# source files.
    /// </summary>
    private async Task BuildNamespaceMappingsAsync(
        DependencyGraph graph,
        string sourceDirectory,
        CancellationToken cancellationToken)
    {
        var csFiles = Directory.EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories);

        foreach (var csFile in csFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip generated files
            var fileName = Path.GetFileName(csFile);
            if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var content = await File.ReadAllTextAsync(csFile, cancellationToken);
            var matches = NamespaceDeclarationRegex().Matches(content);

            foreach (Match match in matches)
            {
                var ns = match.Groups["ns"].Value;
                graph.AddNamespaceMapping(Path.GetFullPath(csFile), ns);
            }
        }
    }

    /// <summary>
    /// Attempts to find the source directory for a specs directory.
    /// Looks for adjacent 'src' directory or parent project structure.
    /// </summary>
    private static string? FindSourceDirectory(string specDirectory)
    {
        // Common patterns:
        // 1. specs/ next to src/ - look for sibling src/
        // 2. ProjectName.Specs/ next to ProjectName/ - look for sibling without .Specs
        // 3. specs inside project - look in parent

        var parent = Path.GetDirectoryName(specDirectory);
        if (parent == null) return null;

        // Check for sibling 'src' directory
        var srcDir = Path.Combine(parent, "src");
        if (Directory.Exists(srcDir))
            return srcDir;

        // Check for adjacent project directory (e.g., TodoApi next to TodoApi.Specs)
        var dirName = Path.GetFileName(specDirectory);
        if (dirName?.EndsWith(".Specs", StringComparison.OrdinalIgnoreCase) == true)
        {
            var projectName = dirName[..^6]; // Remove .Specs
            var projectDir = Path.Combine(parent, projectName);
            if (Directory.Exists(projectDir))
                return projectDir;
        }

        // Check parent directory
        if (Directory.EnumerateFiles(parent, "*.cs", SearchOption.TopDirectoryOnly).Any())
            return parent;

        return null;
    }
}
