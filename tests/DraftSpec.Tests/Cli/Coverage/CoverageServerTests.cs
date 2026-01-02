using System.Diagnostics;
using DraftSpec.Cli;
using DraftSpec.Cli.Coverage;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Coverage;

/// <summary>
/// Tests for DotnetCoverageServer with mocked dependencies.
/// </summary>
public class CoverageServerTests
{
    private MockProcessRunner _processRunner = null!;
    private MockFileSystem _fileSystem = null!;

    [Before(Test)]
    public void SetUp()
    {
        _processRunner = new MockProcessRunner();
        _fileSystem = new MockFileSystem();
    }

    private DotnetCoverageServer CreateServer(string? format = null) =>
        format is null
            ? new DotnetCoverageServer(_processRunner, _fileSystem, TestPaths.CoverageDir)
            : new DotnetCoverageServer(_processRunner, _fileSystem, TestPaths.CoverageDir, format);

    private void SetupRunningProcess(bool hasExited = false)
    {
        _processRunner.ProcessHandleToReturn = new MockProcessHandle { HasExited = hasExited };
    }

    private void SetupCoverageDirectory() => _fileSystem.AddDirectory(TestPaths.CoverageDir);

    private void SetupCoverageFile() => _fileSystem.AddFile(TestPaths.Coverage("coverage.cobertura.xml"));

    #region Constructor Tests

    [Test]
    public async Task Constructor_SetsSessionId_WithDraftspecPrefix()
    {
        var server = CreateServer();

        await Assert.That(server.SessionId).StartsWith("draftspec-");
        await Assert.That(server.SessionId.Length).IsGreaterThan(20);
    }

    [Test]
    public async Task Constructor_SetsCoverageFile_WithCoberturaExtension()
    {
        var server = CreateServer();

        await Assert.That(server.CoverageFile).IsEqualTo(TestPaths.Coverage("coverage.cobertura.xml"));
    }

    [Test]
    public async Task Constructor_SetsCoverageFile_WithXmlFormat()
    {
        var server = CreateServer("xml");

        await Assert.That(server.CoverageFile).IsEqualTo(TestPaths.Coverage("coverage.xml"));
    }

    [Test]
    public async Task Constructor_SetsCoverageFile_WithCoverageFormat()
    {
        var server = CreateServer("coverage");

        await Assert.That(server.CoverageFile).IsEqualTo(TestPaths.Coverage("coverage.coverage"));
    }

    [Test]
    public async Task Constructor_NormalizesFormatToLowercase()
    {
        var server = CreateServer("COBERTURA");

        await Assert.That(server.CoverageFile).IsEqualTo(TestPaths.Coverage("coverage.cobertura.xml"));
    }

    [Test]
    public async Task Constructor_GeneratesUniqueSessionIds()
    {
        var server1 = CreateServer();
        var server2 = CreateServer();

        await Assert.That(server1.SessionId).IsNotEqualTo(server2.SessionId);
    }

    #endregion

    #region IsRunning Tests

    [Test]
    public async Task IsRunning_ReturnsFalseInitially()
    {
        var server = CreateServer();

        await Assert.That(server.IsRunning).IsFalse();
    }

    [Test]
    public async Task IsRunning_ReturnsTrueWhenServerStartedAndNotExited()
    {
        SetupRunningProcess(hasExited: false);
        SetupCoverageDirectory();

        var server = CreateServer();
        server.Start();

        await Assert.That(server.IsRunning).IsTrue();
    }

    [Test]
    public async Task IsRunning_ReturnsFalseWhenProcessHasExited()
    {
        SetupRunningProcess(hasExited: true);
        SetupCoverageDirectory();

        var server = CreateServer();
        server.Start();

        await Assert.That(server.IsRunning).IsFalse();
    }

    #endregion

    #region Start Tests

    [Test]
    public async Task Start_CreatesOutputDirectoryIfNotExists()
    {
        SetupRunningProcess();

        var server = CreateServer();
        server.Start();

        await Assert.That(_fileSystem.DirectoryExists(TestPaths.CoverageDir)).IsTrue();
    }

    [Test]
    public async Task Start_DoesNotCreateDirectoryIfExists()
    {
        SetupRunningProcess();
        SetupCoverageDirectory();

        var server = CreateServer();
        server.Start();

        await Assert.That(_fileSystem.CreateDirectoryCalls).IsEqualTo(0);
    }

    [Test]
    public async Task Start_CallsStartProcessWithCorrectFileName()
    {
        SetupRunningProcess();

        var server = CreateServer();
        server.Start();

        await Assert.That(_processRunner.LastStartInfo).IsNotNull();
        await Assert.That(_processRunner.LastStartInfo!.FileName).IsEqualTo("dotnet-coverage");
    }

    [Test]
    public async Task Start_CallsStartProcessWithCorrectArguments()
    {
        SetupRunningProcess();

        var server = CreateServer("cobertura");
        server.Start();

        var args = _processRunner.LastStartInfo!.ArgumentList;

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
        SetupRunningProcess();

        var server = CreateServer();
        server.Start();

        var startInfo = _processRunner.LastStartInfo!;
        await Assert.That(startInfo.UseShellExecute).IsFalse();
        await Assert.That(startInfo.RedirectStandardOutput).IsTrue();
        await Assert.That(startInfo.RedirectStandardError).IsTrue();
        await Assert.That(startInfo.CreateNoWindow).IsTrue();
    }

    [Test]
    public async Task Start_ReturnsTrueWhenProcessStartsSuccessfully()
    {
        SetupRunningProcess();

        var server = CreateServer();
        var result = server.Start();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Start_ReturnsFalseWhenProcessExitsImmediately()
    {
        SetupRunningProcess(hasExited: true);

        var server = CreateServer();
        var result = server.Start();

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Start_ReturnsFalseWhenStartProcessThrows()
    {
        _processRunner.ThrowOnStartProcess = true;

        var server = CreateServer();
        var result = server.Start();

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Start_ReturnsExistingStateIfAlreadyStarted()
    {
        var processHandle = new MockProcessHandle { HasExited = false };
        _processRunner.ProcessHandleToReturn = processHandle;

        var server = CreateServer();
        server.Start();

        // Change the handle state
        processHandle.HasExited = true;

        // Second call should return current state, not start again
        var result = server.Start();

        await Assert.That(result).IsFalse();
        await Assert.That(_processRunner.StartProcessCallCount).IsEqualTo(1);
    }

    #endregion

    #region Shutdown Tests

    [Test]
    public async Task Shutdown_SendsShutdownCommand()
    {
        SetupRunningProcess();
        _processRunner.AddRunResult(new ProcessResult("", "", 0));
        SetupCoverageFile();

        var server = CreateServer();
        server.Start();
        server.Shutdown();

        await Assert.That(_processRunner.RunCalls).Count().IsEqualTo(1);
        var (fileName, args) = _processRunner.RunCalls[0];
        await Assert.That(fileName).IsEqualTo("dotnet-coverage");
        await Assert.That(args.First()).IsEqualTo("shutdown");
        await Assert.That(args.Last()).IsEqualTo(server.SessionId);
    }

    [Test]
    public async Task Shutdown_ReturnsFalseWhenNotStarted()
    {
        var server = CreateServer();
        var result = server.Shutdown();

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Shutdown_ReturnsTrueWhenSuccessfulAndCoverageFileExists()
    {
        SetupRunningProcess();
        _processRunner.AddRunResult(new ProcessResult("", "", 0));
        SetupCoverageFile();

        var server = CreateServer();
        server.Start();
        var result = server.Shutdown();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Shutdown_ReturnsFalseWhenCoverageFileDoesNotExist()
    {
        SetupRunningProcess();
        _processRunner.AddRunResult(new ProcessResult("", "", 0));
        // File doesn't exist by default in MockFileSystem

        var server = CreateServer();
        server.Start();
        var result = server.Shutdown();

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Shutdown_WaitsForProcessToExit()
    {
        var processHandle = new MockProcessHandle { HasExited = false };
        _processRunner.ProcessHandleToReturn = processHandle;
        _processRunner.AddRunResult(new ProcessResult("", "", 0));
        SetupCoverageFile();

        var server = CreateServer();
        server.Start();
        server.Shutdown();

        await Assert.That(processHandle.WaitForExitCalled).IsTrue();
    }

    #endregion

    #region Dispose Tests

    [Test]
    public async Task Dispose_CallsShutdownIfServerWasStarted()
    {
        var processHandle = new MockProcessHandle { HasExited = false };
        _processRunner.ProcessHandleToReturn = processHandle;
        _processRunner.AddRunResult(new ProcessResult("", "", 0));

        var server = CreateServer();
        server.Start();
        server.Dispose();

        // Verify shutdown was called (which calls Run with shutdown command)
        await Assert.That(_processRunner.RunCalls).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Dispose_DoesNotCallShutdownIfNotStarted()
    {
        var server = CreateServer();
        server.Dispose();

        await Assert.That(_processRunner.RunCalls).IsEmpty();
    }

    [Test]
    public async Task Dispose_KillsProcessIfStillRunning()
    {
        var processHandle = new MockProcessHandle { HasExited = false };
        _processRunner.ProcessHandleToReturn = processHandle;
        _processRunner.AddRunResult(new ProcessResult("", "", 0));

        var server = CreateServer();
        server.Start();
        server.Dispose();

        await Assert.That(processHandle.KillCalled).IsTrue();
    }

    [Test]
    public async Task Dispose_DisposesProcessHandle()
    {
        var processHandle = new MockProcessHandle { HasExited = false };
        _processRunner.ProcessHandleToReturn = processHandle;
        _processRunner.AddRunResult(new ProcessResult("", "", 0));

        var server = CreateServer();
        server.Start();
        server.Dispose();

        await Assert.That(processHandle.DisposeCalled).IsTrue();
    }

    [Test]
    public async Task Dispose_IsIdempotent()
    {
        var processHandle = new MockProcessHandle { HasExited = false };
        _processRunner.ProcessHandleToReturn = processHandle;
        _processRunner.AddRunResult(new ProcessResult("", "", 0));
        _processRunner.AddRunResult(new ProcessResult("", "", 0));

        var server = CreateServer();
        server.Start();
        server.Dispose();
        server.Dispose(); // Second dispose should be no-op

        // Only one shutdown call should have been made
        await Assert.That(_processRunner.RunCalls).Count().IsEqualTo(1);
    }

    #endregion
}
