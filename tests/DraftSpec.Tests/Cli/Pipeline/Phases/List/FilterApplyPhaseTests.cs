using DraftSpec.Cli.Options;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.List;
using DraftSpec.TestingPlatform;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.List;

/// <summary>
/// Tests for <see cref="FilterApplyPhase"/>.
/// </summary>
public class FilterApplyPhaseTests
{
    #region Happy Path Tests

    [Test]
    public async Task ExecuteAsync_ConvertsParsedSpecsToDiscoveredSpecs()
    {
        var phase = new FilterApplyPhase();
        var context = CreateContextWithParsedSpecs("/specs", new Dictionary<string, StaticParseResult>
        {
            ["/specs/test.spec.csx"] = new StaticParseResult
            {
                Specs =
                [
                    CreateStaticSpec("adds numbers", ["Calculator"])
                ]
            }
        });

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        var filteredSpecs = context.Get<IReadOnlyList<DiscoveredSpec>>(ContextKeys.FilteredSpecs);
        await Assert.That(filteredSpecs).IsNotNull();
        await Assert.That(filteredSpecs!.Count).IsEqualTo(1);
        await Assert.That(filteredSpecs[0].Description).IsEqualTo("adds numbers");
        await Assert.That(filteredSpecs[0].DisplayName).IsEqualTo("Calculator > adds numbers");
    }

    [Test]
    public async Task ExecuteAsync_GeneratesCorrectId()
    {
        var phase = new FilterApplyPhase();
        var context = CreateContextWithParsedSpecs("/specs", new Dictionary<string, StaticParseResult>
        {
            ["/specs/test.spec.csx"] = new StaticParseResult
            {
                Specs =
                [
                    CreateStaticSpec("my spec", ["Context", "Nested"])
                ]
            }
        });

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        var filteredSpecs = context.Get<IReadOnlyList<DiscoveredSpec>>(ContextKeys.FilteredSpecs)!;
        await Assert.That(filteredSpecs[0].Id).IsEqualTo("test.spec.csx:Context/Nested/my spec");
    }

    [Test]
    public async Task ExecuteAsync_PropagatesPipelineResult()
    {
        var phase = new FilterApplyPhase();
        var context = CreateContextWithParsedSpecs("/specs", CreateEmptyParsedSpecs());

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(42),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task ExecuteAsync_CollectsWarningsAsDiscoveryErrors()
    {
        var phase = new FilterApplyPhase();
        var context = CreateContextWithParsedSpecs("/specs", new Dictionary<string, StaticParseResult>
        {
            ["/specs/test.spec.csx"] = new StaticParseResult
            {
                Specs = [],
                Warnings = ["Dynamic description at line 5"],
                IsComplete = false  // Warnings are only collected when parsing is incomplete
            }
        });

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        var errors = context.Get<IReadOnlyList<DiscoveryError>>(ContextKeys.DiscoveryErrors)!;
        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0].Message).IsEqualTo("Dynamic description at line 5");
        await Assert.That(errors[0].RelativeSourceFile).IsEqualTo("test.spec.csx");
    }

    #endregion

    #region Filter Tests

    [Test]
    public async Task ExecuteAsync_FocusedOnlyFilter_FiltersNonFocused()
    {
        var phase = new FilterApplyPhase();
        var context = CreateContextWithParsedSpecs("/specs", new Dictionary<string, StaticParseResult>
        {
            ["/specs/test.spec.csx"] = new StaticParseResult
            {
                Specs =
                [
                    CreateStaticSpec("regular spec", [], StaticSpecType.Regular),
                    CreateStaticSpec("focused spec", [], StaticSpecType.Focused)
                ]
            }
        });
        context.Set(ContextKeys.FocusedOnly, true);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        var filteredSpecs = context.Get<IReadOnlyList<DiscoveredSpec>>(ContextKeys.FilteredSpecs)!;
        await Assert.That(filteredSpecs.Count).IsEqualTo(1);
        await Assert.That(filteredSpecs[0].Description).IsEqualTo("focused spec");
    }

    [Test]
    public async Task ExecuteAsync_PendingOnlyFilter_FiltersToPendingSpecs()
    {
        var phase = new FilterApplyPhase();
        var context = CreateContextWithParsedSpecs("/specs", new Dictionary<string, StaticParseResult>
        {
            ["/specs/test.spec.csx"] = new StaticParseResult
            {
                Specs =
                [
                    CreateStaticSpec("regular spec", [], StaticSpecType.Regular, isPending: false),
                    CreateStaticSpec("pending spec", [], StaticSpecType.Regular, isPending: true)
                ]
            }
        });
        context.Set(ContextKeys.PendingOnly, true);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        var filteredSpecs = context.Get<IReadOnlyList<DiscoveredSpec>>(ContextKeys.FilteredSpecs)!;
        await Assert.That(filteredSpecs.Count).IsEqualTo(1);
        await Assert.That(filteredSpecs[0].Description).IsEqualTo("pending spec");
    }

    [Test]
    public async Task ExecuteAsync_SkippedOnlyFilter_FiltersToSkippedSpecs()
    {
        var phase = new FilterApplyPhase();
        var context = CreateContextWithParsedSpecs("/specs", new Dictionary<string, StaticParseResult>
        {
            ["/specs/test.spec.csx"] = new StaticParseResult
            {
                Specs =
                [
                    CreateStaticSpec("regular spec", [], StaticSpecType.Regular),
                    CreateStaticSpec("skipped spec", [], StaticSpecType.Skipped)
                ]
            }
        });
        context.Set(ContextKeys.SkippedOnly, true);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        var filteredSpecs = context.Get<IReadOnlyList<DiscoveredSpec>>(ContextKeys.FilteredSpecs)!;
        await Assert.That(filteredSpecs.Count).IsEqualTo(1);
        await Assert.That(filteredSpecs[0].Description).IsEqualTo("skipped spec");
    }

    [Test]
    public async Task ExecuteAsync_MultipleStatusFilters_UsesOrLogic()
    {
        var phase = new FilterApplyPhase();
        var context = CreateContextWithParsedSpecs("/specs", new Dictionary<string, StaticParseResult>
        {
            ["/specs/test.spec.csx"] = new StaticParseResult
            {
                Specs =
                [
                    CreateStaticSpec("regular spec", [], StaticSpecType.Regular),
                    CreateStaticSpec("focused spec", [], StaticSpecType.Focused),
                    CreateStaticSpec("pending spec", [], StaticSpecType.Regular, isPending: true)
                ]
            }
        });
        context.Set(ContextKeys.FocusedOnly, true);
        context.Set(ContextKeys.PendingOnly, true);

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        var filteredSpecs = context.Get<IReadOnlyList<DiscoveredSpec>>(ContextKeys.FilteredSpecs)!;
        await Assert.That(filteredSpecs.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_FilterName_FiltersToMatchingNames()
    {
        var phase = new FilterApplyPhase();
        var context = CreateContextWithParsedSpecs("/specs", new Dictionary<string, StaticParseResult>
        {
            ["/specs/test.spec.csx"] = new StaticParseResult
            {
                Specs =
                [
                    CreateStaticSpec("add numbers", ["Calculator"]),
                    CreateStaticSpec("subtract numbers", ["Calculator"])
                ]
            }
        });
        context.Set(ContextKeys.Filter, new FilterOptions { FilterName = "add" });

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        var filteredSpecs = context.Get<IReadOnlyList<DiscoveredSpec>>(ContextKeys.FilteredSpecs)!;
        await Assert.That(filteredSpecs.Count).IsEqualTo(1);
        await Assert.That(filteredSpecs[0].Description).IsEqualTo("add numbers");
    }

    [Test]
    public async Task ExecuteAsync_FilterTags_FiltersToMatchingTags()
    {
        // Note: Current implementation sets Tags = [] for all specs from static parsing,
        // so this tests that the filter correctly filters out specs without tags
        var phase = new FilterApplyPhase();
        var context = CreateContextWithParsedSpecs("/specs", new Dictionary<string, StaticParseResult>
        {
            ["/specs/test.spec.csx"] = new StaticParseResult
            {
                Specs =
                [
                    CreateStaticSpec("spec one", []),
                    CreateStaticSpec("spec two", [])
                ]
            }
        });
        context.Set(ContextKeys.Filter, new FilterOptions { FilterTags = "slow" });

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        var filteredSpecs = context.Get<IReadOnlyList<DiscoveredSpec>>(ContextKeys.FilteredSpecs)!;
        await Assert.That(filteredSpecs.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_StatusAndNameFilterCombined_UsesAndLogic()
    {
        var phase = new FilterApplyPhase();
        var context = CreateContextWithParsedSpecs("/specs", new Dictionary<string, StaticParseResult>
        {
            ["/specs/test.spec.csx"] = new StaticParseResult
            {
                Specs =
                [
                    CreateStaticSpec("focused apple", ["Test"], StaticSpecType.Focused),
                    CreateStaticSpec("focused banana", ["Test"], StaticSpecType.Focused),
                    CreateStaticSpec("regular apple", ["Test"], StaticSpecType.Regular)
                ]
            }
        });
        context.Set(ContextKeys.FocusedOnly, true);
        context.Set(ContextKeys.Filter, new FilterOptions { FilterName = "apple" });

        await phase.ExecuteAsync(context, (_, _) => Task.FromResult(0), CancellationToken.None);

        var filteredSpecs = context.Get<IReadOnlyList<DiscoveredSpec>>(ContextKeys.FilteredSpecs)!;
        await Assert.That(filteredSpecs.Count).IsEqualTo(1);
        await Assert.That(filteredSpecs[0].Description).IsEqualTo("focused apple");
    }

    #endregion

    #region Error Tests

    [Test]
    public async Task ExecuteAsync_ProjectPathNotSet_ReturnsError()
    {
        var phase = new FilterApplyPhase();
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
    public async Task ExecuteAsync_ParsedSpecsNotSet_ReturnsError()
    {
        var phase = new FilterApplyPhase();
        var console = new MockConsole();
        var context = CreateContext(console);
        context.Set<string>(ContextKeys.ProjectPath, "/specs");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(console.Errors).Contains("ParsedSpecs not set");
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

    private static CommandContext CreateContextWithParsedSpecs(
        string projectPath,
        Dictionary<string, StaticParseResult> parsedSpecs,
        MockConsole? console = null)
    {
        var context = CreateContext(console);
        context.Set<string>(ContextKeys.ProjectPath, projectPath);
        context.Set<IReadOnlyDictionary<string, StaticParseResult>>(ContextKeys.ParsedSpecs, parsedSpecs);
        return context;
    }

    private static Dictionary<string, StaticParseResult> CreateEmptyParsedSpecs()
    {
        return new Dictionary<string, StaticParseResult>
        {
            ["/specs/empty.spec.csx"] = new StaticParseResult { Specs = [] }
        };
    }

    private static StaticSpec CreateStaticSpec(
        string description,
        string[] contextPath,
        StaticSpecType type = StaticSpecType.Regular,
        bool isPending = false)
    {
        return new StaticSpec
        {
            Description = description,
            ContextPath = contextPath,
            LineNumber = 1,
            Type = type,
            IsPending = isPending
        };
    }

    #endregion
}
