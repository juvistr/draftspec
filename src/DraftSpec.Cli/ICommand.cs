namespace DraftSpec.Cli;

/// <summary>
/// Interface for CLI commands. All commands implement this for uniform invocation.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Execute the command.
    /// </summary>
    /// <param name="options">CLI options parsed from command line</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Exit code: 0 for success, non-zero for failure</returns>
    /// <remarks>
    /// Commands should throw exceptions for unexpected errors (bad config, file not found, etc.)
    /// which will be caught and presented uniformly by Program.cs.
    /// Return non-zero exit codes for expected failures (e.g., test failures).
    /// </remarks>
    Task<int> ExecuteAsync(CliOptions options, CancellationToken ct = default);
}
