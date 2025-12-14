using System.Diagnostics;

namespace DraftSpec.Cli;

public record ProcessResult(string Output, string Error, int ExitCode)
{
    public bool Success => ExitCode == 0;
}

public static class ProcessHelper
{
    /// <summary>
    /// Run a command and capture output.
    /// </summary>
    public static ProcessResult Run(string fileName, string arguments, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(output, error, process.ExitCode);
    }

    /// <summary>
    /// Run dotnet with the given arguments.
    /// </summary>
    public static ProcessResult RunDotnet(string arguments, string? workingDirectory = null)
    {
        return Run("dotnet", arguments, workingDirectory);
    }
}
