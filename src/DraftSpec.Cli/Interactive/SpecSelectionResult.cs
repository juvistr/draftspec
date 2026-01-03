namespace DraftSpec.Cli.Interactive;

/// <summary>
/// Result of an interactive spec selection operation.
/// </summary>
public sealed class SpecSelectionResult
{
    /// <summary>
    /// True if the user cancelled the selection (pressed Escape).
    /// </summary>
    public bool Cancelled { get; init; }

    /// <summary>
    /// Selected spec IDs (used for filtering).
    /// </summary>
    public IReadOnlyList<string> SelectedSpecIds { get; init; } = [];

    /// <summary>
    /// Selected spec display names (for building filter pattern).
    /// </summary>
    public IReadOnlyList<string> SelectedDisplayNames { get; init; } = [];

    /// <summary>
    /// Count of total specs available for selection.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Creates a cancelled result.
    /// </summary>
    public static SpecSelectionResult Cancel() => new() { Cancelled = true };

    /// <summary>
    /// Creates a successful selection result.
    /// </summary>
    public static SpecSelectionResult Success(
        IReadOnlyList<string> specIds,
        IReadOnlyList<string> displayNames,
        int totalCount) => new()
        {
            Cancelled = false,
            SelectedSpecIds = specIds,
            SelectedDisplayNames = displayNames,
            TotalCount = totalCount
        };
}
