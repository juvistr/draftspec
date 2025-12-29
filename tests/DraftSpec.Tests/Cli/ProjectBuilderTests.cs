using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for DotnetProjectBuilder with mocked dependencies.
/// </summary>
public class ProjectBuilderTests
{
    #region BuildProjects Tests

    [Test]
    public async Task BuildProjects_NoProjectFiles_DoesNothing()
    {
        var fileSystem = new MockFileSystem();
        var processRunner = new MockProcessRunner();
        var buildCache = new InMemoryBuildCache();
        var timeProvider = new MockTimeProvider();

        var builder = new DotnetProjectBuilder(fileSystem, processRunner, buildCache, timeProvider);

        var buildStartedCalled = false;
        builder.OnBuildStarted += _ => buildStartedCalled = true;

        builder.BuildProjects("/some/spec/dir");

        await Assert.That(buildStartedCalled).IsFalse();
        await Assert.That(processRunner.RunDotnetCalls).IsEmpty();
    }

    [Test]
    public async Task BuildProjects_WithProjectFile_BuildsProject()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFiles("/project", ["MyProject.csproj"]);
        fileSystem.AddFiles("/project", ["Program.cs"]);

        var processRunner = new MockProcessRunner();
        processRunner.AddResult(new ProcessResult("Build succeeded", "", 0));

        var buildCache = new InMemoryBuildCache();
        var timeProvider = new MockTimeProvider { CurrentUtcNow = DateTime.UtcNow };

        var builder = new DotnetProjectBuilder(fileSystem, processRunner, buildCache, timeProvider);

        string? buildStartedProject = null;
        builder.OnBuildStarted += p => buildStartedProject = p;

        BuildResult? buildResult = null;
        builder.OnBuildCompleted += r => buildResult = r;

        builder.BuildProjects("/project");

        await Assert.That(buildStartedProject).Contains("MyProject.csproj");
        await Assert.That(buildResult).IsNotNull();
        await Assert.That(buildResult!.Success).IsTrue();
    }

    [Test]
    public async Task BuildProjects_CacheHit_SkipsBuild()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFiles("/project", ["MyProject.csproj"]);
        fileSystem.AddFiles("/project", ["Program.cs"]);
        fileSystem.SetLastWriteTimeUtc("/project/Program.cs", DateTime.UtcNow.AddHours(-1));
        fileSystem.SetLastWriteTimeUtc("/project/MyProject.csproj", DateTime.UtcNow.AddHours(-1));

        var processRunner = new MockProcessRunner();
        var buildCache = new InMemoryBuildCache();
        var timeProvider = new MockTimeProvider { CurrentUtcNow = DateTime.UtcNow };

        // Pre-populate the cache
        buildCache.UpdateCache("/project", DateTime.UtcNow, DateTime.UtcNow.AddHours(-1));

        var builder = new DotnetProjectBuilder(fileSystem, processRunner, buildCache, timeProvider);

        string? skippedProject = null;
        builder.OnBuildSkipped += p => skippedProject = p;

        builder.BuildProjects("/project");

        await Assert.That(skippedProject).Contains("MyProject.csproj");
        await Assert.That(processRunner.RunDotnetCalls).IsEmpty();
    }

    [Test]
    public async Task BuildProjects_SourceModified_Rebuilds()
    {
        var now = DateTime.UtcNow;
        var fileSystem = new MockFileSystem();
        fileSystem.AddFiles("/project", ["MyProject.csproj"]);
        fileSystem.AddFiles("/project", ["Program.cs"]);
        fileSystem.SetLastWriteTimeUtc("/project/Program.cs", now);
        fileSystem.SetLastWriteTimeUtc("/project/MyProject.csproj", now.AddHours(-2));

        var processRunner = new MockProcessRunner();
        processRunner.AddResult(new ProcessResult("Build succeeded", "", 0));

        var buildCache = new InMemoryBuildCache();
        // Cache says last build was an hour ago with source from 2 hours ago
        buildCache.UpdateCache("/project", now.AddHours(-1), now.AddHours(-2));

        var timeProvider = new MockTimeProvider { CurrentUtcNow = now };

        var builder = new DotnetProjectBuilder(fileSystem, processRunner, buildCache, timeProvider);

        BuildResult? buildResult = null;
        builder.OnBuildCompleted += r => buildResult = r;

        builder.BuildProjects("/project");

        // Should have built because source was modified
        await Assert.That(buildResult).IsNotNull();
        await Assert.That(processRunner.RunDotnetCalls).Count().IsEqualTo(1);
    }

    #endregion

    #region FindOutputDirectory Tests

    [Test]
    public async Task FindOutputDirectory_WithNetFolder_ReturnsNetPath()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFiles("/project", ["MyProject.csproj"]);
        fileSystem.AddDirectory("/project/bin/Debug");
        fileSystem.AddDirectory("/project/bin/Debug/net10.0");

        var builder = new DotnetProjectBuilder(
            fileSystem,
            new MockProcessRunner(),
            new InMemoryBuildCache(),
            new MockTimeProvider());

        var result = builder.FindOutputDirectory("/project");

        await Assert.That(result).IsEqualTo("/project/bin/Debug/net10.0");
    }

    [Test]
    public async Task FindOutputDirectory_NoBinFolder_ReturnsSpecDirectory()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFiles("/project", ["MyProject.csproj"]);

        var builder = new DotnetProjectBuilder(
            fileSystem,
            new MockProcessRunner(),
            new InMemoryBuildCache(),
            new MockTimeProvider());

        var result = builder.FindOutputDirectory("/project/Specs");

        await Assert.That(result).IsEqualTo("/project/Specs");
    }

    #endregion

    #region ClearBuildCache Tests

    [Test]
    public async Task ClearBuildCache_ClearsUnderlyingCache()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFiles("/project", ["MyProject.csproj"]);
        fileSystem.AddFiles("/project", ["Program.cs"]);

        var processRunner = new MockProcessRunner();
        processRunner.AddResult(new ProcessResult("", "", 0));
        processRunner.AddResult(new ProcessResult("", "", 0));

        var buildCache = new InMemoryBuildCache();
        var timeProvider = new MockTimeProvider { CurrentUtcNow = DateTime.UtcNow };

        var builder = new DotnetProjectBuilder(fileSystem, processRunner, buildCache, timeProvider);

        // First build
        builder.BuildProjects("/project");
        await Assert.That(processRunner.RunDotnetCalls).Count().IsEqualTo(1);

        // Clear cache
        builder.ClearBuildCache();

        // Should rebuild after clear
        builder.BuildProjects("/project");
        await Assert.That(processRunner.RunDotnetCalls).Count().IsEqualTo(2);
    }

    #endregion

    #region FindProjectFiles Tests

    [Test]
    public async Task FindProjectFiles_SearchesUpDirectoryTree()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFiles("/project", ["MyProject.csproj"]);
        fileSystem.AddFiles("/project", ["Program.cs"]);

        var processRunner = new MockProcessRunner();
        processRunner.AddResult(new ProcessResult("", "", 0));

        var builder = new DotnetProjectBuilder(
            fileSystem,
            processRunner,
            new InMemoryBuildCache(),
            new MockTimeProvider { CurrentUtcNow = DateTime.UtcNow });

        string? buildStartedProject = null;
        builder.OnBuildStarted += p => buildStartedProject = p;

        // Spec is in a subdirectory
        builder.BuildProjects("/project/Specs");

        await Assert.That(buildStartedProject).Contains("MyProject.csproj");
    }

    #endregion
}

#region Mock Implementations

/// <summary>
/// Mock file system for testing.
/// </summary>
file class MockFileSystem : IFileSystem
{
    private readonly Dictionary<string, List<string>> _directories = new();
    private readonly Dictionary<string, DateTime> _lastWriteTimes = new();
    private readonly HashSet<string> _existingDirectories = new();

    public void AddFiles(string directory, string[] files)
    {
        if (!_directories.ContainsKey(directory))
            _directories[directory] = [];

        _directories[directory].AddRange(files);
        _existingDirectories.Add(directory);
    }

    public void AddDirectory(string directory)
    {
        _existingDirectories.Add(directory);
        if (!_directories.ContainsKey(directory))
            _directories[directory] = [];
    }

    public void SetLastWriteTimeUtc(string path, DateTime time)
    {
        _lastWriteTimes[path] = time;
    }

    public bool FileExists(string path) => false;
    public void WriteAllText(string path, string content) { }
    public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default) => Task.CompletedTask;
    public string ReadAllText(string path) => "";
    public bool DirectoryExists(string path) => _existingDirectories.Contains(path);
    public void CreateDirectory(string path) => _existingDirectories.Add(path);

    public string[] GetFiles(string path, string searchPattern)
    {
        if (!_directories.TryGetValue(path, out var files))
            return [];

        var pattern = searchPattern.Replace("*", "");
        return files.Where(f => f.EndsWith(pattern)).Select(f => Path.Combine(path, f)).ToArray();
    }

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
    {
        return GetFiles(path, searchPattern);
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
    {
        return GetFiles(path, searchPattern, searchOption);
    }

    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern)
    {
        var pattern = searchPattern.Replace("*", "");
        return _existingDirectories
            .Where(d => d.StartsWith(path + "/") && d != path)
            .Where(d => Path.GetFileName(d).StartsWith(pattern));
    }

    public DateTime GetLastWriteTimeUtc(string path)
    {
        return _lastWriteTimes.TryGetValue(path, out var time) ? time : DateTime.MinValue;
    }
}

/// <summary>
/// Mock process runner for testing.
/// </summary>
file class MockProcessRunner : IProcessRunner
{
    private readonly Queue<ProcessResult> _results = new();
    public List<(IEnumerable<string> Args, string? WorkingDir)> RunDotnetCalls { get; } = [];

    public void AddResult(ProcessResult result) => _results.Enqueue(result);

    public ProcessResult Run(string fileName, IEnumerable<string> arguments, string? workingDirectory = null, Dictionary<string, string>? environmentVariables = null)
    {
        return _results.Count > 0 ? _results.Dequeue() : new ProcessResult("", "", 0);
    }

    public ProcessResult RunDotnet(IEnumerable<string> arguments, string? workingDirectory = null, Dictionary<string, string>? environmentVariables = null)
    {
        RunDotnetCalls.Add((arguments.ToList(), workingDirectory));
        return _results.Count > 0 ? _results.Dequeue() : new ProcessResult("", "", 0);
    }

    public IProcessHandle StartProcess(System.Diagnostics.ProcessStartInfo startInfo)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Mock time provider for testing.
/// </summary>
file class MockTimeProvider : ITimeProvider
{
    public DateTime CurrentUtcNow { get; set; } = DateTime.UtcNow;
    public DateTime UtcNow => CurrentUtcNow;

    private readonly MockStopwatch _stopwatch = new();
    public IStopwatch StartNew() => _stopwatch;
}

/// <summary>
/// Mock stopwatch for testing.
/// </summary>
file class MockStopwatch : IStopwatch
{
    public TimeSpan Elapsed => TimeSpan.FromMilliseconds(100);
    public void Stop() { }
}

#endregion
