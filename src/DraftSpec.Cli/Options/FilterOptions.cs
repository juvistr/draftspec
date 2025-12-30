namespace DraftSpec.Cli.Options;

/// <summary>
/// Composable options for filtering which specs to run.
/// Used by run, list, and watch commands.
/// </summary>
public class FilterOptions
{
    /// <summary>
    /// Specific spec name to run.
    /// </summary>
    public string? SpecName { get; set; }

    /// <summary>
    /// Comma-separated list of tags to include.
    /// Only specs with any of these tags will run.
    /// </summary>
    public string? FilterTags { get; set; }

    /// <summary>
    /// Comma-separated list of tags to exclude.
    /// Specs with any of these tags will be skipped.
    /// </summary>
    public string? ExcludeTags { get; set; }

    /// <summary>
    /// Regex pattern to match spec names (context path + description).
    /// Only specs matching this pattern will run.
    /// </summary>
    public string? FilterName { get; set; }

    /// <summary>
    /// Regex pattern to exclude spec names (context path + description).
    /// Specs matching this pattern will be skipped.
    /// </summary>
    public string? ExcludeName { get; set; }

    /// <summary>
    /// Context patterns to include (glob-style with / separator).
    /// Only specs within matching contexts will run.
    /// Supports * (single segment) and ** (multiple segments).
    /// Example: "UserService/CreateAsync", "*/CreateAsync", "Integration/**"
    /// </summary>
    public List<string>? FilterContext { get; set; }

    /// <summary>
    /// Context patterns to exclude (glob-style with / separator).
    /// Specs within matching contexts will be skipped.
    /// Supports * (single segment) and ** (multiple segments).
    /// Example: "Legacy/*", "**/Slow"
    /// </summary>
    public List<string>? ExcludeContext { get; set; }

    /// <summary>
    /// Line number filters parsed from file:line syntax.
    /// Used to run specific specs by line number (e.g., "file.spec.csx:15,23").
    /// </summary>
    public List<LineFilter>? LineFilters { get; set; }

    /// <summary>
    /// Returns true if any filter is active.
    /// </summary>
    public bool HasActiveFilters =>
        !string.IsNullOrEmpty(SpecName) ||
        !string.IsNullOrEmpty(FilterTags) ||
        !string.IsNullOrEmpty(ExcludeTags) ||
        !string.IsNullOrEmpty(FilterName) ||
        !string.IsNullOrEmpty(ExcludeName) ||
        FilterContext is { Count: > 0 } ||
        ExcludeContext is { Count: > 0 } ||
        LineFilters is { Count: > 0 };
}
