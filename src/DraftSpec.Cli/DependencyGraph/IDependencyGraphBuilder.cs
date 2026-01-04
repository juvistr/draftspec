namespace DraftSpec.Cli.DependencyGraph;

/// <summary>
/// Builds dependency graphs from spec and source files.
/// </summary>
public interface IDependencyGraphBuilder
{
    /// <summary>
    /// Builds a dependency graph from spec files in the specified directory.
    /// </summary>
    /// <param name="specDirectory">Directory containing .spec.csx files.</param>
    /// <param name="sourceDirectory">Optional directory containing .cs source files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dependency graph representing the relationships between files.</returns>
    Task<DependencyGraph> BuildAsync(
        string specDirectory,
        string? sourceDirectory = null,
        CancellationToken cancellationToken = default);
}
