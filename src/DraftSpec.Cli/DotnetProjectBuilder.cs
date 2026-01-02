namespace DraftSpec.Cli;

/// <summary>
/// Implementation that builds .NET projects using dotnet CLI.
/// </summary>
public class DotnetProjectBuilder : IProjectBuilder
{
    private readonly IFileSystem _fileSystem;
    private readonly IProcessRunner _processRunner;
    private readonly IBuildCache _buildCache;
    private readonly IClock _clock;
    private readonly Dictionary<string, DateTime> _sourceModificationCache = new(StringComparer.OrdinalIgnoreCase);

    public DotnetProjectBuilder(
        IFileSystem fileSystem,
        IProcessRunner processRunner,
        IBuildCache buildCache,
        IClock clock)
    {
        _fileSystem = fileSystem;
        _processRunner = processRunner;
        _buildCache = buildCache;
        _clock = clock;
    }

    public event Action<string>? OnBuildStarted;
    public event Action<BuildResult>? OnBuildCompleted;
    public event Action<string>? OnBuildSkipped;

    public void BuildProjects(string directory)
    {
        var (projects, projectDir) = FindProjectFiles(directory);
        if (projects.Length == 0) return;

        // Check if rebuild is needed (incremental build support)
        var latestSource = GetLatestSourceModification(projectDir);
        if (!_buildCache.NeedsRebuild(projectDir, latestSource))
        {
            foreach (var project in projects) OnBuildSkipped?.Invoke(project);
            return;
        }

        foreach (var project in projects)
        {
            OnBuildStarted?.Invoke(project);

            var result = _processRunner.RunDotnet(
                ["build", project, "--nologo", "-v", "q"],
                projectDir);

            OnBuildCompleted?.Invoke(new BuildResult(result.Success, result.Output, result.Error));
        }

        // Update build cache on successful build
        _buildCache.UpdateCache(projectDir, _clock.UtcNow, latestSource);

        // Invalidate source cache to detect new changes on next run
        InvalidateSourceCache(projectDir);
    }

    public string FindOutputDirectory(string specDirectory)
    {
        // Look for bin/Debug/net* folders
        var (_, projectDir) = FindProjectFiles(specDirectory);
        var binDir = Path.Combine(projectDir, "bin", "Debug");

        if (_fileSystem.DirectoryExists(binDir))
        {
            // Find the first net* folder (e.g., net10.0, net9.0)
            var netDir = _fileSystem.EnumerateDirectories(binDir, "net*").FirstOrDefault();
            if (netDir != null)
            {
                return netDir;
            }
        }

        // Fall back to the spec directory
        return specDirectory;
    }

    public void ClearBuildCache()
    {
        _buildCache.Clear();
        _sourceModificationCache.Clear();
    }

    /// <summary>
    /// Find project files by searching up the directory tree.
    /// </summary>
    internal (string[] Projects, string ProjectDirectory) FindProjectFiles(string specDirectory)
    {
        var currentDir = specDirectory;
        const int maxLevels = 3;

        for (var i = 0; i < maxLevels; i++)
        {
            var projects = _fileSystem.GetFiles(currentDir, "*.csproj");
            if (projects.Length > 0) return (projects, currentDir);

            var parentDir = Directory.GetParent(currentDir)?.FullName;
            if (parentDir == null || string.Equals(parentDir, currentDir, StringComparison.Ordinal)) break;

            currentDir = parentDir;
        }

        return ([], specDirectory);
    }

    /// <summary>
    /// Get the latest source file modification time in the directory.
    /// Uses a cache to avoid repeated file system scans within the same session.
    /// </summary>
    internal DateTime GetLatestSourceModification(string directory)
    {
        var normalizedDir = Path.GetFullPath(directory);

        if (_sourceModificationCache.TryGetValue(normalizedDir, out var cached))
        {
            return cached;
        }

        var latest = ScanForLatestModification(normalizedDir);
        _sourceModificationCache[normalizedDir] = latest;
        return latest;
    }

    /// <summary>
    /// Scan directory for latest source file modification time.
    /// </summary>
    private DateTime ScanForLatestModification(string directory)
    {
        var latest = DateTime.MinValue;

        foreach (var file in _fileSystem.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var modified = _fileSystem.GetLastWriteTimeUtc(file);
            if (modified > latest) latest = modified;
        }

        foreach (var file in _fileSystem.GetFiles(directory, "*.csproj"))
        {
            var modified = _fileSystem.GetLastWriteTimeUtc(file);
            if (modified > latest) latest = modified;
        }

        return latest;
    }

    /// <summary>
    /// Invalidate the source modification cache for a directory.
    /// Called after successful build to detect new changes.
    /// </summary>
    internal void InvalidateSourceCache(string directory)
    {
        var normalizedDir = Path.GetFullPath(directory);
        _sourceModificationCache.Remove(normalizedDir);
    }
}
