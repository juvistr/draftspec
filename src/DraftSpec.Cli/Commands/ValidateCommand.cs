using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Validates spec structure without execution.
/// Uses the pipeline pattern for consistent behavior with other commands.
/// </summary>
public class ValidateCommand : ICommand<ValidateOptions>
{
    private readonly Func<CommandContext, CancellationToken, Task<int>> _pipeline;
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public ValidateCommand(
        [FromKeyedServices("validate")] Func<CommandContext, CancellationToken, Task<int>> pipeline,
        IConsole console,
        IFileSystem fileSystem)
    {
        _pipeline = pipeline;
        _console = console;
        _fileSystem = fileSystem;
    }

    public Task<int> ExecuteAsync(ValidateOptions options, CancellationToken ct = default)
    {
        var context = new CommandContext
        {
            Path = options.Path,
            Console = _console,
            FileSystem = _fileSystem
        };

        // Set validate-specific options in context
        context.Set(ContextKeys.ExplicitFiles, options.Files);
        context.Set(ContextKeys.Strict, options.Strict);
        context.Set(ContextKeys.Quiet, options.Quiet);

        return _pipeline(context, ct);
    }
}
