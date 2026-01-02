using DraftSpec.Cli.CoverageMap;
using DraftSpec.Cli.Formatters;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Services;
using Microsoft.CodeAnalysis.CSharp;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Analyzes spec coverage of source code methods.
/// Uses static parsing to discover both source methods and spec references.
/// </summary>
public class CoverageMapCommand : ICommand<CoverageMapOptions>
{
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public CoverageMapCommand(IConsole console, IFileSystem fileSystem)
    {
        _console = console;
        _fileSystem = fileSystem;
    }

    public async Task<int> ExecuteAsync(CoverageMapOptions options, CancellationToken ct = default)
    {
        // 1. Resolve paths
        var sourcePath = Path.GetFullPath(options.SourcePath);
        var projectPath = FindProjectRoot(sourcePath) ?? Path.GetDirectoryName(sourcePath) ?? sourcePath;

        if (!_fileSystem.DirectoryExists(sourcePath) && !_fileSystem.FileExists(sourcePath))
        {
            _console.WriteError($"Source path not found: {sourcePath}");
            return 1;
        }

        // 2. Determine spec path
        var specPath = options.SpecPath != null
            ? Path.GetFullPath(options.SpecPath)
            : projectPath;

        // 3. Find and parse source files
        var sourceFiles = GetSourceFiles(sourcePath);
        if (sourceFiles.Count == 0)
        {
            _console.WriteError("No C# source files found.");
            return 1;
        }

        var methods = await ParseSourceMethodsAsync(sourceFiles, ct);
        if (methods.Count == 0)
        {
            _console.WriteLine("No public methods found in source files.");
            return 0;
        }

        // 4. Apply namespace filter if specified
        if (!string.IsNullOrEmpty(options.NamespaceFilter))
        {
            var filters = options.NamespaceFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            methods = methods
                .Where(m => filters.Any(f => m.Namespace.StartsWith(f, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (methods.Count == 0)
            {
                _console.WriteLine($"No methods found matching namespace filter: {options.NamespaceFilter}");
                return 0;
            }
        }

        // 5. Find and parse spec files
        var specFiles = GetSpecFiles(specPath);
        if (specFiles.Count == 0)
        {
            _console.WriteError("No spec files found.");
            return 1;
        }

        var specReferences = await ParseSpecReferencesAsync(specFiles, projectPath, ct);

        // 6. Compute coverage map
        var mapper = new CoverageMapper();
        var result = mapper.Map(
            methods,
            specReferences,
            Path.GetRelativePath(projectPath, sourcePath),
            Path.GetRelativePath(projectPath, specPath));

        // 7. Format and output
        var formatter = CreateFormatter(options.Format);
        var output = formatter.Format(result, options.GapsOnly);
        _console.WriteLine(output);

        // Return non-zero if gaps-only mode found uncovered methods
        return options.GapsOnly && result.UncoveredMethods.Count > 0 ? 1 : 0;
    }

    private List<string> GetSourceFiles(string path)
    {
        if (_fileSystem.FileExists(path) && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return [path];
        }

        if (_fileSystem.DirectoryExists(path))
        {
            return _fileSystem.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories)
                .Where(f => !IsGeneratedFile(f))
                .ToList();
        }

        return [];
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

    private static bool IsGeneratedFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        // Skip common generated file patterns
        return fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase) ||
               filePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
               filePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<SourceMethod>> ParseSourceMethodsAsync(
        List<string> sourceFiles,
        CancellationToken ct)
    {
        var allMethods = new List<SourceMethod>();

        foreach (var sourceFile in sourceFiles)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var sourceText = await File.ReadAllTextAsync(sourceFile, ct);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, cancellationToken: ct);
                var root = await syntaxTree.GetRootAsync(ct);

                var walker = new SourceMethodWalker(sourceFile);
                walker.Visit(root);

                allMethods.AddRange(walker.Methods);
            }
            catch (Exception ex)
            {
                _console.WriteError($"Failed to parse {sourceFile}: {ex.Message}");
            }
        }

        return allMethods;
    }

    private async Task<List<SpecReference>> ParseSpecReferencesAsync(
        List<string> specFiles,
        string projectPath,
        CancellationToken ct)
    {
        var allReferences = new List<SpecReference>();

        foreach (var specFile in specFiles)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var sourceText = await File.ReadAllTextAsync(specFile, ct);

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
            catch (Exception ex)
            {
                _console.WriteError($"Failed to parse {specFile}: {ex.Message}");
            }
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

    private string? FindProjectRoot(string startPath)
    {
        var dir = _fileSystem.DirectoryExists(startPath)
            ? startPath
            : Path.GetDirectoryName(startPath);

        while (dir != null)
        {
            // Look for solution or project file
            if (_fileSystem.EnumerateFiles(dir, "*.sln", SearchOption.TopDirectoryOnly).Any() ||
                _fileSystem.EnumerateFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly).Any())
            {
                return dir;
            }

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }

    private static ICoverageMapFormatter CreateFormatter(CoverageMapFormat format)
    {
        return format switch
        {
            CoverageMapFormat.Json => new JsonCoverageMapFormatter(),
            _ => new ConsoleCoverageMapFormatter()
        };
    }
}
