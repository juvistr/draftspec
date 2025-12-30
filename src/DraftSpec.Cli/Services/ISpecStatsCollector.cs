namespace DraftSpec.Cli.Services;

/// <summary>
/// Collects pre-run statistics about discovered specs using static parsing.
/// </summary>
public interface ISpecStatsCollector
{
    /// <summary>
    /// Collects statistics from the specified spec files.
    /// </summary>
    /// <param name="specFiles">List of spec file paths.</param>
    /// <param name="projectPath">Base project path for relative paths.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Statistics about discovered specs.</returns>
    Task<SpecStats> CollectAsync(
        IReadOnlyList<string> specFiles,
        string projectPath,
        CancellationToken ct = default);
}
