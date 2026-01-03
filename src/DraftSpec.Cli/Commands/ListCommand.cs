using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Lists discovered specs without executing them.
/// Uses the pipeline pattern for consistent behavior with other commands.
/// </summary>
public class ListCommand : ICommand<ListOptions>
{
    private readonly Func<CommandContext, CancellationToken, Task<int>> _pipeline;
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public ListCommand(
        [FromKeyedServices("list")] Func<CommandContext, CancellationToken, Task<int>> pipeline,
        IConsole console,
        IFileSystem fileSystem)
    {
        _pipeline = pipeline;
        _console = console;
        _fileSystem = fileSystem;
    }

    public Task<int> ExecuteAsync(ListOptions options, CancellationToken ct = default)
    {
        var context = new CommandContext
        {
            Path = options.Path,
            Console = _console,
            FileSystem = _fileSystem
        };

        // Set list-specific options in context
        context.Set(ContextKeys.ListFormat, options.Format);
        context.Set(ContextKeys.ShowLineNumbers, options.ShowLineNumbers);
        context.Set(ContextKeys.FocusedOnly, options.FocusedOnly);
        context.Set(ContextKeys.PendingOnly, options.PendingOnly);
        context.Set(ContextKeys.SkippedOnly, options.SkippedOnly);
        context.Set(ContextKeys.Filter, options.Filter);

        return _pipeline(context, ct);
    }
}
