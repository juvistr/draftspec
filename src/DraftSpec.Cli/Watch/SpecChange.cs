namespace DraftSpec.Cli.Watch;

/// <summary>
/// Represents a single spec change detected between runs.
/// </summary>
/// <param name="Description">The spec description (the it() text).</param>
/// <param name="ContextPath">The describe() blocks containing this spec.</param>
/// <param name="ChangeType">The type of change (Added, Modified, Deleted).</param>
/// <param name="OldLineNumber">The line number before the change (for Modified/Deleted).</param>
/// <param name="NewLineNumber">The line number after the change (for Added/Modified).</param>
public sealed record SpecChange(
    string Description,
    IReadOnlyList<string> ContextPath,
    SpecChangeType ChangeType,
    int? OldLineNumber = null,
    int? NewLineNumber = null);
