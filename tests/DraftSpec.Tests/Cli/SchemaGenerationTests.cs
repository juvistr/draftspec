using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using DraftSpec.Cli.Formatters;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests to ensure the committed JSON schema matches the generated schema from DTOs.
/// This prevents drift between the schema documentation and the actual output format.
/// </summary>
public class SchemaGenerationTests
{
    private static readonly string SchemaPath = Path.Combine(
        GetRepositoryRoot(),
        "docs", "schemas", "list-output.schema.json");

    [Test]
    public async Task GeneratedSchema_MatchesCommittedSchema()
    {
        // Generate schema from DTOs using the same settings as SchemaCommand
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        var exporterOptions = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true
        };

        var schemaNode = serializerOptions.GetJsonSchemaAsNode(typeof(ListOutputDto), exporterOptions);

        var outputOptions = new JsonSerializerOptions { WriteIndented = true };
        var generated = schemaNode.ToJsonString(outputOptions);

        // Read committed schema
        await Assert.That(File.Exists(SchemaPath))
            .IsTrue()
            .Because($"Schema file should exist at {SchemaPath}");

        var committed = await File.ReadAllTextAsync(SchemaPath);

        // Compare - normalize line endings for cross-platform compatibility
        var normalizedGenerated = generated.Replace("\r\n", "\n").Trim();
        var normalizedCommitted = committed.Replace("\r\n", "\n").Trim();

        await Assert.That(normalizedGenerated)
            .IsEqualTo(normalizedCommitted)
            .Because("Generated schema should match committed schema. If DTOs changed, run 'draftspec schema > docs/schemas/list-output.schema.json' to update.");
    }

    [Test]
    public async Task SchemaFile_IsValidJson()
    {
        await Assert.That(File.Exists(SchemaPath))
            .IsTrue()
            .Because($"Schema file should exist at {SchemaPath}");

        var content = await File.ReadAllTextAsync(SchemaPath);

        // Should parse without throwing
        var document = JsonDocument.Parse(content);

        // Should have expected top-level structure
        await Assert.That(document.RootElement.TryGetProperty("type", out var typeElement)).IsTrue();
        await Assert.That(typeElement.GetString()).IsEqualTo("object");

        await Assert.That(document.RootElement.TryGetProperty("properties", out _)).IsTrue();
        await Assert.That(document.RootElement.TryGetProperty("required", out _)).IsTrue();
    }

    private static string GetRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;

        // Walk up until we find the .git directory or solution file
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory, ".git")) ||
                File.Exists(Path.Combine(directory, "DraftSpec.sln")))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory);
        }

        throw new InvalidOperationException("Could not find repository root");
    }
}
