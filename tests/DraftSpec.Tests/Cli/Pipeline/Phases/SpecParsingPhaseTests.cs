using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Common;
using DraftSpec.TestingPlatform;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases;

/// <summary>
/// Tests for <see cref="SpecParsingPhase"/>.
/// </summary>
public class SpecParsingPhaseTests
{
    #region Happy Path Tests

    [Test]
    public async Task ExecuteAsync_ParsesAllFiles_SetsParsedSpecs()
    {
        var mockParser = new MockStaticSpecParser()
            .WithSpecs("/specs/a.spec.csx", CreateSpec("test a"))
            .WithSpecs("/specs/b.spec.csx", CreateSpec("test b"));
        var factory = new MockStaticSpecParserFactory(mockParser);
        var phase = new SpecParsingPhase(factory);
        var context = CreateContextWithSpecFiles("/specs", "/specs/a.spec.csx", "/specs/b.spec.csx");
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => { pipelineCalled = true; return Task.FromResult(0); },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsTrue();
        await Assert.That(mockParser.ParseFileCalls).Count().IsEqualTo(2);

        var parsedSpecs = context.Get<IReadOnlyDictionary<string, StaticParseResult>>(ContextKeys.ParsedSpecs);
        await Assert.That(parsedSpecs).IsNotNull();
        await Assert.That(parsedSpecs!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_CreatesParserWithCorrectBaseDirectory()
    {
        var factory = new MockStaticSpecParserFactory();
        var phase = new SpecParsingPhase(factory);
        var context = CreateContextWithSpecFiles("/my/project", "/my/project/test.spec.csx");

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(factory.CreateCalls).Count().IsEqualTo(1);
        await Assert.That(factory.CreateCalls[0].BaseDirectory).IsEqualTo("/my/project");
        await Assert.That(factory.CreateCalls[0].UseCache).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineResult()
    {
        var factory = new MockStaticSpecParserFactory();
        var phase = new SpecParsingPhase(factory);
        var context = CreateContextWithSpecFiles("/specs", "/specs/test.spec.csx");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    #endregion

    #region Warning Tests

    [Test]
    public async Task ExecuteAsync_ParserReturnsWarnings_WritesWarnings()
    {
        var mockParser = new MockStaticSpecParser()
            .WithWarnings("/specs/test.spec.csx", "Dynamic description detected");
        var factory = new MockStaticSpecParserFactory(mockParser);
        var phase = new SpecParsingPhase(factory);
        var console = new MockConsole();
        var context = CreateContextWithSpecFiles("/specs", "/specs/test.spec.csx", console);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(console.Warnings).Contains("Dynamic description detected");
    }

    [Test]
    public async Task ExecuteAsync_WithWarnings_StillContinuesPipeline()
    {
        var mockParser = new MockStaticSpecParser()
            .WithWarnings("/specs/test.spec.csx", "Some warning");
        var factory = new MockStaticSpecParserFactory(mockParser);
        var phase = new SpecParsingPhase(factory);
        var context = CreateContextWithSpecFiles("/specs", "/specs/test.spec.csx");
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) => { pipelineCalled = true; return Task.FromResult(0); },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsTrue();
    }

    #endregion

    #region Error Tests

    [Test]
    public async Task ExecuteAsync_ProjectPathNotSet_ReturnsError()
    {
        var factory = new MockStaticSpecParserFactory();
        var phase = new SpecParsingPhase(factory);
        var console = new MockConsole();
        var context = CreateContext(console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("ProjectPath not set");
    }

    [Test]
    public async Task ExecuteAsync_SpecFilesNotSet_ReturnsError()
    {
        var factory = new MockStaticSpecParserFactory();
        var phase = new SpecParsingPhase(factory);
        var console = new MockConsole();
        var context = CreateContext(console);
        context.Set<string>(ContextKeys.ProjectPath, "/specs");
        // SpecFiles not set

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("SpecFiles not set");
    }

    [Test]
    public async Task ExecuteAsync_ParserThrowsException_ReturnsError()
    {
        var mockParser = new MockStaticSpecParser()
            .WithException("/specs/bad.spec.csx", new InvalidOperationException("Parse failed"));
        var factory = new MockStaticSpecParserFactory(mockParser);
        var phase = new SpecParsingPhase(factory);
        var console = new MockConsole();
        var context = CreateContextWithSpecFiles("/specs", "/specs/bad.spec.csx", console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("Failed to parse");
        await Assert.That(console.Errors).Contains("Parse failed");
    }

    [Test]
    public async Task ExecuteAsync_OneFileFailsOthersSucceed_ReturnsError()
    {
        var mockParser = new MockStaticSpecParser()
            .WithSpecs("/specs/good.spec.csx", CreateSpec("test"))
            .WithException("/specs/bad.spec.csx", new Exception("Boom"));
        var factory = new MockStaticSpecParserFactory(mockParser);
        var phase = new SpecParsingPhase(factory);
        var context = CreateContextWithSpecFiles("/specs", "/specs/good.spec.csx", "/specs/bad.spec.csx");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        // Both files should have been attempted
        await Assert.That(mockParser.ParseFileCalls).Count().IsEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_ParseError_DoesNotCallPipeline()
    {
        var mockParser = new MockStaticSpecParser()
            .WithException("/specs/bad.spec.csx", new Exception("Boom"));
        var factory = new MockStaticSpecParserFactory(mockParser);
        var phase = new SpecParsingPhase(factory);
        var context = CreateContextWithSpecFiles("/specs", "/specs/bad.spec.csx");
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) => { pipelineCalled = true; return Task.FromResult(0); },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsFalse();
    }

    #endregion

    #region Helper Methods

    private static CommandContext CreateContext(MockConsole? console = null)
    {
        return new CommandContext
        {
            Path = "/test",
            Console = console ?? new MockConsole(),
            FileSystem = new MockFileSystem()
        };
    }

    private static CommandContext CreateContextWithSpecFiles(
        string projectPath,
        params string[] specFiles)
    {
        return CreateContextWithSpecFiles(projectPath, specFiles, null);
    }

    private static CommandContext CreateContextWithSpecFiles(
        string projectPath,
        string specFile,
        MockConsole? console)
    {
        return CreateContextWithSpecFiles(projectPath, [specFile], console);
    }

    private static CommandContext CreateContextWithSpecFiles(
        string projectPath,
        string[] specFiles,
        MockConsole? console)
    {
        var context = CreateContext(console);
        context.Set<string>(ContextKeys.ProjectPath, projectPath);
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, specFiles);
        return context;
    }

    private static StaticSpec CreateSpec(string description)
    {
        return new StaticSpec
        {
            Description = description,
            ContextPath = [],
            LineNumber = 1,
            Type = StaticSpecType.Regular
        };
    }

    #endregion
}
