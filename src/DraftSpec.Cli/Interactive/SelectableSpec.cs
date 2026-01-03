using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Interactive;

/// <summary>
/// View model for displaying specs in the interactive selector.
/// Wraps DiscoveredSpec with display-specific properties.
/// </summary>
public sealed class SelectableSpec
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> ContextPath { get; init; }
    public required string RelativeSourceFile { get; init; }
    public required int LineNumber { get; init; }
    public bool IsPending { get; init; }
    public bool IsSkipped { get; init; }
    public bool IsFocused { get; init; }

    /// <summary>
    /// Formatted display string for the selection UI.
    /// Uses Spectre.Console markup for styling.
    /// </summary>
    public string FormattedDisplay => FormatForDisplay();

    private string FormatForDisplay()
    {
        var icon = GetIcon();
        var suffix = GetStatusSuffix();
        return $"{icon} {DisplayName}{suffix}";
    }

    private string GetIcon()
    {
        if (IsFocused) return "[yellow]*[/]";
        if (IsSkipped) return "[dim]-[/]";
        if (IsPending) return "[blue]?[/]";
        return "[green].[/]";
    }

    private string GetStatusSuffix()
    {
        if (IsFocused) return " [yellow](focused)[/]";
        if (IsSkipped) return " [dim](skipped)[/]";
        if (IsPending) return " [blue](pending)[/]";
        return "";
    }

    /// <summary>
    /// Creates a SelectableSpec from a DiscoveredSpec.
    /// </summary>
    public static SelectableSpec FromDiscoveredSpec(DiscoveredSpec spec) => new()
    {
        Id = spec.Id,
        DisplayName = spec.DisplayName,
        Description = spec.Description,
        ContextPath = spec.ContextPath,
        RelativeSourceFile = spec.RelativeSourceFile,
        LineNumber = spec.LineNumber,
        IsPending = spec.IsPending,
        IsSkipped = spec.IsSkipped,
        IsFocused = spec.IsFocused
    };
}
