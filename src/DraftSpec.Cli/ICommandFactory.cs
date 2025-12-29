namespace DraftSpec.Cli;

/// <summary>
/// Factory for creating command instances from the DI container.
/// </summary>
public interface ICommandFactory
{
    /// <summary>
    /// Create a command instance by name.
    /// </summary>
    /// <param name="commandName">The name of the command (e.g., "run", "watch", "list")</param>
    /// <returns>The command instance, or null if the command is not found</returns>
    ICommand? Create(string commandName);
}
