using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Schema;
using DraftSpec.Tests.Infrastructure;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Schema;

/// <summary>
/// Tests for <see cref="SchemaOutputPhase"/>.
/// </summary>
public class SchemaOutputPhaseTests
{
    #region Console Output Tests

    [Test]
    public async Task ExecuteAsync_NoOutputFile_WritesToConsole()
    {
        var console = new MockConsole();
        var phase = new SchemaOutputPhase();
        var context = CreateContext(console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        // JSON schema includes type definitions
        await Assert.That(console.Output).Contains("\"type\"");
    }

    [Test]
    public async Task ExecuteAsync_NoOutputFile_OutputsValidJson()
    {
        var console = new MockConsole();
        var phase = new SchemaOutputPhase();
        var context = CreateContext(console);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        // Should be valid JSON with schema structure
        await Assert.That(console.Output).Contains("{");
        await Assert.That(console.Output).Contains("}");
        await Assert.That(console.Output).Contains("type");
    }

    #endregion

    #region File Output Tests

    [Test]
    public async Task ExecuteAsync_WithOutputFile_WritesToFile()
    {
        var schemaPath = TestPaths.Temp("schema.json");
        var fileSystem = new MockFileSystem();
        var console = new MockConsole();
        var phase = new SchemaOutputPhase();
        var context = CreateContext(console, fileSystem);
        context.Set(ContextKeys.OutputFile, schemaPath);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(fileSystem.WrittenFiles.ContainsKey(schemaPath)).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_WithOutputFile_FileContainsSchema()
    {
        var schemaPath = TestPaths.Temp("schema.json");
        var fileSystem = new MockFileSystem();
        var phase = new SchemaOutputPhase();
        var context = CreateContext(fileSystem: fileSystem);
        context.Set(ContextKeys.OutputFile, schemaPath);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        var content = fileSystem.WrittenFiles[schemaPath];
        // JSON schema includes type definitions and required properties
        await Assert.That(content).Contains("\"type\"");
        await Assert.That(content).Contains("\"required\"");
    }

    [Test]
    public async Task ExecuteAsync_WithOutputFile_ShowsSuccessMessage()
    {
        var schemaPath = TestPaths.Temp("schema.json");
        var fileSystem = new MockFileSystem();
        var console = new MockConsole();
        var phase = new SchemaOutputPhase();
        var context = CreateContext(console, fileSystem);
        context.Set(ContextKeys.OutputFile, schemaPath);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Output).Contains($"Schema written to {schemaPath}");
    }

    #endregion

    #region Schema Content Tests

    [Test]
    public async Task ExecuteAsync_SchemaIncludesSpecsProperty()
    {
        var console = new MockConsole();
        var phase = new SchemaOutputPhase();
        var context = CreateContext(console);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        // ListOutputDto has a 'specs' array property
        await Assert.That(console.Output).Contains("specs");
    }

    [Test]
    public async Task ExecuteAsync_SchemaIncludesSummaryProperty()
    {
        var console = new MockConsole();
        var phase = new SchemaOutputPhase();
        var context = CreateContext(console);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        // ListOutputDto has a 'summary' property
        await Assert.That(console.Output).Contains("summary");
    }

    [Test]
    public async Task ExecuteAsync_SchemaIsPrettyPrinted()
    {
        var console = new MockConsole();
        var phase = new SchemaOutputPhase();
        var context = CreateContext(console);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        // Pretty-printed JSON has indentation (newlines with spaces)
        await Assert.That(console.Output).Contains("\n  ");
    }

    [Test]
    public async Task ExecuteAsync_SchemaUsesCamelCase()
    {
        var console = new MockConsole();
        var phase = new SchemaOutputPhase();
        var context = CreateContext(console);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        // Properties should be camelCase, not PascalCase
        await Assert.That(console.Output).Contains("\"specs\"");
        await Assert.That(console.Output).DoesNotContain("\"Specs\"");
    }

    #endregion

    #region Pipeline Propagation Tests

    [Test]
    public async Task ExecuteAsync_CallsPipeline()
    {
        var phase = new SchemaOutputPhase();
        var context = CreateContext();
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineResult()
    {
        var phase = new SchemaOutputPhase();
        var context = CreateContext();

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    #endregion

    #region Helper Methods

    private static CommandContext CreateContext(MockConsole? console = null, MockFileSystem? fileSystem = null)
    {
        return new CommandContext
        {
            Path = ".",
            Console = console ?? new MockConsole(),
            FileSystem = fileSystem ?? new MockFileSystem()
        };
    }

    #endregion
}
