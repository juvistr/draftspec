namespace DraftSpec.Cli.Pipeline;

/// <summary>
/// Registry for command executors.
/// Allows commands to be registered by name and retrieved for execution.
/// </summary>
public interface ICommandRegistry
{
    /// <summary>
    /// Register a command with the registry.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TOptions">The command-specific options type.</typeparam>
    /// <param name="name">The command name (e.g., "run", "watch").</param>
    /// <param name="commandFactory">Factory function to create the command.</param>
    /// <param name="optionsConverter">Function to convert CLI options to command options.</param>
    void Register<TCommand, TOptions>(
        string name,
        Func<TCommand> commandFactory,
        Func<CliOptions, TOptions> optionsConverter)
        where TCommand : ICommand<TOptions>;

    /// <summary>
    /// Get an executor for the named command.
    /// </summary>
    /// <param name="name">The command name.</param>
    /// <returns>The command executor, or null if not found.</returns>
    ICommandExecutor? GetExecutor(string name);

    /// <summary>
    /// Get all registered command names.
    /// </summary>
    IEnumerable<string> RegisteredCommands { get; }
}
