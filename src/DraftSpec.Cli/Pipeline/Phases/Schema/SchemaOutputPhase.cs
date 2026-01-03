using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using DraftSpec.Cli.Formatters;

namespace DraftSpec.Cli.Pipeline.Phases.Schema;

/// <summary>
/// Generates and outputs the JSON schema for the list command output.
/// </summary>
/// <remarks>
/// <para><b>Reads:</b> <c>Items[OutputFile]</c> - optional file path to write schema</para>
/// <para><b>Note:</b> No preconditions - schema is generated in-memory</para>
/// </remarks>
public class SchemaOutputPhase : ICommandPhase
{
    /// <inheritdoc />
    public async Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
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
        var outputFile = context.Get<string>(ContextKeys.OutputFile);

        if (!string.IsNullOrEmpty(outputFile))
        {
            await context.FileSystem.WriteAllTextAsync(outputFile, schema, ct);
            context.Console.WriteLine($"Schema written to {outputFile}");
        }
        else
        {
            context.Console.WriteLine(schema);
        }

        return await pipeline(context, ct);
    }
}
