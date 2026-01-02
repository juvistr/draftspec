using System.Diagnostics;
using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Integration tests for SystemProcessHandle.
/// Uses real processes for testing.
/// </summary>
public class SystemProcessHandleTests
{
    [Test]
    public async Task HasExited_RunningProcess_ReturnsFalse()
    {
        using var process = CreateSleepProcess();
        process.Start();

        using var handle = new SystemProcessHandle(process);

        await Assert.That(handle.HasExited).IsFalse();

        handle.Kill();
    }

    [Test]
    public async Task HasExited_CompletedProcess_ReturnsTrue()
    {
        using var process = CreateEchoProcess("done");
        process.Start();

        using var handle = new SystemProcessHandle(process);

        handle.WaitForExit(5000);

        await Assert.That(handle.HasExited).IsTrue();
    }

    [Test]
    public async Task WaitForExit_ProcessCompletes_ReturnsTrue()
    {
        using var process = CreateEchoProcess("wait test");
        process.Start();

        using var handle = new SystemProcessHandle(process);

        var completed = handle.WaitForExit(5000);

        await Assert.That(completed).IsTrue();
    }

    [Test]
    public async Task WaitForExit_Timeout_ReturnsFalse()
    {
        using var process = CreateSleepProcess();
        process.Start();

        using var handle = new SystemProcessHandle(process);

        // Give the process time to actually start on slower CI machines
        await Task.Delay(100);

        // Use a longer timeout to avoid race conditions, but still short enough
        // that a 30-second sleep won't complete
        var completed = handle.WaitForExit(200);

        await Assert.That(completed).IsFalse();

        handle.Kill();
    }

    [Test]
    public async Task Kill_TerminatesProcess()
    {
        using var process = CreateSleepProcess();
        process.Start();

        using var handle = new SystemProcessHandle(process);

        // Give process time to start
        await Task.Delay(50);

        handle.Kill();

        var completed = handle.WaitForExit(5000);

        await Assert.That(completed).IsTrue();
        await Assert.That(handle.HasExited).IsTrue();
    }

    [Test]
    public async Task Dispose_DisposesProcess()
    {
        var process = CreateEchoProcess("dispose");
        process.Start();

        var handle = new SystemProcessHandle(process);
        handle.WaitForExit(5000);

        handle.Dispose();

        // Verify dispose completed without error
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Dispose_CanBeCalledMultipleTimes()
    {
        var process = CreateEchoProcess("multi-dispose");
        process.Start();

        var handle = new SystemProcessHandle(process);
        handle.WaitForExit(5000);

        handle.Dispose();
        handle.Dispose(); // Should not throw

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

    private static Process CreateSleepProcess()
    {
        // On Windows, use PowerShell's Start-Sleep which works reliably with redirected IO
        // The cmd.exe 'timeout' command doesn't work properly when console is hidden
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "powershell.exe" : "/bin/sh",
                Arguments = OperatingSystem.IsWindows()
                    ? "-Command \"Start-Sleep -Seconds 30\""
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
