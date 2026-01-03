using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Analyzes spec coverage of source code methods.
/// Uses the pipeline pattern for consistent behavior with other commands.
/// </summary>
public class CoverageMapCommand : ICommand<CoverageMapOptions>
{
    private readonly Func<CommandContext, CancellationToken, Task<int>> _pipeline;
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public CoverageMapCommand(
        [FromKeyedServices("coverage-map")] Func<CommandContext, CancellationToken, Task<int>> pipeline,
        IConsole console,
        IFileSystem fileSystem)
    {
        _pipeline = pipeline;
        _console = console;
        _fileSystem = fileSystem;
    }

    public Task<int> ExecuteAsync(CoverageMapOptions options, CancellationToken ct = default)
    {
        var context = new CommandContext
        {
            Path = options.SourcePath,
            Console = _console,
            FileSystem = _fileSystem
        };

        // Set coverage-map-specific options in context
        context.Set(ContextKeys.SourcePath, options.SourcePath);
        context.Set(ContextKeys.SpecPath, options.SpecPath);
        context.Set(ContextKeys.CoverageMapFormat, options.Format);
        context.Set(ContextKeys.GapsOnly, options.GapsOnly);
        context.Set(ContextKeys.NamespaceFilter, options.NamespaceFilter);

        return _pipeline(context, ct);
    }
}
