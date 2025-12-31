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
