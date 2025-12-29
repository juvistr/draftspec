using System.Diagnostics;

namespace DraftSpec.Cli;

/// <summary>
/// Abstraction for running external processes, enabling deterministic testing.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Run a command with arguments and capture output.
    /// </summary>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="arguments">Arguments to pass to the executable.</param>
    /// <param name="workingDirectory">Optional working directory.</param>
    /// <param name="environmentVariables">Optional environment variables.</param>
    /// <returns>Process result with output, error, and exit code.</returns>
    ProcessResult Run(
        string fileName,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null);

    /// <summary>
    /// Run dotnet with the given arguments.
    /// </summary>
    /// <param name="arguments">Arguments to pass to dotnet.</param>
    /// <param name="workingDirectory">Optional working directory.</param>
    /// <param name="environmentVariables">Optional environment variables.</param>
    /// <returns>Process result with output, error, and exit code.</returns>
    ProcessResult RunDotnet(
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null);

    /// <summary>
    /// Start a long-running process and return a handle for lifecycle management.
    /// </summary>
    /// <param name="startInfo">Process start configuration.</param>
    /// <returns>Handle to the running process.</returns>
    IProcessHandle StartProcess(ProcessStartInfo startInfo);
}

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

/// <summary>
/// Implementation that delegates to ProcessHelper and System.Diagnostics.Process.
/// </summary>
public class SystemProcessRunner : IProcessRunner
{
    public ProcessResult Run(
        string fileName,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null)
    {
        return ProcessHelper.Run(fileName, arguments, workingDirectory, environmentVariables);
    }

    public ProcessResult RunDotnet(
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null)
    {
        return ProcessHelper.RunDotnet(arguments, workingDirectory, environmentVariables);
    }

    public IProcessHandle StartProcess(ProcessStartInfo startInfo)
    {
        var process = Process.Start(startInfo);
        return new SystemProcessHandle(process!);
    }
}

/// <summary>
/// Implementation that wraps System.Diagnostics.Process.
/// </summary>
public class SystemProcessHandle : IProcessHandle
{
    private readonly Process _process;

    public SystemProcessHandle(Process process)
    {
        _process = process;
    }

    public bool HasExited => _process.HasExited;

    public bool WaitForExit(int milliseconds) => _process.WaitForExit(milliseconds);

    public void Kill() => _process.Kill();

    public void Dispose() => _process.Dispose();
}
