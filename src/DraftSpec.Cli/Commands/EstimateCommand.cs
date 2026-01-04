using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Estimates runtime based on historical execution data.
/// Uses the pipeline pattern for consistent behavior with other commands.
/// </summary>
public class EstimateCommand : ICommand<EstimateOptions>
{
    private readonly Func<CommandContext, CancellationToken, Task<int>> _pipeline;
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public EstimateCommand(
        [FromKeyedServices("estimate")] Func<CommandContext, CancellationToken, Task<int>> pipeline,
        IConsole console,
        IFileSystem fileSystem)
    {
        _pipeline = pipeline;
        _console = console;
        _fileSystem = fileSystem;
    }

    public Task<int> ExecuteAsync(EstimateOptions options, CancellationToken ct = default)
    {
        var context = new CommandContext
        {
            Path = options.Path,
            Console = _console,
            FileSystem = _fileSystem
        };

        // Set estimate-specific options in context
        context.Set(ContextKeys.Percentile, options.Percentile);
        context.Set(ContextKeys.OutputSeconds, options.OutputSeconds);

        return _pipeline(context, ct);
    }
}
