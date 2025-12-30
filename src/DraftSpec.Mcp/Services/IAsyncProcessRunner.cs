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

/// <summary>
/// Handle to a running process for async lifecycle management.
/// </summary>
public interface IAsyncProcessHandle : IAsyncDisposable
{
    /// <summary>
    /// Standard output stream reader.
    /// </summary>
    StreamReader StandardOutput { get; }

    /// <summary>
    /// Standard error stream reader.
    /// </summary>
    StreamReader StandardError { get; }

    /// <summary>
    /// Exit code of the process (only valid after WaitForExitAsync completes).
    /// </summary>
    int ExitCode { get; }

    /// <summary>
    /// Wait for the process to exit asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WaitForExitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Kill the process.
    /// </summary>
    /// <param name="entireProcessTree">Whether to kill the entire process tree.</param>
    void Kill(bool entireProcessTree = false);
}
