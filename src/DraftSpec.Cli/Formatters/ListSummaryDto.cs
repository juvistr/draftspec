namespace DraftSpec.Cli.Formatters;

/// <summary>
/// Summary statistics for the discovery run.
/// </summary>
public sealed class ListSummaryDto
{
    /// <summary>
    /// Total number of specs discovered.
    /// </summary>
    public required int TotalSpecs { get; init; }

    /// <summary>
    /// Number of focused specs (fit).
    /// </summary>
    public required int FocusedCount { get; init; }

    /// <summary>
    /// Number of skipped specs (xit).
    /// </summary>
    public required int SkippedCount { get; init; }

    /// <summary>
    /// Number of pending specs (no body).
    /// </summary>
    public required int PendingCount { get; init; }

    /// <summary>
    /// Number of specs with compilation errors.
    /// </summary>
    public required int ErrorCount { get; init; }

    /// <summary>
    /// Total number of spec files discovered.
    /// </summary>
    public required int TotalFiles { get; init; }

    /// <summary>
    /// Number of files that failed to parse.
    /// </summary>
    public required int FilesWithErrors { get; init; }
}
