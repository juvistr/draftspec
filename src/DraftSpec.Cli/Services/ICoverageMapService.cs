using DraftSpec.Cli.CoverageMap;

namespace DraftSpec.Cli.Services;

/// <summary>
/// Service for computing coverage mapping between source methods and specs.
/// </summary>
public interface ICoverageMapService
{
    /// <summary>
    /// Computes coverage mapping from source files to spec files.
    /// </summary>
    /// <param name="sourceFiles">List of C# source file paths to analyze.</param>
    /// <param name="specFiles">List of spec file paths to analyze.</param>
    /// <param name="projectPath">Project root path for relative path computation.</param>
    /// <param name="sourcePath">Relative source path for result metadata.</param>
    /// <param name="specPath">Relative spec path for result metadata.</param>
    /// <param name="namespaceFilter">Optional comma-separated namespace prefixes to filter methods.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Coverage map result with all method coverage information.</returns>
    Task<CoverageMapResult> ComputeCoverageAsync(
        IReadOnlyList<string> sourceFiles,
        IReadOnlyList<string> specFiles,
        string projectPath,
        string? sourcePath = null,
        string? specPath = null,
        string? namespaceFilter = null,
        CancellationToken ct = default);
}
