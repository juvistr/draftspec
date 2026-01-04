namespace DraftSpec.Cli.Watch;

/// <summary>
/// Processes file change events and determines what action to take.
/// </summary>
/// <remarks>
/// This interface acts as a "port" in hexagonal architecture, separating
/// the decision logic (what action to take) from the side effects
/// (running specs, recording state). This makes the decision logic
/// fully testable without requiring actual file system events.
/// </remarks>
public interface IWatchEventProcessor
{
    /// <summary>
    /// Processes a file change event and determines the appropriate action.
    /// </summary>
    /// <param name="change">Information about the changed file.</param>
    /// <param name="allSpecFiles">All known spec files being watched.</param>
    /// <param name="basePath">The base path for the project/spec directory.</param>
    /// <param name="incremental">Whether incremental mode is enabled.</param>
    /// <param name="noCache">Whether caching is disabled.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The action to take in response to the change.</returns>
    Task<WatchAction> ProcessChangeAsync(
        FileChangeInfo change,
        IReadOnlyList<string> allSpecFiles,
        string basePath,
        bool incremental,
        bool noCache,
        CancellationToken ct);
}
