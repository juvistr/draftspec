using System.Diagnostics;

namespace DraftSpec.Cli;

public record ProcessResult(string Output, string Error, int ExitCode)
{
    public bool Success => ExitCode == 0;
}
