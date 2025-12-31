using System.Diagnostics;

namespace DraftSpec.Cli;

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
