using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;

namespace DraftSpec.Cli;

/// <summary>
/// Factory for creating command executors.
/// Uses explicit factory functions for testability (no IServiceProvider coupling).
/// </summary>
public class CommandFactory : ICommandFactory
{
    private readonly IConfigApplier _configApplier;
    private readonly Func<RunCommand> _runFactory;
    private readonly Func<WatchCommand> _watchFactory;
    private readonly Func<ListCommand> _listFactory;
    private readonly Func<ValidateCommand> _validateFactory;
    private readonly Func<InitCommand> _initFactory;
    private readonly Func<NewCommand> _newFactory;
    private readonly Func<SchemaCommand> _schemaFactory;
    private readonly Func<FlakyCommand> _flakyFactory;

    public CommandFactory(
        IConfigApplier configApplier,
        Func<RunCommand> runFactory,
        Func<WatchCommand> watchFactory,
        Func<ListCommand> listFactory,
        Func<ValidateCommand> validateFactory,
        Func<InitCommand> initFactory,
        Func<NewCommand> newFactory,
        Func<SchemaCommand> schemaFactory,
        Func<FlakyCommand> flakyFactory)
    {
        _configApplier = configApplier;
        _runFactory = runFactory;
        _watchFactory = watchFactory;
        _listFactory = listFactory;
        _validateFactory = validateFactory;
        _initFactory = initFactory;
        _newFactory = newFactory;
        _schemaFactory = schemaFactory;
        _flakyFactory = flakyFactory;
    }

    public Func<CliOptions, CancellationToken, Task<int>>? Create(string commandName)
    {
        var executor = CreateExecutor(commandName);
        return executor == null ? null : executor.ExecuteAsync;
    }

    private ICommandExecutor? CreateExecutor(string commandName) =>
        commandName.ToLowerInvariant() switch
        {
            "run" => new CommandExecutor<RunCommand, RunOptions>(
                _runFactory(), o => o.ToRunOptions(), _configApplier),
            "watch" => new CommandExecutor<WatchCommand, WatchOptions>(
                _watchFactory(), o => o.ToWatchOptions(), _configApplier),
            "list" => new CommandExecutor<ListCommand, ListOptions>(
                _listFactory(), o => o.ToListOptions(), _configApplier),
            "validate" => new CommandExecutor<ValidateCommand, ValidateOptions>(
                _validateFactory(), o => o.ToValidateOptions(), _configApplier),
            "init" => new CommandExecutor<InitCommand, InitOptions>(
                _initFactory(), o => o.ToInitOptions(), _configApplier),
            "new" => new CommandExecutor<NewCommand, NewOptions>(
                _newFactory(), o => o.ToNewOptions(), _configApplier),
            "schema" => new CommandExecutor<SchemaCommand, SchemaOptions>(
                _schemaFactory(), o => o.ToSchemaOptions(), _configApplier),
            "flaky" => new CommandExecutor<FlakyCommand, FlakyOptions>(
                _flakyFactory(), o => o.ToFlakyOptions(), _configApplier),
            _ => null
        };
}
