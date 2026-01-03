using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Generates living documentation from spec structure.
/// Uses the pipeline pattern for consistent behavior with other commands.
/// </summary>
public class DocsCommand : ICommand<DocsOptions>
{
    private readonly Func<CommandContext, CancellationToken, Task<int>> _pipeline;
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public DocsCommand(
        [FromKeyedServices("docs")] Func<CommandContext, CancellationToken, Task<int>> pipeline,
        IConsole console,
        IFileSystem fileSystem)
    {
        _pipeline = pipeline;
        _console = console;
        _fileSystem = fileSystem;
    }

    public Task<int> ExecuteAsync(DocsOptions options, CancellationToken ct = default)
    {
        var context = new CommandContext
        {
            Path = options.Path,
            Console = _console,
            FileSystem = _fileSystem
        };

        // Set docs-specific options in context
        context.Set(ContextKeys.DocsFormat, options.Format);
        context.Set(ContextKeys.ContextFilter, options.Context);
        context.Set(ContextKeys.WithResults, options.WithResults);
        context.Set(ContextKeys.ResultsFile, options.ResultsFile);
        context.Set(ContextKeys.Filter, options.Filter);

        return _pipeline(context, ct);
    }
}
