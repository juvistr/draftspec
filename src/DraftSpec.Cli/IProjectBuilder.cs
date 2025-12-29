namespace DraftSpec.Cli;

/// <summary>
/// Discovers and builds .NET projects for spec execution.
/// </summary>
public interface IProjectBuilder
{
    /// <summary>
    /// Event raised when a build starts.
    /// </summary>
    event Action<string>? OnBuildStarted;

    /// <summary>
    /// Event raised when a build completes.
    /// </summary>
    event Action<BuildResult>? OnBuildCompleted;

    /// <summary>
    /// Event raised when a build is skipped (no changes detected).
    /// </summary>
    event Action<string>? OnBuildSkipped;

    /// <summary>
    /// Build projects in the given directory.
    /// </summary>
    /// <param name="directory">The directory containing specs.</param>
    void BuildProjects(string directory);

    /// <summary>
    /// Find the output directory for assemblies (e.g., bin/Debug/net10.0).
    /// </summary>
    /// <param name="specDirectory">The directory containing specs.</param>
    /// <returns>The path to the output directory.</returns>
    string FindOutputDirectory(string specDirectory);

    /// <summary>
    /// Clear the build cache to force rebuilds.
    /// </summary>
    void ClearBuildCache();
}

/// <summary>
/// Implementation that builds .NET projects using dotnet CLI.
/// </summary>
public class DotnetProjectBuilder : IProjectBuilder
{
    private readonly IFileSystem _fileSystem;
    private readonly IProcessRunner _processRunner;
    private readonly IBuildCache _buildCache;
    private readonly ITimeProvider _timeProvider;

    public DotnetProjectBuilder(
        IFileSystem fileSystem,
        IProcessRunner processRunner,
        IBuildCache buildCache,
        ITimeProvider timeProvider)
    {
        _fileSystem = fileSystem;
        _processRunner = processRunner;
        _buildCache = buildCache;
        _timeProvider = timeProvider;
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
        _buildCache.UpdateCache(projectDir, _timeProvider.UtcNow, latestSource);
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
            if (parentDir == null || parentDir == currentDir) break;

            currentDir = parentDir;
        }

        return ([], specDirectory);
    }

    /// <summary>
    /// Get the latest source file modification time in the directory.
    /// </summary>
    internal DateTime GetLatestSourceModification(string directory)
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
}
