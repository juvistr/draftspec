using DraftSpec.Cli.Pipeline;

namespace DraftSpec.Cli;

/// <summary>
/// Factory for creating command executors.
/// Delegates to ICommandRegistry for command lookup.
/// </summary>
public class CommandFactory : ICommandFactory
{
    private readonly ICommandRegistry _registry;

    /// <summary>
    /// Create a command factory.
    /// </summary>
    /// <param name="registry">The command registry to use for lookups.</param>
    public CommandFactory(ICommandRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public Func<CliOptions, CancellationToken, Task<int>>? Create(string commandName)
    {
        var executor = _registry.GetExecutor(commandName);
        return executor == null ? null : executor.ExecuteAsync;
    }
}
