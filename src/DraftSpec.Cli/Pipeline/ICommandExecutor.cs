namespace DraftSpec.Cli.Pipeline;

/// <summary>
/// Executes a CLI command with the full pipeline:
/// config loading, option conversion, and command execution.
/// </summary>
public interface ICommandExecutor
{
    /// <summary>
    /// Execute the command with the given CLI options.
    /// </summary>
    /// <param name="options">The CLI options from argument parsing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code (0 for success).</returns>
    Task<int> ExecuteAsync(CliOptions options, CancellationToken cancellationToken);
}
