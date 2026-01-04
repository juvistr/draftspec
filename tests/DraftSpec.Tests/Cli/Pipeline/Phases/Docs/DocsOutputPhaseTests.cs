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

    [Test]
    public async Task ExecuteAsync_WithResultsFileNotFound_ShowsError()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var fileSystem = new MockFileSystem(); // File doesn't exist
        var context = CreateContextWithSpecs(console, fileSystem, CreateSpec("spec", "Context"));
        context.Set(ContextKeys.ProjectPath, "/test/project");
        context.Set(ContextKeys.WithResults, true);
        context.Set(ContextKeys.ResultsFile, "/missing/results.json");

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Errors).Contains("Results file not found");
    }

    [Test]
    public async Task ExecuteAsync_WithResultsMalformedJson_ShowsError()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var fileSystem = new MockFileSystem()
            .AddFile("/results.json", "{ invalid json }");
        var context = CreateContextWithSpecs(console, fileSystem, CreateSpec("spec", "Context"));
        context.Set(ContextKeys.ProjectPath, "/test/project");
        context.Set(ContextKeys.WithResults, true);
        context.Set(ContextKeys.ResultsFile, "/results.json");

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Errors).Contains("Failed to parse results file");
    }

    [Test]
    public async Task ExecuteAsync_WithResultsValidJson_ShowsCheckedBoxForPassedSpec()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var resultsJson = """
            {
                "timestamp": "2024-01-01T00:00:00Z",
                "summary": { "total": 1, "passed": 1 },
                "contexts": [{
                    "description": "Context",
                    "specs": [{ "description": "spec", "status": "passed" }],
                    "contexts": []
                }]
            }
            """;
        var fileSystem = new MockFileSystem()
            .AddFile("/results.json", resultsJson);
        var context = CreateContextWithSpecs(console, fileSystem, CreateSpec("spec", "Context"));
        context.Set(ContextKeys.ProjectPath, "/test/project");
        context.Set(ContextKeys.DocsFormat, DocsFormat.Markdown);
        context.Set(ContextKeys.WithResults, true);
        context.Set(ContextKeys.ResultsFile, "/results.json");

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        // Should have checked checkbox for passed spec
        await Assert.That(console.Output).Contains("- [x]");
    }

    [Test]
    public async Task ExecuteAsync_WithResultsNestedContexts_FlattensCorrectly()
    {
        var phase = new DocsOutputPhase();
        var console = new MockConsole();
        var resultsJson = """
            {
                "timestamp": "2024-01-01T00:00:00Z",
                "summary": { "total": 2, "passed": 1, "failed": 1 },
                "contexts": [{
                    "description": "Parent",
                    "specs": [],
                    "contexts": [{
                        "description": "Child",
                        "specs": [
                            { "description": "passes", "status": "passed" },
                            { "description": "fails", "status": "failed" }
                        ],
                        "contexts": []
                    }]
                }]
            }
            """;
        var fileSystem = new MockFileSystem()
            .AddFile("/results.json", resultsJson);
        // Create specs matching the nested structure
        var context = CreateContextWithSpecs(console, fileSystem,
            CreateNestedSpec("passes", ["Parent", "Child"]),
            CreateNestedSpec("fails", ["Parent", "Child"]));
        context.Set(ContextKeys.ProjectPath, "/test/project");
        context.Set(ContextKeys.DocsFormat, DocsFormat.Markdown);
        context.Set(ContextKeys.WithResults, true);
        context.Set(ContextKeys.ResultsFile, "/results.json");

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        // Should have both passed and failed specs with appropriate markers
        await Assert.That(console.Output).Contains("- [x] passes"); // passed
        await Assert.That(console.Output).Contains("**FAILED**"); // failed marker
    }

    #endregion

    #region Helper Methods

    private static CommandContext CreateContext(MockConsole? console = null, MockFileSystem? fileSystem = null)
    {
        return new CommandContext
        {
            Path = "/test",
            Console = console ?? new MockConsole(),
            FileSystem = fileSystem ?? new MockFileSystem()
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

    private static CommandContext CreateContextWithSpecs(
        MockConsole console,
        MockFileSystem fileSystem,
        params DiscoveredSpec[] specs)
    {
        var context = CreateContext(console, fileSystem);
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
        // ID format matches FlattenResults: ":{contextPath}/{description}"
        return new DiscoveredSpec
        {
            Id = $":{contextName}/{description}",
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

    private static DiscoveredSpec CreateNestedSpec(
        string description,
        string[] contextPath,
        bool isPending = false,
        bool isSkipped = false,
        bool isFocused = false)
    {
        // ID format matches FlattenResults: ":{contextPath}/{description}"
        var contextPathString = string.Join("/", contextPath);
        return new DiscoveredSpec
        {
            Id = $":{contextPathString}/{description}",
            Description = description,
            DisplayName = $"{contextPathString} > {description}",
            ContextPath = contextPath,
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
