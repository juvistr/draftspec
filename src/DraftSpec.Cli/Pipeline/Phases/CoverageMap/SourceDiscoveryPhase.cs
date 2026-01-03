namespace DraftSpec.Cli.Pipeline.Phases.CoverageMap;

/// <summary>
/// Discovers C# source files at the source path, setting <see cref="ContextKeys.SourceFiles"/>.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[ProjectPath]</c>, <c>Items[SourcePath]</c></para>
/// <para><b>Produces:</b> <c>Items[SourceFiles]</c> - list of C# source file paths</para>
/// <para><b>Short-circuits:</b> If no source files found (returns 1)</para>
/// </remarks>
public sealed class SourceDiscoveryPhase : ICommandPhase
{
    private readonly IFileSystem _fileSystem;

    public SourceDiscoveryPhase(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var projectPath = context.Get<string>(ContextKeys.ProjectPath);
        if (string.IsNullOrEmpty(projectPath))
        {
            context.Console.WriteError("ProjectPath not set. Run PathResolutionPhase first.");
            return Task.FromResult(1);
        }

        var sourcePath = context.Get<string>(ContextKeys.SourcePath);
        if (string.IsNullOrEmpty(sourcePath))
        {
            // Default to project path if not specified
            sourcePath = projectPath;
        }
        else
        {
            // Resolve to absolute path
            sourcePath = Path.GetFullPath(sourcePath);
        }

        // Validate source path exists
        if (!_fileSystem.DirectoryExists(sourcePath) && !_fileSystem.FileExists(sourcePath))
        {
            context.Console.WriteError($"Source path not found: {sourcePath}");
            return Task.FromResult(1);
        }

        // Find source files
        var sourceFiles = GetSourceFiles(sourcePath);
        if (sourceFiles.Count == 0)
        {
            context.Console.WriteError("No C# source files found.");
            return Task.FromResult(1);
        }

        context.Set<IReadOnlyList<string>>(ContextKeys.SourceFiles, sourceFiles);
        return pipeline(context, ct);
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
}
