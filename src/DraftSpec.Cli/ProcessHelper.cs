using System.Diagnostics;

namespace DraftSpec.Cli;

public record ProcessResult(string Output, string Error, int ExitCode)
{
    public bool Success => ExitCode == 0;
}

public static class ProcessHelper
{
    /// <summary>
    /// Run a command with arguments and capture output.
    /// Uses ArgumentList for secure argument passing without shell interpretation.
    /// </summary>
    /// <param name="fileName">The executable to run</param>
    /// <param name="arguments">Arguments to pass to the executable</param>
    /// <param name="workingDirectory">Optional working directory</param>
    /// <returns>Process result with output, error, and exit code</returns>
    public static ProcessResult Run(string fileName, IEnumerable<string> arguments, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Use ArgumentList for secure argument passing
        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(output, error, process.ExitCode);
    }

    /// <summary>
    /// Run dotnet with the given arguments.
    /// </summary>
    /// <param name="arguments">Arguments to pass to dotnet</param>
    /// <param name="workingDirectory">Optional working directory</param>
    /// <returns>Process result with output, error, and exit code</returns>
    public static ProcessResult RunDotnet(IEnumerable<string> arguments, string? workingDirectory = null)
    {
        return Run("dotnet", arguments, workingDirectory);
    }
}
