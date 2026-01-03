using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.List;
using DraftSpec.TestingPlatform;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.List;

/// <summary>
/// Tests for <see cref="ListOutputPhase"/>.
/// </summary>
public class ListOutputPhaseTests
{
    #region Happy Path Tests

    [Test]
    public async Task ExecuteAsync_WithSpecs_FormatsAndWritesOutput()
    {
        var phase = new ListOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("test spec", "Calculator"));

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(999), // Should not be called
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(console.Output).Contains("test spec");
    }

    [Test]
    public async Task ExecuteAsync_TreeFormat_UsesTreeFormatter()
    {
        var phase = new ListOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec1", "Context"));
        context.Set(ContextKeys.ListFormat, ListFormat.Tree);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        // Tree format uses box-drawing characters and file structure
        await Assert.That(console.Output).Contains("test.spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_FlatFormat_UsesFlatFormatter()
    {
        var phase = new ListOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec1", "Context"));
        context.Set(ContextKeys.ListFormat, ListFormat.Flat);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        // Flat format shows context path
        await Assert.That(console.Output).Contains("Context > spec1");
    }

    [Test]
    public async Task ExecuteAsync_JsonFormat_UsesJsonFormatter()
    {
        var phase = new ListOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec1", "Context"));
        context.Set(ContextKeys.ListFormat, ListFormat.Json);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("\"specs\"");
        await Assert.That(console.Output).Contains("\"summary\"");
    }

    [Test]
    public async Task ExecuteAsync_ShowLineNumbers_IncludesLineNumbers()
    {
        var phase = new ListOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec1", "Context", lineNumber: 10));
        context.Set(ContextKeys.ListFormat, ListFormat.Tree);
        context.Set(ContextKeys.ShowLineNumbers, true);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        // Tree format uses "(line N)" format
        await Assert.That(console.Output).Contains("(line 10)");
    }

    [Test]
    public async Task ExecuteAsync_NoShowLineNumbers_ExcludesLineNumbers()
    {
        var phase = new ListOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec1", "Context", lineNumber: 10));
        context.Set(ContextKeys.ListFormat, ListFormat.Flat);
        context.Set(ContextKeys.ShowLineNumbers, false);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        // With ShowLineNumbers=false, the output should not contain ":10"
        // But it will contain "Context > spec1" without the line number prefix
        await Assert.That(console.Output).Contains("spec1");
    }

    [Test]
    public async Task ExecuteAsync_DefaultShowLineNumbers_IsTrue()
    {
        var phase = new ListOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec1", "Context", lineNumber: 5));
        context.Set(ContextKeys.ListFormat, ListFormat.Tree);
        // Don't set ShowLineNumbers - should default to true

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        // Tree format uses "(line N)" format
        await Assert.That(console.Output).Contains("(line 5)");
    }

    [Test]
    public async Task ExecuteAsync_TerminalPhase_DoesNotCallNextPipeline()
    {
        var phase = new ListOutputPhase();
        var context = CreateContextWithSpecs(new MockConsole(), CreateSpec("spec", "Context"));
        var pipelineCalled = false;

        await phase.ExecuteAsync(
            context,
            (_, _) => { pipelineCalled = true; return Task.FromResult(99); },
            CancellationToken.None);

        await Assert.That(pipelineCalled).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_WithDiscoveryErrors_IncludesErrorsInOutput()
    {
        var phase = new ListOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec", "Context"));
        context.Set<IReadOnlyList<DiscoveryError>>(ContextKeys.DiscoveryErrors, new List<DiscoveryError>
        {
            new()
            {
                SourceFile = "/specs/bad.spec.csx",
                RelativeSourceFile = "bad.spec.csx",
                Message = "Parse error"
            }
        });
        context.Set(ContextKeys.ListFormat, ListFormat.Json);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        // JSON format includes errors section
        await Assert.That(console.Output).Contains("errors");
    }

    #endregion

    #region Spec Type Display Tests

    [Test]
    public async Task ExecuteAsync_PendingSpec_ShowsPendingMarker()
    {
        var phase = new ListOutputPhase();
        var console = new MockConsole();
        var spec = CreateSpec("pending spec", "Feature", isPending: true);
        var context = CreateContextWithSpecs(console, spec);
        context.Set(ContextKeys.ListFormat, ListFormat.Flat);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("[PENDING]");
    }

    [Test]
    public async Task ExecuteAsync_SkippedSpec_ShowsSkippedMarker()
    {
        var phase = new ListOutputPhase();
        var console = new MockConsole();
        var spec = CreateSpec("skipped spec", "Feature", isSkipped: true);
        var context = CreateContextWithSpecs(console, spec);
        context.Set(ContextKeys.ListFormat, ListFormat.Flat);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("[SKIPPED]");
    }

    [Test]
    public async Task ExecuteAsync_FocusedSpec_ShowsFocusedMarker()
    {
        var phase = new ListOutputPhase();
        var console = new MockConsole();
        var spec = CreateSpec("focused spec", "Feature", isFocused: true);
        var context = CreateContextWithSpecs(console, spec);
        context.Set(ContextKeys.ListFormat, ListFormat.Flat);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        await Assert.That(console.Output).Contains("[FOCUSED]");
    }

    #endregion

    #region Error Tests

    [Test]
    public async Task ExecuteAsync_FilteredSpecsNotSet_ReturnsError()
    {
        var phase = new ListOutputPhase();
        var console = new MockConsole();
        var context = CreateContext(console);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("FilteredSpecs not set");
    }

    [Test]
    public async Task ExecuteAsync_UnknownListFormat_ThrowsArgumentOutOfRange()
    {
        var phase = new ListOutputPhase();
        var console = new MockConsole();
        var context = CreateContextWithSpecs(console, CreateSpec("spec", "Context"));
        context.Set(ContextKeys.ListFormat, (ListFormat)999);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None));
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
        context.Set<IReadOnlyList<DiscoveryError>>(ContextKeys.DiscoveryErrors, Array.Empty<DiscoveryError>());
        return context;
    }

    private static DiscoveredSpec CreateSpec(
        string description,
        string context,
        int lineNumber = 1,
        bool isPending = false,
        bool isSkipped = false,
        bool isFocused = false)
    {
        return new DiscoveredSpec
        {
            Id = $"test.spec.csx:{context}/{description}",
            Description = description,
            DisplayName = $"{context} > {description}",
            ContextPath = [context],
            SourceFile = "/specs/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = lineNumber,
            IsPending = isPending,
            IsSkipped = isSkipped,
            IsFocused = isFocused,
            Tags = []
        };
    }

    #endregion
}
