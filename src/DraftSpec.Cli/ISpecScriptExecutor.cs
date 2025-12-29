using DraftSpec.Scripting;

namespace DraftSpec.Cli;

/// <summary>
/// Executes spec scripts and returns the spec tree.
/// </summary>
public interface ISpecScriptExecutor
{
    /// <summary>
    /// Execute a spec file and return the root context.
    /// </summary>
    /// <param name="specFile">Full path to the spec file.</param>
    /// <param name="outputDirectory">Directory containing compiled assemblies.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The root spec context, or null if no specs were defined.</returns>
    Task<SpecContext?> ExecuteAsync(string specFile, string outputDirectory, CancellationToken ct = default);
}

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
