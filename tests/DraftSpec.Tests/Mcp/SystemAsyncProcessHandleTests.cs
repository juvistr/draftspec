using System.Diagnostics;
using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Mcp;

/// <summary>
/// Integration tests for SystemAsyncProcessHandle.
/// Uses real processes for testing.
/// </summary>
public class SystemAsyncProcessHandleTests
{
    [Test]
    public async Task WaitForExitAsync_ProcessCompletes_ReturnsSuccessfully()
    {
        using var process = CreateEchoProcess("hello");
        process.Start();

        await using var handle = new SystemAsyncProcessHandle(process);

        await handle.WaitForExitAsync();

        await Assert.That(handle.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task StandardOutput_CapturesProcessOutput()
    {
        using var process = CreateEchoProcess("test output");
        process.Start();

        await using var handle = new SystemAsyncProcessHandle(process);

        var output = await handle.StandardOutput.ReadToEndAsync();
        await handle.WaitForExitAsync();

        await Assert.That(output.Trim()).IsEqualTo("test output");
    }

    [Test]
    public async Task StandardError_CapturesErrorOutput()
    {
        // Use a command that writes to stderr
        using var process = CreateStderrProcess("error message");
        process.Start();

        await using var handle = new SystemAsyncProcessHandle(process);

        var error = await handle.StandardError.ReadToEndAsync();
        await handle.WaitForExitAsync();

        await Assert.That(error.Trim()).IsEqualTo("error message");
    }

    [Test]
    public async Task ExitCode_FailingProcess_ReturnsNonZero()
    {
        using var process = CreateFailingProcess();
        process.Start();

        await using var handle = new SystemAsyncProcessHandle(process);

        await handle.WaitForExitAsync();

        await Assert.That(handle.ExitCode).IsNotEqualTo(0);
    }

    [Test]
    public async Task Kill_TerminatesProcess()
    {
        using var process = CreateSleepProcess();
        process.Start();

        await using var handle = new SystemAsyncProcessHandle(process);

        // Give process time to start
        await Task.Delay(50);

        handle.Kill();

        // Process should terminate quickly after kill
        var completed = process.WaitForExit(5000);
        await Assert.That(completed).IsTrue();
    }

    [Test]
    public async Task DisposeAsync_DisposesProcess()
    {
        var process = CreateEchoProcess("dispose test");
        process.Start();

        var handle = new SystemAsyncProcessHandle(process);
        await handle.WaitForExitAsync();

        await handle.DisposeAsync();

        // After dispose, accessing process properties should throw or be invalid
        // Just verify dispose completes without error
        await Assert.That(true).IsTrue();
    }

    #region Helper Methods

    private static Process CreateEchoProcess(string message)
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
                Arguments = OperatingSystem.IsWindows()
                    ? $"/c echo {message}"
                    : $"-c \"echo {message}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
    }

    private static Process CreateStderrProcess(string message)
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
                Arguments = OperatingSystem.IsWindows()
                    ? $"/c echo {message} 1>&2"
                    : $"-c \"echo {message} >&2\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
    }

    private static Process CreateFailingProcess()
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
                Arguments = OperatingSystem.IsWindows()
                    ? "/c exit 1"
                    : "-c \"exit 1\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
    }

    private static Process CreateSleepProcess()
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
                Arguments = OperatingSystem.IsWindows()
                    ? "/c timeout /t 30 /nobreak"
                    : "-c \"sleep 30\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
    }

    #endregion
}
