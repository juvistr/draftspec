using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Outputs the JSON schema for the `draftspec list --format json` output.
/// Uses .NET's JsonSchemaExporter to generate the schema from DTOs.
/// </summary>
public class SchemaCommand : ICommand<SchemaOptions>
{
    private readonly Func<CommandContext, CancellationToken, Task<int>> _pipeline;
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public SchemaCommand(
        [FromKeyedServices("schema")] Func<CommandContext, CancellationToken, Task<int>> pipeline,
        IConsole console,
        IFileSystem fileSystem)
    {
        _pipeline = pipeline;
        _console = console;
        _fileSystem = fileSystem;
    }

    public Task<int> ExecuteAsync(SchemaOptions options, CancellationToken ct = default)
    {
        var context = new CommandContext
        {
            Path = ".", // Schema command doesn't use path, but required by CommandContext
            Console = _console,
            FileSystem = _fileSystem
        };

        context.Set(ContextKeys.OutputFile, options.OutputFile);

        return _pipeline(context, ct);
    }
}
