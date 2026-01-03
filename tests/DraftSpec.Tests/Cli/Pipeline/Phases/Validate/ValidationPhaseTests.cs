using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Validate;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Validate;

/// <summary>
/// Tests for <see cref="ValidationPhase"/>.
/// </summary>
public class ValidationPhaseTests
{
    #region Happy Path Tests

    [Test]
    public async Task ExecuteAsync_ValidSpecs_ProducesValidationResults()
    {
        var console = new MockConsole();
        var parserFactory = new MockStaticSpecParserFactory();
        var phase = new ValidationPhase(parserFactory);
        var context = CreateContextWithSpecFiles(console, "/test/project", "/test/project/test.spec.csx");

        var pipelineCalled = false;
        IReadOnlyList<FileValidationResult>? results = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                pipelineCalled = true;
                results = ctx.Get<IReadOnlyList<FileValidationResult>>(ContextKeys.ValidationResults);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsTrue();
        await Assert.That(results).IsNotNull();
        await Assert.That(results!.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_CallsNextPipeline()
    {
        var console = new MockConsole();
        var parserFactory = new MockStaticSpecParserFactory();
        var phase = new ValidationPhase(parserFactory);
        var context = CreateContextWithSpecFiles(console, "/test/project", "/test/project/test.spec.csx");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task ExecuteAsync_CountsSpecsPerFile()
    {
        var console = new MockConsole();
        var parserFactory = new MockStaticSpecParserFactory()
            .WithSpecCount(5);
        var phase = new ValidationPhase(parserFactory);
        var context = CreateContextWithSpecFiles(console, "/test/project", "/test/project/test.spec.csx");

        IReadOnlyList<FileValidationResult>? results = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                results = ctx.Get<IReadOnlyList<FileValidationResult>>(ContextKeys.ValidationResults);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(results![0].SpecCount).IsEqualTo(5);
    }

    #endregion

    #region Warning Categorization Tests

    [Test]
    public async Task ExecuteAsync_DynamicDescription_CategorizedAsWarning()
    {
        var console = new MockConsole();
        var parserFactory = new MockStaticSpecParserFactory()
            .WithWarnings("Line 5: 'it' has dynamic description - cannot analyze statically");
        var phase = new ValidationPhase(parserFactory);
        var context = CreateContextWithSpecFiles(console, "/test/project", "/test/project/test.spec.csx");

        IReadOnlyList<FileValidationResult>? results = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                results = ctx.Get<IReadOnlyList<FileValidationResult>>(ContextKeys.ValidationResults);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(results![0].Warnings.Count).IsEqualTo(1);
        await Assert.That(results[0].Errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_MissingDescription_CategorizedAsError()
    {
        var console = new MockConsole();
        var parserFactory = new MockStaticSpecParserFactory()
            .WithWarnings("Line 10: 'it' missing description argument");
        var phase = new ValidationPhase(parserFactory);
        var context = CreateContextWithSpecFiles(console, "/test/project", "/test/project/test.spec.csx");

        IReadOnlyList<FileValidationResult>? results = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                results = ctx.Get<IReadOnlyList<FileValidationResult>>(ContextKeys.ValidationResults);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(results![0].Errors.Count).IsEqualTo(1);
        await Assert.That(results[0].Warnings.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_EmptyDescription_CategorizedAsError()
    {
        var console = new MockConsole();
        var parserFactory = new MockStaticSpecParserFactory()
            .WithWarnings("Line 8: 'describe' has empty description");
        var phase = new ValidationPhase(parserFactory);
        var context = CreateContextWithSpecFiles(console, "/test/project", "/test/project/test.spec.csx");

        IReadOnlyList<FileValidationResult>? results = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                results = ctx.Get<IReadOnlyList<FileValidationResult>>(ContextKeys.ValidationResults);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(results![0].Errors.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_ParseError_CategorizedAsError()
    {
        var console = new MockConsole();
        var parserFactory = new MockStaticSpecParserFactory()
            .WithWarnings("Line 3: parse error: unexpected token");
        var phase = new ValidationPhase(parserFactory);
        var context = CreateContextWithSpecFiles(console, "/test/project", "/test/project/test.spec.csx");

        IReadOnlyList<FileValidationResult>? results = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                results = ctx.Get<IReadOnlyList<FileValidationResult>>(ContextKeys.ValidationResults);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(results![0].Errors.Count).IsEqualTo(1);
        await Assert.That(results[0].Warnings.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_SyntaxError_CategorizedAsError()
    {
        var console = new MockConsole();
        var parserFactory = new MockStaticSpecParserFactory()
            .WithWarnings("Line 7: syntax error in expression");
        var phase = new ValidationPhase(parserFactory);
        var context = CreateContextWithSpecFiles(console, "/test/project", "/test/project/test.spec.csx");

        IReadOnlyList<FileValidationResult>? results = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                results = ctx.Get<IReadOnlyList<FileValidationResult>>(ContextKeys.ValidationResults);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(results![0].Errors.Count).IsEqualTo(1);
        await Assert.That(results[0].Warnings.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_ParsesLineNumber()
    {
        var console = new MockConsole();
        var parserFactory = new MockStaticSpecParserFactory()
            .WithWarnings("Line 15: warning message here");
        var phase = new ValidationPhase(parserFactory);
        var context = CreateContextWithSpecFiles(console, "/test/project", "/test/project/test.spec.csx");

        IReadOnlyList<FileValidationResult>? results = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                results = ctx.Get<IReadOnlyList<FileValidationResult>>(ContextKeys.ValidationResults);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(results![0].Warnings[0].LineNumber).IsEqualTo(15);
        await Assert.That(results[0].Warnings[0].Message).IsEqualTo("warning message here");
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task ExecuteAsync_ParserThrows_RecordsAsError()
    {
        var console = new MockConsole();
        var parserFactory = new MockStaticSpecParserFactory()
            .ThrowsOnParse(new InvalidOperationException("Script compilation failed"));
        var phase = new ValidationPhase(parserFactory);
        var context = CreateContextWithSpecFiles(console, "/test/project", "/test/project/test.spec.csx");

        IReadOnlyList<FileValidationResult>? results = null;

        await phase.ExecuteAsync(
            context,
            (ctx, _) =>
            {
                results = ctx.Get<IReadOnlyList<FileValidationResult>>(ContextKeys.ValidationResults);
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(results![0].Errors.Count).IsEqualTo(1);
        await Assert.That(results[0].Errors[0].Message).Contains("Parse error");
        await Assert.That(results[0].Errors[0].Message).Contains("Script compilation failed");
    }

    #endregion

    #region Precondition Tests

    [Test]
    public async Task ExecuteAsync_ProjectPathNotSet_ReturnsError()
    {
        var console = new MockConsole();
        var parserFactory = new MockStaticSpecParserFactory();
        var phase = new ValidationPhase(parserFactory);
        var context = CreateContext(console);
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, ["test.spec.csx"]);
        // Don't set ProjectPath

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
        var console = new MockConsole();
        var parserFactory = new MockStaticSpecParserFactory();
        var phase = new ValidationPhase(parserFactory);
        var context = CreateContext(console);
        context.Set(ContextKeys.ProjectPath, "/test/project");
        // Don't set SpecFiles

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("SpecFiles not set");
    }

    [Test]
    public async Task ExecuteAsync_EmptySpecFiles_ReturnsError()
    {
        var console = new MockConsole();
        var parserFactory = new MockStaticSpecParserFactory();
        var phase = new ValidationPhase(parserFactory);
        var context = CreateContext(console);
        context.Set(ContextKeys.ProjectPath, "/test/project");
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, []);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
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
        MockConsole console,
        string projectPath,
        params string[] specFiles)
    {
        var context = CreateContext(console);
        context.Set(ContextKeys.ProjectPath, projectPath);
        context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, specFiles);
        return context;
    }

    #endregion
}
