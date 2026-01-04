using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.Commands;

public class NewCommand : ICommand<NewOptions>
{
    private readonly Func<CommandContext, CancellationToken, Task<int>> _pipeline;
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public NewCommand(
        [FromKeyedServices("new")] Func<CommandContext, CancellationToken, Task<int>> pipeline,
        IConsole console,
        IFileSystem fileSystem)
    {
        _pipeline = pipeline;
        _console = console;
        _fileSystem = fileSystem;
    }

    public Task<int> ExecuteAsync(NewOptions options, CancellationToken ct = default)
    {
        var context = new CommandContext
        {
            Path = options.Path,
            Console = _console,
            FileSystem = _fileSystem
        };

        context.Set(ContextKeys.SpecName, options.SpecName);

        return _pipeline(context, ct);
    }
}
