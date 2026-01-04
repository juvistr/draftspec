using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Watch;

/// <summary>
/// Represents the action to take in response to a file change event.
/// </summary>
public sealed record WatchAction
{
    /// <summary>
    /// The type of action to take.
    /// </summary>
    public required WatchActionType Type { get; init; }

    /// <summary>
    /// The spec file path for RunFile and RunFiltered actions.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// The regex filter pattern for RunFiltered actions.
    /// </summary>
    public string? FilterPattern { get; init; }

    /// <summary>
    /// An optional message to display (e.g., reason for action).
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// The parse result to record after a successful run (for incremental mode).
    /// </summary>
    public StaticParseResult? ParseResultToRecord { get; init; }

    /// <summary>
    /// Creates a Skip action indicating no re-run is needed.
    /// </summary>
    /// <param name="message">Optional message explaining why skip was chosen.</param>
    public static WatchAction Skip(string? message = null)
        => new() { Type = WatchActionType.Skip, Message = message };

    /// <summary>
    /// Creates a RunAll action to run all spec files.
    /// </summary>
    public static WatchAction RunAll()
        => new() { Type = WatchActionType.RunAll };

    /// <summary>
    /// Creates a RunFile action to run a single spec file.
    /// </summary>
    /// <param name="filePath">The spec file to run.</param>
    /// <param name="message">Optional message explaining the reason.</param>
    public static WatchAction RunFile(string filePath, string? message = null)
        => new() { Type = WatchActionType.RunFile, FilePath = filePath, Message = message };

    /// <summary>
    /// Creates a RunFiltered action to run specific specs within a file.
    /// </summary>
    /// <param name="filePath">The spec file to run.</param>
    /// <param name="filterPattern">The regex filter pattern for spec selection.</param>
    /// <param name="message">Message describing the action.</param>
    /// <param name="parseResultToRecord">The parse result to record after successful run.</param>
    public static WatchAction RunFiltered(
        string filePath,
        string filterPattern,
        string message,
        StaticParseResult parseResultToRecord)
        => new()
        {
            Type = WatchActionType.RunFiltered,
            FilePath = filePath,
            FilterPattern = filterPattern,
            Message = message,
            ParseResultToRecord = parseResultToRecord
        };
}
