using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for SchemaCommand.
/// </summary>
public class SchemaCommandTests
{
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;

    [Before(Test)]
    public void SetUp()
    {
        _console = new MockConsole();
        _fileSystem = new MockFileSystem();
    }

    private SchemaCommand CreateCommand() => new(_console, _fileSystem);

    #region Basic Execution

    [Test]
    public async Task ExecuteAsync_NoOutputFile_WritesToConsole()
    {
        var command = CreateCommand();
        var options = new SchemaOptions();

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        // JSON schema includes type definitions
        await Assert.That(_console.Output).Contains("\"type\"");
    }

    [Test]
    public async Task ExecuteAsync_NoOutputFile_OutputsValidJson()
    {
        var command = CreateCommand();
        var options = new SchemaOptions();

        await command.ExecuteAsync(options);

        // Should be valid JSON with schema structure
        await Assert.That(_console.Output).Contains("{");
        await Assert.That(_console.Output).Contains("}");
        await Assert.That(_console.Output).Contains("type");
    }

    #endregion

    #region Output to File

    [Test]
    public async Task ExecuteAsync_WithOutputFile_WritesToFile()
    {
        var command = CreateCommand();
        var options = new SchemaOptions { OutputFile = "/tmp/schema.json" };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_fileSystem.WrittenFiles.ContainsKey("/tmp/schema.json")).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_WithOutputFile_FileContainsSchema()
    {
        var command = CreateCommand();
        var options = new SchemaOptions { OutputFile = "/tmp/schema.json" };

        await command.ExecuteAsync(options);

        var content = _fileSystem.WrittenFiles["/tmp/schema.json"];
        // JSON schema includes type definitions and required properties
        await Assert.That(content).Contains("\"type\"");
        await Assert.That(content).Contains("\"required\"");
    }

    [Test]
    public async Task ExecuteAsync_WithOutputFile_ShowsSuccessMessage()
    {
        var command = CreateCommand();
        var options = new SchemaOptions { OutputFile = "/tmp/schema.json" };

        await command.ExecuteAsync(options);

        await Assert.That(_console.Output).Contains("Schema written to /tmp/schema.json");
    }

    #endregion

    #region Schema Content

    [Test]
    public async Task ExecuteAsync_SchemaIncludesSpecsProperty()
    {
        var command = CreateCommand();
        var options = new SchemaOptions();

        await command.ExecuteAsync(options);

        // ListOutputDto has a 'specs' array property
        await Assert.That(_console.Output).Contains("specs");
    }

    [Test]
    public async Task ExecuteAsync_SchemaIncludesSummaryProperty()
    {
        var command = CreateCommand();
        var options = new SchemaOptions();

        await command.ExecuteAsync(options);

        // ListOutputDto has a 'summary' property
        await Assert.That(_console.Output).Contains("summary");
    }

    [Test]
    public async Task ExecuteAsync_SchemaIsPrettyPrinted()
    {
        var command = CreateCommand();
        var options = new SchemaOptions();

        await command.ExecuteAsync(options);

        // Pretty-printed JSON has indentation (newlines with spaces)
        await Assert.That(_console.Output).Contains("\n  ");
    }

    [Test]
    public async Task ExecuteAsync_SchemaUsesCamelCase()
    {
        var command = CreateCommand();
        var options = new SchemaOptions();

        await command.ExecuteAsync(options);

        // Properties should be camelCase, not PascalCase
        await Assert.That(_console.Output).Contains("\"specs\"");
        await Assert.That(_console.Output).DoesNotContain("\"Specs\"");
    }

    #endregion
}
