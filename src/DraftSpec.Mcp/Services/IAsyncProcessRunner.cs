using System.Diagnostics;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Abstraction for running external processes asynchronously with stream access.
/// Enables deterministic testing of process execution.
/// </summary>
public interface IAsyncProcessRunner
{
    /// <summary>
    /// Start a process and return a handle for lifecycle management.
    /// </summary>
    /// <param name="startInfo">Process start configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Handle to the running process.</returns>
    Task<IAsyncProcessHandle> StartAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken = default);
}
