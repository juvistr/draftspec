using DraftSpec.Cli.Options.Enums;

namespace DraftSpec.Cli.Options;

/// <summary>
/// Command-specific options for the 'list' command.
/// Composes FilterOptions for selecting which specs to list.
/// </summary>
public class ListOptions
{
    /// <summary>
    /// Path to spec files or directory.
    /// </summary>
    public string Path { get; set; } = ".";

    /// <summary>
    /// Output format for the list: tree, flat, or json.
    /// </summary>
    public ListFormat Format { get; set; } = ListFormat.Tree;

    /// <summary>
    /// Show line numbers in list output.
    /// </summary>
    public bool ShowLineNumbers { get; set; } = true;

    /// <summary>
    /// Show only focused specs (fit()) in list output.
    /// </summary>
    public bool FocusedOnly { get; set; }

    /// <summary>
    /// Show only pending specs (specs without body) in list output.
    /// </summary>
    public bool PendingOnly { get; set; }

    /// <summary>
    /// Show only skipped specs (xit()) in list output.
    /// </summary>
    public bool SkippedOnly { get; set; }

    /// <summary>
    /// Filter options for selecting which specs to list.
    /// </summary>
    public FilterOptions Filter { get; set; } = new();
}
