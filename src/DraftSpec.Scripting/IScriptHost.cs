namespace DraftSpec.Scripting;

/// <summary>
/// Abstraction for executing CSX spec files.
/// Enables deterministic testing of discovery and execution logic.
/// </summary>
public interface IScriptHost
{
    /// <summary>
    /// Executes a CSX spec file and returns the root spec context.
    /// </summary>
    /// <param name="csxFilePath">Path to the CSX spec file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The root SpecContext containing the spec tree, or null if no specs defined.</returns>
    Task<SpecContext?> ExecuteAsync(
        string csxFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the DSL state for isolation between executions.
    /// </summary>
    void Reset();
}
