using System.Diagnostics;

namespace DraftSpec.Cli;

/// <summary>
/// Handle to a running process for lifecycle management.
/// </summary>
public interface IProcessHandle : IDisposable
{
    /// <summary>
    /// Whether the process has exited.
    /// </summary>
    bool HasExited { get; }

    /// <summary>
    /// Wait for the process to exit.
    /// </summary>
    /// <param name="milliseconds">Maximum time to wait.</param>
    /// <returns>True if the process exited within the timeout.</returns>
    bool WaitForExit(int milliseconds);

    /// <summary>
    /// Kill the process.
    /// </summary>
    void Kill();
}
