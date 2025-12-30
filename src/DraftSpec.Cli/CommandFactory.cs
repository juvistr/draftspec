using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli;

/// <summary>
/// Factory for creating command executors from the DI container.
/// Handles conversion from CliOptions to command-specific options.
/// </summary>
public class CommandFactory : ICommandFactory
{
    private readonly IServiceProvider _services;

    public CommandFactory(IServiceProvider services)
    {
        _services = services;
    }

    public Func<CliOptions, CancellationToken, Task<int>>? Create(string commandName)
    {
        return commandName.ToLowerInvariant() switch
        {
            "run" => CreateRunExecutor(),
            "watch" => CreateWatchExecutor(),
            "list" => CreateListExecutor(),
            "validate" => CreateValidateExecutor(),
            "init" => CreateLegacyExecutor<InitCommand>(),
            "new" => CreateLegacyExecutor<NewCommand>(),
            "schema" => CreateLegacyExecutor<SchemaCommand>(),
            _ => null
        };
    }

    private Func<CliOptions, CancellationToken, Task<int>> CreateRunExecutor()
    {
        return async (cliOptions, ct) =>
        {
            // Load and apply config before conversion
            var configLoader = _services.GetRequiredService<IConfigLoader>();
            var configResult = configLoader.Load(cliOptions.Path);
            if (configResult.Error != null)
                throw new InvalidOperationException(configResult.Error);

            if (configResult.Config != null)
                cliOptions.ApplyDefaults(configResult.Config);

            var command = _services.GetRequiredService<RunCommand>();
            var options = cliOptions.ToRunOptions();
            return await command.ExecuteAsync(options, ct);
        };
    }

    private Func<CliOptions, CancellationToken, Task<int>> CreateWatchExecutor()
    {
        return async (cliOptions, ct) =>
        {
            // Load and apply config before conversion
            var configLoader = _services.GetRequiredService<IConfigLoader>();
            var configResult = configLoader.Load(cliOptions.Path);
            if (configResult.Error != null)
                throw new InvalidOperationException(configResult.Error);

            if (configResult.Config != null)
                cliOptions.ApplyDefaults(configResult.Config);

            var command = _services.GetRequiredService<WatchCommand>();
            var options = cliOptions.ToWatchOptions();
            return await command.ExecuteAsync(options, ct);
        };
    }

    private Func<CliOptions, CancellationToken, Task<int>> CreateListExecutor()
    {
        return async (cliOptions, ct) =>
        {
            var command = _services.GetRequiredService<ListCommand>();
            var options = cliOptions.ToListOptions();
            return await command.ExecuteAsync(options, ct);
        };
    }

    private Func<CliOptions, CancellationToken, Task<int>> CreateValidateExecutor()
    {
        return async (cliOptions, ct) =>
        {
            var command = _services.GetRequiredService<ValidateCommand>();
            var options = cliOptions.ToValidateOptions();
            return await command.ExecuteAsync(options, ct);
        };
    }

    /// <summary>
    /// Creates an executor for commands that still use the legacy ICommand interface.
    /// </summary>
    private Func<CliOptions, CancellationToken, Task<int>> CreateLegacyExecutor<TCommand>()
        where TCommand : ICommand<CliOptions>
    {
        return async (cliOptions, ct) =>
        {
            var command = _services.GetRequiredService<TCommand>();
            return await command.ExecuteAsync(cliOptions, ct);
        };
    }
}
