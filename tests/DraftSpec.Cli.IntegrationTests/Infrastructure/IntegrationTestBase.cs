namespace DraftSpec.Cli.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for CLI integration tests.
/// Provides temp directory management and CLI execution helpers.
/// </summary>
public abstract class IntegrationTestBase
{
    protected string _tempDir = null!;
    private static string? _cliDllPath;
    private static string? _solutionDir;
    private static readonly object _buildLock = new();
    private static bool _cliBuilt;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"DraftSpec_IntegrationTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [After(Test)]
    public void TearDown()
    {
        // Retry cleanup with delays to handle Windows file locking
        const int maxRetries = 3;
        const int delayMs = 100;

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, recursive: true);
                }
                return; // Success
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                // File might be locked, wait and retry
                Thread.Sleep(delayMs * (i + 1));
            }
            catch (UnauthorizedAccessException) when (i < maxRetries - 1)
            {
                // Permission issue, wait and retry
                Thread.Sleep(delayMs * (i + 1));
            }
            catch
            {
                // Final attempt or other error - don't fail the test
                return;
            }
        }
    }

    /// <summary>
    /// Creates a new spec fixture builder for this test.
    /// </summary>
    protected SpecFixtureBuilder CreateFixture()
        => new(Path.Combine(_tempDir, $"specs_{Guid.NewGuid():N}"));

    /// <summary>
    /// Creates a history file builder for a project directory.
    /// </summary>
    protected HistoryFileBuilder CreateHistoryFile(string projectDir)
        => new(projectDir);

    /// <summary>
    /// Creates a git repository builder for this test.
    /// </summary>
    protected GitRepositoryBuilder CreateGitRepo()
        => new(Path.Combine(_tempDir, $"repo_{Guid.NewGuid():N}"));

    /// <summary>
    /// Runs the CLI with the given arguments and returns the result.
    /// </summary>
    protected Task<ProcessResult> RunCliAsync(params string[] args)
    {
        var dllPath = EnsureCliBuilt();
        // Run CLI via dotnet exec
        var dotnetArgs = new[] { "exec", dllPath }.Concat(args);
        return Task.FromResult(ProcessHelper.RunDotnet(dotnetArgs, _tempDir));
    }

    /// <summary>
    /// Runs the CLI with a specific working directory.
    /// </summary>
    protected Task<ProcessResult> RunCliInDirectoryAsync(string workingDirectory, params string[] args)
    {
        var dllPath = EnsureCliBuilt();
        // Run CLI via dotnet exec
        var dotnetArgs = new[] { "exec", dllPath }.Concat(args);
        return Task.FromResult(ProcessHelper.RunDotnet(dotnetArgs, workingDirectory));
    }

    /// <summary>
    /// Gets the solution directory path.
    /// </summary>
    protected static string GetSolutionDirectory()
    {
        if (_solutionDir != null) return _solutionDir;

        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "DraftSpec.sln")))
            {
                _solutionDir = dir;
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException("Could not find solution directory");
    }

    private static string EnsureCliBuilt()
    {
        if (_cliDllPath != null && _cliBuilt) return _cliDllPath;

        lock (_buildLock)
        {
            if (_cliDllPath != null && _cliBuilt) return _cliDllPath;

            var solutionDir = GetSolutionDirectory();
            var projectPath = Path.Combine(solutionDir, "src", "DraftSpec.Cli", "DraftSpec.Cli.csproj");

            // Build the CLI project
            var buildResult = ProcessHelper.RunDotnet(
                ["build", projectPath, "-c", "Release", "--nologo", "-v", "q"]);

            if (!buildResult.Success)
            {
                throw new InvalidOperationException(
                    $"Failed to build CLI project: {buildResult.Error}");
            }

            // Find the built DLL
            var outputDir = Path.Combine(solutionDir, "src", "DraftSpec.Cli", "bin", "Release", "net10.0");
            _cliDllPath = Path.Combine(outputDir, "DraftSpec.Cli.dll");

            if (!File.Exists(_cliDllPath))
            {
                throw new InvalidOperationException(
                    $"CLI DLL not found at {_cliDllPath}");
            }

            _cliBuilt = true;
            return _cliDllPath;
        }
    }
}
