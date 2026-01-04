using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Lists detected flaky specs based on execution history.
/// Uses the pipeline pattern for consistent behavior with other commands.
/// </summary>
public class FlakyCommand : ICommand<FlakyOptions>
{
    private readonly Func<CommandContext, CancellationToken, Task<int>> _pipeline;
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public FlakyCommand(
        [FromKeyedServices("flaky")] Func<CommandContext, CancellationToken, Task<int>> pipeline,
        IConsole console,
        IFileSystem fileSystem)
    {
        _pipeline = pipeline;
        _console = console;
        _fileSystem = fileSystem;
    }

    public Task<int> ExecuteAsync(FlakyOptions options, CancellationToken ct = default)
    {
        var context = new CommandContext
        {
            Path = options.Path,
            Console = _console,
            FileSystem = _fileSystem
        };

        // Set flaky-specific options in context
        context.Set(ContextKeys.MinStatusChanges, options.MinStatusChanges);
        context.Set(ContextKeys.WindowSize, options.WindowSize);
        context.Set(ContextKeys.Clear, options.Clear);

        return _pipeline(context, ct);
    }
}
