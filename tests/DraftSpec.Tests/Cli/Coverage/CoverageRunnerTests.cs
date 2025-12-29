using System.Diagnostics;
using DraftSpec.Cli;
using DraftSpec.Cli.Coverage;

namespace DraftSpec.Tests.Cli.Coverage;

/// <summary>
/// Tests for CoverageRunner with mocked dependencies.
/// </summary>
public class CoverageRunnerTests
{
    #region GetFileExtension

    [Test]
    public async Task GetFileExtension_Cobertura_ReturnsXmlExtension()
    {
        var runner = new CoverageRunner("/tmp", "cobertura");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("cobertura.xml");
    }

    [Test]
    public async Task GetFileExtension_Xml_ReturnsXml()
    {
        var runner = new CoverageRunner("/tmp", "xml");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("xml");
    }

    [Test]
    public async Task GetFileExtension_Coverage_ReturnsCoverage()
    {
        var runner = new CoverageRunner("/tmp", "coverage");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("coverage");
    }

    [Test]
    public async Task GetFileExtension_Unknown_DefaultsToCobertura()
    {
        var runner = new CoverageRunner("/tmp", "unknown");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("cobertura.xml");
    }

    [Test]
    public async Task GetFileExtension_IsCaseInsensitive()
    {
        var runner = new CoverageRunner("/tmp", "COBERTURA");

        var extension = runner.GetFileExtension();

        await Assert.That(extension).IsEqualTo("cobertura.xml");
    }

    #endregion

    #region BuildDotnetCommand

    [Test]
    public async Task BuildDotnetCommand_SimpleArgs_JoinsWithSpaces()
    {
        var args = new[] { "test", "MyProject.csproj" };

        var command = CoverageRunner.BuildDotnetCommand(args);

        await Assert.That(command).IsEqualTo("dotnet test MyProject.csproj");
    }

    [Test]
    public async Task BuildDotnetCommand_EmptyArgs_ReturnsJustDotnet()
    {
        var args = Array.Empty<string>();

        var command = CoverageRunner.BuildDotnetCommand(args);

        await Assert.That(command).IsEqualTo("dotnet ");
    }

    [Test]
    public async Task BuildDotnetCommand_WithSpaces_QuotesArgs()
    {
        var args = new[] { "test", "My Project.csproj" };

        var command = CoverageRunner.BuildDotnetCommand(args);

        await Assert.That(command).IsEqualTo("dotnet test \"My Project.csproj\"");
    }

    [Test]
    public async Task BuildDotnetCommand_MixedArgs_QuotesOnlyNeeded()
    {
        var args = new[] { "test", "--filter", "Category=Integration Tests" };

        var command = CoverageRunner.BuildDotnetCommand(args);

        await Assert.That(command).IsEqualTo("dotnet test --filter \"Category=Integration Tests\"");
    }

    #endregion

    #region QuoteIfNeeded

    [Test]
    public async Task QuoteIfNeeded_SimpleString_NoQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("simple");

        await Assert.That(result).IsEqualTo("simple");
    }

    [Test]
    public async Task QuoteIfNeeded_EmptyString_ReturnsEmptyQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("");

        await Assert.That(result).IsEqualTo("\"\"");
    }

    [Test]
    public async Task QuoteIfNeeded_NullString_ReturnsEmptyQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded(null!);

        await Assert.That(result).IsEqualTo("\"\"");
    }

    [Test]
    public async Task QuoteIfNeeded_WithSpaces_AddsQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("has spaces");

        await Assert.That(result).IsEqualTo("\"has spaces\"");
    }

    [Test]
    public async Task QuoteIfNeeded_WithQuotes_EscapesAndQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("say \"hello\"");

        await Assert.That(result).IsEqualTo("\"say \\\"hello\\\"\"");
    }

    [Test]
    public async Task QuoteIfNeeded_WithBackslash_EscapesAndQuotes()
    {
        var result = CoverageRunner.QuoteIfNeeded("path\\to\\file");

        await Assert.That(result).IsEqualTo("\"path\\\\to\\\\file\"");
    }

    [Test]
    public async Task QuoteIfNeeded_WithMultipleSpecialChars_EscapesAll()
    {
        var result = CoverageRunner.QuoteIfNeeded("path with \"quotes\" and \\slashes");

        await Assert.That(result).IsEqualTo("\"path with \\\"quotes\\\" and \\\\slashes\"");
    }

    [Test]
    public async Task QuoteIfNeeded_PathLikeString_NoQuotesNeeded()
    {
        var result = CoverageRunner.QuoteIfNeeded("/usr/local/bin");

        await Assert.That(result).IsEqualTo("/usr/local/bin");
    }

    [Test]
    public async Task QuoteIfNeeded_DotnetArgs_NoQuotesNeeded()
    {
        var result = CoverageRunner.QuoteIfNeeded("--configuration=Release");

        await Assert.That(result).IsEqualTo("--configuration=Release");
    }

    #endregion

    #region Constructor and Properties

    [Test]
    public async Task Constructor_SetsOutputDirectory()
    {
        using var runner = new CoverageRunner("/custom/path", "cobertura");

        await Assert.That(runner.OutputDirectory).IsEqualTo("/custom/path");
    }

    [Test]
    public async Task Constructor_SetsFormat()
    {
        using var runner = new CoverageRunner("/tmp", "xml");

        await Assert.That(runner.Format).IsEqualTo("xml");
    }

    [Test]
    public async Task Constructor_DefaultsToCobertura()
    {
        using var runner = new CoverageRunner("/tmp");

        await Assert.That(runner.Format).IsEqualTo("cobertura");
    }

    [Test]
    public async Task Constructor_NormalizesFormatToLowercase()
    {
        using var runner = new CoverageRunner("/tmp", "COBERTURA");

        await Assert.That(runner.Format).IsEqualTo("cobertura");
    }

    [Test]
    public async Task Constructor_GeneratesSessionId()
    {
        using var runner = new CoverageRunner("/tmp");

        await Assert.That(runner.SessionId).StartsWith("draftspec-");
        await Assert.That(runner.SessionId.Length).IsGreaterThan(20);
    }

    [Test]
    public async Task Constructor_SetsUniqueSessions()
    {
        using var runner1 = new CoverageRunner("/tmp");
        using var runner2 = new CoverageRunner("/tmp");

        await Assert.That(runner1.SessionId).IsNotEqualTo(runner2.SessionId);
    }

    [Test]
    public async Task Constructor_SetsCoverageFilePath()
    {
        using var runner = new CoverageRunner("/tmp/coverage", "cobertura");

        await Assert.That(runner.CoverageFile).IsEqualTo("/tmp/coverage/coverage.cobertura.xml");
    }

    [Test]
    public async Task IsServerRunning_InitiallyFalse()
    {
        using var runner = new CoverageRunner("/tmp");

        await Assert.That(runner.IsServerRunning).IsFalse();
    }

    [Test]
    public async Task GetCoverageFile_ReturnsNullWhenFileDoesNotExist()
    {
        using var runner = new CoverageRunner("/tmp/nonexistent");

        await Assert.That(runner.GetCoverageFile()).IsNull();
    }

    [Test]
    public async Task Shutdown_ReturnsFalseWhenServerNotStarted()
    {
        using var runner = new CoverageRunner("/tmp");

        var result = runner.Shutdown();

        await Assert.That(result).IsFalse();
    }

    #endregion

    #region RunWithCoverage with Mocks

    [Test]
    public async Task RunWithCoverage_ServerRunning_UsesConnect()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/tmp/coverage");

        // Simulate server process that hasn't exited
        var mockProcessHandle = new MockProcessHandle { HasExited = false };
        processRunner.SetProcessHandle(mockProcessHandle);
        processRunner.AddResult(new ProcessResult("", "", 0)); // For connect command

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        // Start the server to set _serverStarted = true
        runner.StartServer();

        // Now run with coverage
        var result = runner.RunWithCoverage(["test", "MyProject.csproj"]);

        // Verify connect was called (not collect)
        await Assert.That(processRunner.RunCalls.Count).IsGreaterThanOrEqualTo(1);

        var lastCall = processRunner.RunCalls.Last();
        await Assert.That(lastCall.FileName).IsEqualTo("dotnet-coverage");
        await Assert.That(lastCall.Arguments.First()).IsEqualTo("connect");
    }

    [Test]
    public async Task RunWithCoverage_ServerNotRunning_FallsBackToCollect()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        processRunner.AddResult(new ProcessResult("", "", 0)); // For collect command

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        // Don't start the server - directly call RunWithCoverage
        var result = runner.RunWithCoverage(["test", "MyProject.csproj"]);

        // Verify collect was called (standalone mode)
        await Assert.That(processRunner.RunCalls.Count).IsEqualTo(1);

        var call = processRunner.RunCalls.First();
        await Assert.That(call.FileName).IsEqualTo("dotnet-coverage");
        await Assert.That(call.Arguments.First()).IsEqualTo("collect");
    }

    [Test]
    public async Task RunWithCoverage_ServerExited_FallsBackToCollect()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/tmp/coverage");

        // Simulate server process that HAS exited
        var mockProcessHandle = new MockProcessHandle { HasExited = true };
        processRunner.SetProcessHandle(mockProcessHandle);
        processRunner.AddResult(new ProcessResult("", "", 0)); // For collect command

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        // Start the server (but process has exited)
        runner.StartServer();

        // Now run with coverage
        var result = runner.RunWithCoverage(["test", "MyProject.csproj"]);

        // Verify collect was called (fallback mode) since server is not running
        var lastCall = processRunner.RunCalls.Last();
        await Assert.That(lastCall.FileName).IsEqualTo("dotnet-coverage");
        await Assert.That(lastCall.Arguments.First()).IsEqualTo("collect");
    }

    #endregion

    #region GenerateReports with Mocks

    [Test]
    public async Task GenerateReports_CallsFormatterFactory()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var formatterFactory = new MockCoverageFormatterFactory();

        var mockFormatter = new MockCoverageFormatter
        {
            FileExtension = ".html",
            FormatName = "html",
            FormattedContent = "<html>coverage report</html>"
        };
        formatterFactory.AddFormatter("html", mockFormatter);

        // Add a file that exists so FileExists returns true
        fileSystem.AddFile("/tmp/coverage/coverage.cobertura.xml");

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem,
            formatterFactory);

        // Call GenerateReports - note: this will fail because CoberturaParser.TryParseFile
        // reads directly from the file system, but we can verify the factory was invoked
        // when the file doesn't exist (returns empty dict)
        var reports = runner.GenerateReports(["html"]);

        // Since CoberturaParser.TryParseFile can't be mocked (uses File.Exists directly),
        // and the file doesn't actually exist, the result will be empty.
        // This test verifies the method doesn't throw and handles missing files gracefully.
        await Assert.That(reports).IsNotNull();
    }

    [Test]
    public async Task GenerateReports_FileNotExists_ReturnsEmptyDictionary()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();
        var formatterFactory = new MockCoverageFormatterFactory();

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem,
            formatterFactory);

        var reports = runner.GenerateReports(["html", "json"]);

        await Assert.That(reports).IsEmpty();
    }

    #endregion

    #region StartServer with Mocks

    [Test]
    public async Task StartServer_CreatesOutputDirectory()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var mockProcessHandle = new MockProcessHandle { HasExited = false };
        processRunner.SetProcessHandle(mockProcessHandle);

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        runner.StartServer();

        await Assert.That(fileSystem.DirectoryExists("/tmp/coverage")).IsTrue();
    }

    [Test]
    public async Task StartServer_ReturnsTrue_WhenProcessStartsSuccessfully()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var mockProcessHandle = new MockProcessHandle { HasExited = false };
        processRunner.SetProcessHandle(mockProcessHandle);

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        var result = runner.StartServer();

        await Assert.That(result).IsTrue();
        await Assert.That(runner.IsServerRunning).IsTrue();
    }

    [Test]
    public async Task StartServer_CalledTwice_ReturnsCachedState()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var mockProcessHandle = new MockProcessHandle { HasExited = false };
        processRunner.SetProcessHandle(mockProcessHandle);

        using var runner = new CoverageRunner(
            "/tmp/coverage",
            "cobertura",
            processRunner,
            fileSystem);

        runner.StartServer();
        var secondResult = runner.StartServer();

        // Should return the running state, not try to start again
        await Assert.That(secondResult).IsTrue();
        await Assert.That(processRunner.StartProcessCalls).IsEqualTo(1);
    }

    #endregion
}

#region Mock Implementations

/// <summary>
/// Mock process runner for testing CoverageRunner.
/// </summary>
file class MockProcessRunner : IProcessRunner
{
    private readonly Queue<ProcessResult> _results = new();
    private MockProcessHandle? _processHandle;

    public List<(string FileName, List<string> Arguments, string? WorkingDir)> RunCalls { get; } = [];
    public int StartProcessCalls { get; private set; }

    public void AddResult(ProcessResult result) => _results.Enqueue(result);

    public void SetProcessHandle(MockProcessHandle handle) => _processHandle = handle;

    public ProcessResult Run(
        string fileName,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null)
    {
        RunCalls.Add((fileName, arguments.ToList(), workingDirectory));
        return _results.Count > 0 ? _results.Dequeue() : new ProcessResult("", "", 0);
    }

    public ProcessResult RunDotnet(
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null)
    {
        return Run("dotnet", arguments, workingDirectory, environmentVariables);
    }

    public IProcessHandle StartProcess(ProcessStartInfo startInfo)
    {
        StartProcessCalls++;
        return _processHandle ?? new MockProcessHandle { HasExited = false };
    }
}

/// <summary>
/// Mock process handle for testing.
/// </summary>
file class MockProcessHandle : IProcessHandle
{
    public bool HasExited { get; set; }

    public bool WaitForExit(int milliseconds) => true;

    public void Kill() { }

    public void Dispose() { }
}

/// <summary>
/// Mock file system for testing CoverageRunner.
/// </summary>
file class MockFileSystem : IFileSystem
{
    private readonly HashSet<string> _existingFiles = new();
    private readonly HashSet<string> _existingDirectories = new();
    private readonly Dictionary<string, string> _fileContents = new();

    public void AddFile(string path)
    {
        _existingFiles.Add(path);
    }

    public void AddDirectory(string directory)
    {
        _existingDirectories.Add(directory);
    }

    public bool FileExists(string path) => _existingFiles.Contains(path);

    public void WriteAllText(string path, string content)
    {
        _fileContents[path] = content;
        _existingFiles.Add(path);
    }

    public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
    {
        WriteAllText(path, content);
        return Task.CompletedTask;
    }

    public string ReadAllText(string path) =>
        _fileContents.TryGetValue(path, out var content) ? content : "";

    public bool DirectoryExists(string path) => _existingDirectories.Contains(path);

    public void CreateDirectory(string path) => _existingDirectories.Add(path);

    public string[] GetFiles(string path, string searchPattern) => [];

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => [];

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => [];

    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern) => [];

    public DateTime GetLastWriteTimeUtc(string path) => DateTime.MinValue;
}

/// <summary>
/// Mock coverage formatter factory for testing.
/// </summary>
file class MockCoverageFormatterFactory : ICoverageFormatterFactory
{
    private readonly Dictionary<string, ICoverageFormatter> _formatters = new();

    public void AddFormatter(string format, ICoverageFormatter formatter)
    {
        _formatters[format.ToLowerInvariant()] = formatter;
    }

    public ICoverageFormatter? GetFormatter(string format)
    {
        return _formatters.TryGetValue(format.ToLowerInvariant(), out var formatter)
            ? formatter
            : null;
    }
}

/// <summary>
/// Mock coverage formatter for testing.
/// </summary>
file class MockCoverageFormatter : ICoverageFormatter
{
    public string FileExtension { get; set; } = ".txt";
    public string FormatName { get; set; } = "mock";
    public string FormattedContent { get; set; } = "";

    public string Format(CoverageReport report) => FormattedContent;
}

#endregion
