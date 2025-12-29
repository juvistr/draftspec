using System.Diagnostics;
using DraftSpec.Cli;
using DraftSpec.Cli.Coverage;

namespace DraftSpec.Tests.Cli.Coverage;

/// <summary>
/// Tests for DotnetCoverageServer with mocked dependencies.
/// </summary>
public class CoverageServerTests
{
    #region Constructor Tests

    [Test]
    public async Task Constructor_SetsSessionId_WithDraftspecPrefix()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");

        await Assert.That(server.SessionId).StartsWith("draftspec-");
        await Assert.That(server.SessionId.Length).IsGreaterThan(20);
    }

    [Test]
    public async Task Constructor_SetsCoverageFile_WithCoberturaExtension()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");

        await Assert.That(server.CoverageFile).IsEqualTo("/tmp/coverage/coverage.cobertura.xml");
    }

    [Test]
    public async Task Constructor_SetsCoverageFile_WithXmlFormat()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage", "xml");

        await Assert.That(server.CoverageFile).IsEqualTo("/tmp/coverage/coverage.xml");
    }

    [Test]
    public async Task Constructor_SetsCoverageFile_WithCoverageFormat()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage", "coverage");

        await Assert.That(server.CoverageFile).IsEqualTo("/tmp/coverage/coverage.coverage");
    }

    [Test]
    public async Task Constructor_NormalizesFormatToLowercase()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage", "COBERTURA");

        await Assert.That(server.CoverageFile).IsEqualTo("/tmp/coverage/coverage.cobertura.xml");
    }

    [Test]
    public async Task Constructor_GeneratesUniqueSessionIds()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var server1 = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        var server2 = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");

        await Assert.That(server1.SessionId).IsNotEqualTo(server2.SessionId);
    }

    #endregion

    #region IsRunning Tests

    [Test]
    public async Task IsRunning_ReturnsFalseInitially()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");

        await Assert.That(server.IsRunning).IsFalse();
    }

    [Test]
    public async Task IsRunning_ReturnsTrueWhenServerStartedAndNotExited()
    {
        var processRunner = new MockProcessRunner();
        var processHandle = new MockProcessHandle { HasExited = false };
        processRunner.ProcessHandleToReturn = processHandle;

        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/tmp/coverage");

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();

        await Assert.That(server.IsRunning).IsTrue();
    }

    [Test]
    public async Task IsRunning_ReturnsFalseWhenProcessHasExited()
    {
        var processRunner = new MockProcessRunner();
        var processHandle = new MockProcessHandle { HasExited = true };
        processRunner.ProcessHandleToReturn = processHandle;

        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/tmp/coverage");

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();

        await Assert.That(server.IsRunning).IsFalse();
    }

    #endregion

    #region Start Tests

    [Test]
    public async Task Start_CreatesOutputDirectoryIfNotExists()
    {
        var processRunner = new MockProcessRunner();
        processRunner.ProcessHandleToReturn = new MockProcessHandle { HasExited = false };

        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();

        await Assert.That(fileSystem.DirectoryExists("/tmp/coverage")).IsTrue();
    }

    [Test]
    public async Task Start_DoesNotCreateDirectoryIfExists()
    {
        var processRunner = new MockProcessRunner();
        processRunner.ProcessHandleToReturn = new MockProcessHandle { HasExited = false };

        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/tmp/coverage");

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();

        await Assert.That(fileSystem.CreateDirectoryCalls).IsEqualTo(0);
    }

    [Test]
    public async Task Start_CallsStartProcessWithCorrectFileName()
    {
        var processRunner = new MockProcessRunner();
        processRunner.ProcessHandleToReturn = new MockProcessHandle { HasExited = false };

        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();

        await Assert.That(processRunner.LastStartInfo).IsNotNull();
        await Assert.That(processRunner.LastStartInfo!.FileName).IsEqualTo("dotnet-coverage");
    }

    [Test]
    public async Task Start_CallsStartProcessWithCorrectArguments()
    {
        var processRunner = new MockProcessRunner();
        processRunner.ProcessHandleToReturn = new MockProcessHandle { HasExited = false };

        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage", "cobertura");
        server.Start();

        var args = processRunner.LastStartInfo!.ArgumentList;

        await Assert.That(args).Contains("collect");
        await Assert.That(args).Contains("--server-mode");
        await Assert.That(args).Contains("--session-id");
        await Assert.That(args).Contains(server.SessionId);
        await Assert.That(args).Contains("-o");
        await Assert.That(args).Contains(server.CoverageFile);
        await Assert.That(args).Contains("-f");
        await Assert.That(args).Contains("cobertura");
    }

    [Test]
    public async Task Start_SetsProcessStartInfoOptions()
    {
        var processRunner = new MockProcessRunner();
        processRunner.ProcessHandleToReturn = new MockProcessHandle { HasExited = false };

        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();

        var startInfo = processRunner.LastStartInfo!;
        await Assert.That(startInfo.UseShellExecute).IsFalse();
        await Assert.That(startInfo.RedirectStandardOutput).IsTrue();
        await Assert.That(startInfo.RedirectStandardError).IsTrue();
        await Assert.That(startInfo.CreateNoWindow).IsTrue();
    }

    [Test]
    public async Task Start_ReturnsTrueWhenProcessStartsSuccessfully()
    {
        var processRunner = new MockProcessRunner();
        processRunner.ProcessHandleToReturn = new MockProcessHandle { HasExited = false };

        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        var result = server.Start();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Start_ReturnsFalseWhenProcessExitsImmediately()
    {
        var processRunner = new MockProcessRunner();
        processRunner.ProcessHandleToReturn = new MockProcessHandle { HasExited = true };

        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        var result = server.Start();

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Start_ReturnsFalseWhenStartProcessThrows()
    {
        var processRunner = new MockProcessRunner();
        processRunner.ThrowOnStartProcess = true;

        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        var result = server.Start();

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Start_ReturnsExistingStateIfAlreadyStarted()
    {
        var processRunner = new MockProcessRunner();
        var processHandle = new MockProcessHandle { HasExited = false };
        processRunner.ProcessHandleToReturn = processHandle;

        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();

        // Change the handle state
        processHandle.HasExited = true;

        // Second call should return current state, not start again
        var result = server.Start();

        await Assert.That(result).IsFalse();
        await Assert.That(processRunner.StartProcessCalls).IsEqualTo(1);
    }

    #endregion

    #region Shutdown Tests

    [Test]
    public async Task Shutdown_SendsShutdownCommand()
    {
        var processRunner = new MockProcessRunner();
        processRunner.ProcessHandleToReturn = new MockProcessHandle { HasExited = false };
        processRunner.AddRunResult(new ProcessResult("", "", 0));

        var fileSystem = new MockFileSystem();
        fileSystem.SetFileExists("/tmp/coverage/coverage.cobertura.xml", true);

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();
        server.Shutdown();

        await Assert.That(processRunner.RunCalls).HasCount().EqualTo(1);
        var (fileName, args) = processRunner.RunCalls[0];
        await Assert.That(fileName).IsEqualTo("dotnet-coverage");
        await Assert.That(args.First()).IsEqualTo("shutdown");
        await Assert.That(args.Last()).IsEqualTo(server.SessionId);
    }

    [Test]
    public async Task Shutdown_ReturnsFalseWhenNotStarted()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        var result = server.Shutdown();

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Shutdown_ReturnsTrueWhenSuccessfulAndCoverageFileExists()
    {
        var processRunner = new MockProcessRunner();
        processRunner.ProcessHandleToReturn = new MockProcessHandle { HasExited = false };
        processRunner.AddRunResult(new ProcessResult("", "", 0));

        var fileSystem = new MockFileSystem();
        fileSystem.SetFileExists("/tmp/coverage/coverage.cobertura.xml", true);

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();
        var result = server.Shutdown();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Shutdown_ReturnsFalseWhenCoverageFileDoesNotExist()
    {
        var processRunner = new MockProcessRunner();
        processRunner.ProcessHandleToReturn = new MockProcessHandle { HasExited = false };
        processRunner.AddRunResult(new ProcessResult("", "", 0));

        var fileSystem = new MockFileSystem();
        fileSystem.SetFileExists("/tmp/coverage/coverage.cobertura.xml", false);

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();
        var result = server.Shutdown();

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Shutdown_WaitsForProcessToExit()
    {
        var processRunner = new MockProcessRunner();
        var processHandle = new MockProcessHandle { HasExited = false };
        processRunner.ProcessHandleToReturn = processHandle;
        processRunner.AddRunResult(new ProcessResult("", "", 0));

        var fileSystem = new MockFileSystem();
        fileSystem.SetFileExists("/tmp/coverage/coverage.cobertura.xml", true);

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();
        server.Shutdown();

        await Assert.That(processHandle.WaitForExitCalled).IsTrue();
    }

    #endregion

    #region Dispose Tests

    [Test]
    public async Task Dispose_CallsShutdownIfServerWasStarted()
    {
        var processRunner = new MockProcessRunner();
        var processHandle = new MockProcessHandle { HasExited = false };
        processRunner.ProcessHandleToReturn = processHandle;
        processRunner.AddRunResult(new ProcessResult("", "", 0));

        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();
        server.Dispose();

        // Verify shutdown was called (which calls Run with shutdown command)
        await Assert.That(processRunner.RunCalls).HasCount().EqualTo(1);
    }

    [Test]
    public async Task Dispose_DoesNotCallShutdownIfNotStarted()
    {
        var processRunner = new MockProcessRunner();
        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Dispose();

        await Assert.That(processRunner.RunCalls).IsEmpty();
    }

    [Test]
    public async Task Dispose_KillsProcessIfStillRunning()
    {
        var processRunner = new MockProcessRunner();
        var processHandle = new MockProcessHandle { HasExited = false };
        processRunner.ProcessHandleToReturn = processHandle;
        processRunner.AddRunResult(new ProcessResult("", "", 0));

        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();
        server.Dispose();

        await Assert.That(processHandle.KillCalled).IsTrue();
    }

    [Test]
    public async Task Dispose_DisposesProcessHandle()
    {
        var processRunner = new MockProcessRunner();
        var processHandle = new MockProcessHandle { HasExited = false };
        processRunner.ProcessHandleToReturn = processHandle;
        processRunner.AddRunResult(new ProcessResult("", "", 0));

        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();
        server.Dispose();

        await Assert.That(processHandle.DisposeCalled).IsTrue();
    }

    [Test]
    public async Task Dispose_IsIdempotent()
    {
        var processRunner = new MockProcessRunner();
        var processHandle = new MockProcessHandle { HasExited = false };
        processRunner.ProcessHandleToReturn = processHandle;
        processRunner.AddRunResult(new ProcessResult("", "", 0));
        processRunner.AddRunResult(new ProcessResult("", "", 0));

        var fileSystem = new MockFileSystem();

        var server = new DotnetCoverageServer(processRunner, fileSystem, "/tmp/coverage");
        server.Start();
        server.Dispose();
        server.Dispose(); // Second dispose should be no-op

        // Only one shutdown call should have been made
        await Assert.That(processRunner.RunCalls).HasCount().EqualTo(1);
    }

    #endregion
}

#region Mock Implementations

/// <summary>
/// Mock process runner for testing DotnetCoverageServer.
/// </summary>
file class MockProcessRunner : IProcessRunner
{
    private readonly Queue<ProcessResult> _runResults = new();

    public ProcessStartInfo? LastStartInfo { get; private set; }
    public IProcessHandle? ProcessHandleToReturn { get; set; }
    public bool ThrowOnStartProcess { get; set; }
    public int StartProcessCalls { get; private set; }
    public List<(string FileName, IEnumerable<string> Args)> RunCalls { get; } = [];

    public void AddRunResult(ProcessResult result) => _runResults.Enqueue(result);

    public ProcessResult Run(
        string fileName,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null)
    {
        RunCalls.Add((fileName, arguments.ToList()));
        return _runResults.Count > 0 ? _runResults.Dequeue() : new ProcessResult("", "", 0);
    }

    public ProcessResult RunDotnet(
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null)
    {
        return _runResults.Count > 0 ? _runResults.Dequeue() : new ProcessResult("", "", 0);
    }

    public IProcessHandle StartProcess(ProcessStartInfo startInfo)
    {
        StartProcessCalls++;
        LastStartInfo = startInfo;

        if (ThrowOnStartProcess)
            throw new InvalidOperationException("Process start failed");

        return ProcessHandleToReturn ?? new MockProcessHandle();
    }
}

/// <summary>
/// Mock process handle for testing.
/// </summary>
file class MockProcessHandle : IProcessHandle
{
    public bool HasExited { get; set; }
    public bool WaitForExitCalled { get; private set; }
    public bool KillCalled { get; private set; }
    public bool DisposeCalled { get; private set; }

    public bool WaitForExit(int milliseconds)
    {
        WaitForExitCalled = true;
        return true;
    }

    public void Kill()
    {
        KillCalled = true;
        HasExited = true;
    }

    public void Dispose()
    {
        DisposeCalled = true;
    }
}

/// <summary>
/// Mock file system for testing DotnetCoverageServer.
/// </summary>
file class MockFileSystem : IFileSystem
{
    private readonly HashSet<string> _existingDirectories = new();
    private readonly Dictionary<string, bool> _fileExistsResults = new();

    public int CreateDirectoryCalls { get; private set; }

    public void AddDirectory(string directory)
    {
        _existingDirectories.Add(directory);
    }

    public void SetFileExists(string path, bool exists)
    {
        _fileExistsResults[path] = exists;
    }

    public bool FileExists(string path)
    {
        return _fileExistsResults.TryGetValue(path, out var exists) && exists;
    }

    public void WriteAllText(string path, string content) { }

    public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
        => Task.CompletedTask;

    public string ReadAllText(string path) => "";

    public bool DirectoryExists(string path) => _existingDirectories.Contains(path);

    public void CreateDirectory(string path)
    {
        CreateDirectoryCalls++;
        _existingDirectories.Add(path);
    }

    public string[] GetFiles(string path, string searchPattern) => [];

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => [];

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        => [];

    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern) => [];

    public DateTime GetLastWriteTimeUtc(string path) => DateTime.MinValue;
}

#endregion
