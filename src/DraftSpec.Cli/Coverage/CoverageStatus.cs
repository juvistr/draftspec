namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Coverage status for a line of code.
/// </summary>
public enum CoverageStatus
{
    /// <summary>
    /// Line was executed.
    /// </summary>
    Covered,

    /// <summary>
    /// Line was not executed.
    /// </summary>
    Uncovered,

    /// <summary>
    /// Line was executed but not all branches taken.
    /// </summary>
    Partial
}