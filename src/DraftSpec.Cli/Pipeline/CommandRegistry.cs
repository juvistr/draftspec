namespace DraftSpec.Cli.Pipeline;

/// <summary>
/// Registry for command executors.
/// Stores command registrations and creates executors on demand.
/// </summary>
public class CommandRegistry : ICommandRegistry
{
    private readonly IConfigApplier _configApplier;
    private readonly Dictionary<string, Func<ICommandExecutor>> _executorFactories = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Create a command registry.
    /// </summary>
    /// <param name="configApplier">Config applier to inject into executors.</param>
    public CommandRegistry(IConfigApplier configApplier)
    {
        _configApplier = configApplier ?? throw new ArgumentNullException(nameof(configApplier));
    }

    /// <inheritdoc />
    public void Register<TCommand, TOptions>(
        string name,
        Func<TCommand> commandFactory,
        Func<CliOptions, TOptions> optionsConverter)
        where TCommand : ICommand<TOptions>
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(commandFactory);
        ArgumentNullException.ThrowIfNull(optionsConverter);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Command name cannot be empty.", nameof(name));
        }

        _executorFactories[name] = () =>
        {
            var command = commandFactory();
            return new CommandExecutor<TCommand, TOptions>(command, optionsConverter, _configApplier);
        };
    }

    /// <inheritdoc />
    public ICommandExecutor? GetExecutor(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _executorFactories.TryGetValue(name, out var factory) ? factory() : null;
    }

    /// <inheritdoc />
    public IEnumerable<string> RegisteredCommands => _executorFactories.Keys;
}
