namespace DraftSpec.Cli;

/// <summary>
/// Generic interface for CLI commands with type-safe options.
/// </summary>
/// <typeparam name="TOptions">The command-specific options type.</typeparam>
public interface ICommand<in TOptions>
{
    /// <summary>
    /// Execute the command.
    /// </summary>
    /// <param name="options">Command-specific options</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Exit code: 0 for success, non-zero for failure</returns>
    /// <remarks>
    /// Commands should throw exceptions for unexpected errors (bad config, file not found, etc.)
    /// which will be caught and presented uniformly by Program.cs.
    /// Return non-zero exit codes for expected failures (e.g., test failures).
    /// </remarks>
    Task<int> ExecuteAsync(TOptions options, CancellationToken ct = default);
}
