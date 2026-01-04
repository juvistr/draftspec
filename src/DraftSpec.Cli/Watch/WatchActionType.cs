namespace DraftSpec.Cli.Watch;

/// <summary>
/// Type of action to take in response to a file change event.
/// </summary>
public enum WatchActionType
{
    /// <summary>
    /// No changes detected, skip re-running.
    /// </summary>
    Skip,

    /// <summary>
    /// Run all spec files.
    /// </summary>
    RunAll,

    /// <summary>
    /// Run a single spec file (non-incremental mode).
    /// </summary>
    RunFile,

    /// <summary>
    /// Run a single spec file with a filter pattern (incremental mode).
    /// </summary>
    RunFiltered
}
