using System.Diagnostics;
using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Integration tests for SystemProcessRunner.
/// Uses real process execution for testing.
/// </summary>
public class SystemProcessRunnerTests
{
    #region Run Method

    [Test]
    public async Task Run_EchoCommand_ReturnsOutput()
    {
        var runner = new SystemProcessRunner();
        var (fileName, args) = GetEchoCommand("hello");

        var result = runner.Run(fileName, args);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("hello");
    }

    [Test]
    public async Task Run_WithWorkingDirectory_Succeeds()
    {
        var runner = new SystemProcessRunner();
        var tempDir = Path.GetTempPath();
        var (fileName, args) = GetPwdCommand();

        var result = runner.Run(fileName, args, workingDirectory: tempDir);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        // Output should contain the temp directory path
        await Assert.That(result.Output.Trim().Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Run_WithEnvironmentVariables_PassesVariables()
    {
        var runner = new SystemProcessRunner();
        var envVars = new Dictionary<string, string>
        {
            ["TEST_VAR"] = "test_value_123"
        };
        var (fileName, args) = GetEnvVarCommand("TEST_VAR");

        var result = runner.Run(fileName, args, environmentVariables: envVars);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("test_value_123");
    }

    [Test]
    public async Task Run_FailingCommand_ReturnsNonZeroExitCode()
    {
        var runner = new SystemProcessRunner();
        var (fileName, args) = GetFailCommand();

        var result = runner.Run(fileName, args);

        await Assert.That(result.ExitCode).IsNotEqualTo(0);
    }

    #endregion

    #region RunDotnet Method

    [Test]
    public async Task RunDotnet_VersionCommand_Succeeds()
    {
        var runner = new SystemProcessRunner();

        var result = runner.RunDotnet(["--version"]);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).IsNotNull();
    }

    [Test]
    public async Task RunDotnet_HelpCommand_ReturnsOutput()
    {
        var runner = new SystemProcessRunner();

        var result = runner.RunDotnet(["--help"]);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("Usage");
    }

    #endregion

    #region StartProcess Method

    [Test]
    public async Task StartProcess_ReturnsProcessHandle()
    {
        var runner = new SystemProcessRunner();
        var (fileName, args) = GetEchoCommand("test");

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var handle = runner.StartProcess(startInfo);

        await Assert.That(handle).IsNotNull();
        handle.WaitForExit(5000);
        await Assert.That(handle.HasExited).IsTrue();
    }

    [Test]
    public async Task StartProcess_LongRunningProcess_CanBeKilled()
    {
        var runner = new SystemProcessRunner();
        var (fileName, args) = GetSleepCommand(30);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var handle = runner.StartProcess(startInfo);

        await Task.Delay(50); // Let it start

        handle.Kill();
        handle.WaitForExit(5000);

        await Assert.That(handle.HasExited).IsTrue();
    }

    #endregion

    #region Helper Methods

    private static (string fileName, string[] args) GetEchoCommand(string message)
    {
        if (OperatingSystem.IsWindows())
            return ("cmd.exe", ["/c", $"echo {message}"]);
        return ("/bin/sh", ["-c", $"echo {message}"]);
    }

    private static (string fileName, string[] args) GetPwdCommand()
    {
        if (OperatingSystem.IsWindows())
            return ("cmd.exe", ["/c", "cd"]);
        return ("/bin/sh", ["-c", "pwd"]);
    }

    private static (string fileName, string[] args) GetEnvVarCommand(string varName)
    {
        if (OperatingSystem.IsWindows())
            return ("cmd.exe", ["/c", $"echo %{varName}%"]);
        return ("/bin/sh", ["-c", $"echo ${varName}"]);
    }

    private static (string fileName, string[] args) GetFailCommand()
    {
        if (OperatingSystem.IsWindows())
            return ("cmd.exe", ["/c", "exit 1"]);
        return ("/bin/sh", ["-c", "exit 1"]);
    }

    private static (string fileName, string[] args) GetSleepCommand(int seconds)
    {
        if (OperatingSystem.IsWindows())
            return ("cmd.exe", ["/c", $"timeout /t {seconds} /nobreak"]);
        return ("/bin/sh", ["-c", $"sleep {seconds}"]);
    }

    #endregion
}
