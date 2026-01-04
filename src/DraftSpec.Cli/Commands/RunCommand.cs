using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Runs specs and outputs results.
/// Uses the pipeline pattern for consistent behavior with other commands.
/// </summary>
public class RunCommand : ICommand<RunOptions>
{
    private readonly Func<CommandContext, CancellationToken, Task<int>> _pipeline;
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public RunCommand(
        [FromKeyedServices("run")] Func<CommandContext, CancellationToken, Task<int>> pipeline,
        IConsole console,
        IFileSystem fileSystem)
    {
        _pipeline = pipeline;
        _console = console;
        _fileSystem = fileSystem;
    }

    public Task<int> ExecuteAsync(RunOptions options, CancellationToken ct = default)
    {
        var context = new CommandContext
        {
            Path = options.Path,
            Console = _console,
            FileSystem = _fileSystem
        };

        // Run-specific options
        context.Set(ContextKeys.OutputFormat, options.Format.ToCliString());
        context.Set(ContextKeys.OutputFile, options.OutputFile);
        context.Set(ContextKeys.CssUrl, options.CssUrl);
        context.Set(ContextKeys.Parallel, options.Parallel);
        context.Set(ContextKeys.NoCache, options.NoCache);
        context.Set(ContextKeys.NoStats, options.NoStats);
        context.Set(ContextKeys.StatsOnly, options.StatsOnly);

        // Filter options
        context.Set(ContextKeys.Filter, options.Filter);

        // Coverage options
        context.Set(ContextKeys.Coverage, options.Coverage);

        // Partition options
        context.Set(ContextKeys.Partition, options.Partition);

        // Impact analysis options
        context.Set(ContextKeys.AffectedBy, options.AffectedBy);
        context.Set(ContextKeys.DryRun, options.DryRun);

        // Quarantine and history options
        context.Set(ContextKeys.Quarantine, options.Quarantine);
        context.Set(ContextKeys.NoHistory, options.NoHistory);

        // Interactive mode
        context.Set(ContextKeys.Interactive, options.Interactive);

        return _pipeline(context, ct);
    }
}
