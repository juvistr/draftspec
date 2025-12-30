using System.Diagnostics;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Implementation that delegates to System.Diagnostics.Process.
/// </summary>
public class SystemAsyncProcessRunner : IAsyncProcessRunner
{
    public Task<IAsyncProcessHandle> StartAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken = default)
    {
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start process");
        return Task.FromResult<IAsyncProcessHandle>(new SystemAsyncProcessHandle(process));
    }
}

/// <summary>
/// Implementation that wraps System.Diagnostics.Process for async operations.
/// </summary>
internal class SystemAsyncProcessHandle : IAsyncProcessHandle
{
    private readonly Process _process;

    public SystemAsyncProcessHandle(Process process)
    {
        _process = process;
    }

    public StreamReader StandardOutput => _process.StandardOutput;
    public StreamReader StandardError => _process.StandardError;
    public int ExitCode => _process.ExitCode;

    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
        => _process.WaitForExitAsync(cancellationToken);

    public void Kill(bool entireProcessTree = false)
        => _process.Kill(entireProcessTree);

    public ValueTask DisposeAsync()
    {
        _process.Dispose();
        return ValueTask.CompletedTask;
    }
}
