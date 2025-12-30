namespace DraftSpec.Cli;

/// <summary>
/// Factory for creating command executors from the DI container.
/// </summary>
public interface ICommandFactory
{
    /// <summary>
    /// Create a command executor by name.
    /// </summary>
    /// <param name="commandName">The name of the command (e.g., "run", "watch", "list")</param>
    /// <returns>A function that executes the command with CliOptions, or null if the command is not found</returns>
    Func<CliOptions, CancellationToken, Task<int>>? Create(string commandName);
}
