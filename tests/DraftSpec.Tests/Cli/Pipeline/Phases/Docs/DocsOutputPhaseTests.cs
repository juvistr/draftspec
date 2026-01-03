using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Docs;
using DraftSpec.TestingPlatform;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Docs;

/// <summary>
/// Tests for <see cref="DocsOutputPhase"/>.
/// </summary>
public class DocsOutputPhaseTests
{
    #region Markdown Format Tests

    [Test]
    public async Task ExecuteAsync_MarkdownFormat_OutputsMarkdown()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec1", "Feature"));
        context.Set(ContextKeys.DocsFormat, DocsFormat.Markdown);
        context.Set(ContextKeys.ProjectPath, "/test/project");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(999), // Should not be called
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("#"); // Markdown heading
        await Assert.That(console.Output).Contains("Feature");
    }

    [Test]
    public async Task ExecuteAsync_MarkdownFormat_ShowsCheckboxes()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("test spec", "Context"));
        context.Set(ContextKeys.DocsFormat, DocsFormat.Markdown);
        context.Set(ContextKeys.ProjectPath, "/test/project");

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("- [ ]");
    }

    [Test]
    public async Task ExecuteAsync_MarkdownFormat_PendingSpecShowsMarker()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("pending", "Feature", isPending: true));
        context.Set(ContextKeys.DocsFormat, DocsFormat.Markdown);
        context.Set(ContextKeys.ProjectPath, "/test/project");

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("*(pending)*");
    }

    [Test]
    public async Task ExecuteAsync_MarkdownFormat_SkippedSpecShowsMarker()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("skipped", "Feature", isSkipped: true));
        context.Set(ContextKeys.DocsFormat, DocsFormat.Markdown);
        context.Set(ContextKeys.ProjectPath, "/test/project");

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("*(skipped)*");
    }

    #endregion

    #region HTML Format Tests

    [Test]
    public async Task ExecuteAsync_HtmlFormat_OutputsHtml()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec1", "Feature"));
        context.Set(ContextKeys.DocsFormat, DocsFormat.Html);
        context.Set(ContextKeys.ProjectPath, "/test/project");

        var result = await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("<!DOCTYPE html>");
        await Assert.That(console.Output).Contains("</html>");
    }

    [Test]
    public async Task ExecuteAsync_HtmlFormat_ContainsDetails()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec1", "Feature"));
        context.Set(ContextKeys.DocsFormat, DocsFormat.Html);
        context.Set(ContextKeys.ProjectPath, "/test/project");

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("<details");
        await Assert.That(console.Output).Contains("<summary>");
    }

    [Test]
    public async Task ExecuteAsync_HtmlFormat_PendingShowsBadge()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("pending", "Feature", isPending: true));
        context.Set(ContextKeys.DocsFormat, DocsFormat.Html);
        context.Set(ContextKeys.ProjectPath, "/test/project");

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("PENDING");
    }

    #endregion

    #region Default Format Tests

    [Test]
    public async Task ExecuteAsync_NoFormatSet_DefaultsToMarkdown()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec1", "Feature"));
        context.Set(ContextKeys.ProjectPath, "/test/project");
        // Don't set DocsFormat

        var result = await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("#"); // Markdown
    }

    #endregion

    #region Terminal Phase Tests

    [Test]
    public async Task ExecuteAsync_TerminalPhase_DoesNotCallNextPipeline()
    {
        var phase = new DocsOutputPhase();
        var context = CreateContextWithSpecs(new MockConsole(), CreateSpec("spec", "Context"));
        context.Set(ContextKeys.ProjectPath, "/test/project");
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) => { pipelineCalled = true; return Task.FromResult(99); },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsFalse();
    }

    #endregion

    #region Error Tests

    [Test]
    public async Task ExecuteAsync_FilteredSpecsNotSet_ReturnsError()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var context = CreateContext(console);
        context.Set(ContextKeys.ProjectPath, "/test/project");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("FilteredSpecs not set");
    }

    [Test]
    public async Task ExecuteAsync_ProjectPathNotSet_ReturnsError()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec", "Context"));
        // Don't set ProjectPath

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("ProjectPath not set");
    }

    [Test]
    public async Task ExecuteAsync_UnknownFormat_ThrowsArgumentOutOfRange()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec", "Context"));
        context.Set(ContextKeys.ProjectPath, "/test/project");
        context.Set(ContextKeys.DocsFormat, (DocsFormat)999);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None));
    }

    #endregion

    #region With Results Tests

    [Test]
    public async Task ExecuteAsync_WithResultsNoFile_ShowsError()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec", "Context"));
        context.Set(ContextKeys.ProjectPath, "/test/project");
        context.Set(ContextKeys.WithResults, true);
        context.Set<string?>(ContextKeys.ResultsFile, null);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Errors).Contains("--with-results requires --results-file");
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

    private static CommandContext CreateContextWithSpecs(
        MockConsole console,
        params DiscoveredSpec[] specs)
    {
        var context = CreateContext(console);
        context.Set<IReadOnlyList<DiscoveredSpec>>(ContextKeys.FilteredSpecs, specs);
        return context;
    }

    private static DiscoveredSpec CreateSpec(
        string description,
        string contextName,
        bool isPending = false,
        bool isSkipped = false,
        bool isFocused = false)
    {
        return new DiscoveredSpec
        {
            Id = $"test.spec.csx:{contextName}/{description}",
            Description = description,
            DisplayName = $"{contextName} > {description}",
            ContextPath = [contextName],
            SourceFile = "/specs/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 1,
            IsPending = isPending,
            IsSkipped = isSkipped,
            IsFocused = isFocused,
            Tags = []
        };
    }

    #endregion
}
