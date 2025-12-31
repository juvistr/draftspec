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
