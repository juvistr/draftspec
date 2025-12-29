using DraftSpec.Scripting;

namespace DraftSpec.Cli;

/// <summary>
/// Implementation that uses CsxScriptHost for Roslyn-based script execution.
/// </summary>
public class RoslynSpecScriptExecutor : ISpecScriptExecutor
{
    public async Task<SpecContext?> ExecuteAsync(string specFile, string outputDirectory, CancellationToken ct = default)
    {
        var scriptHost = new CsxScriptHost(outputDirectory);
        return await scriptHost.ExecuteAsync(specFile, ct);
    }
}
