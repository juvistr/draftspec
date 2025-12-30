using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using DraftSpec.Cli.Formatters;

namespace DraftSpec.Cli.Commands;

/// <summary>
/// Outputs the JSON schema for the `draftspec list --format json` output.
/// Uses .NET's JsonSchemaExporter to generate the schema from DTOs.
/// </summary>
public class SchemaCommand : ICommand<CliOptions>
{
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public SchemaCommand(IConsole console, IFileSystem fileSystem)
    {
        _console = console;
        _fileSystem = fileSystem;
    }

    public async Task<int> ExecuteAsync(CliOptions options, CancellationToken ct = default)
    {
        // Use the same JSON options as JsonListFormatter for consistency
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        var exporterOptions = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true
        };

        // Generate schema from the root DTO type
        var schemaNode = serializerOptions.GetJsonSchemaAsNode(typeof(ListOutputDto), exporterOptions);

        // Pretty-print the schema
        var outputOptions = new JsonSerializerOptions { WriteIndented = true };
        var schema = schemaNode.ToJsonString(outputOptions);

        // Write to file or stdout
        if (!string.IsNullOrEmpty(options.OutputFile))
        {
            await _fileSystem.WriteAllTextAsync(options.OutputFile, schema, ct);
            _console.WriteLine($"Schema written to {options.OutputFile}");
        }
        else
        {
            _console.WriteLine(schema);
        }

        return 0;
    }
}
