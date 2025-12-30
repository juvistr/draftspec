namespace DraftSpec.Cli.Pipeline;

/// <summary>
/// Generic command executor that orchestrates the pipeline:
/// 1. Apply configuration defaults (optional)
/// 2. Convert CLI options to command-specific options
/// 3. Execute the command
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TOptions">The command-specific options type.</typeparam>
public class CommandExecutor<TCommand, TOptions> : ICommandExecutor
    where TCommand : ICommand<TOptions>
{
    private readonly TCommand _command;
    private readonly Func<CliOptions, TOptions> _converter;
    private readonly IConfigApplier? _configApplier;

    /// <summary>
    /// Create a command executor with configuration support.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="converter">Function to convert CLI options to command options.</param>
    /// <param name="configApplier">Optional config applier for loading project defaults.</param>
    public CommandExecutor(TCommand command, Func<CliOptions, TOptions> converter, IConfigApplier? configApplier = null)
    {
        _command = command;
        _converter = converter;
        _configApplier = configApplier;
    }

    /// <inheritdoc />
    public async Task<int> ExecuteAsync(CliOptions options, CancellationToken cancellationToken)
    {
        _configApplier?.ApplyConfig(options);
        var commandOptions = _converter(options);
        return await _command.ExecuteAsync(commandOptions, cancellationToken);
    }
}
