using DraftSpec.Cli.Formatters;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Cli.Formatters;

/// <summary>
/// Unit tests for MarkdownDocsFormatter.
/// </summary>
public class MarkdownDocsFormatterTests
{
    private readonly MarkdownDocsFormatter _formatter = new();

    #region Basic Output Structure

    [Test]
    public async Task Format_EmptySpecs_ContainsHeader()
    {
        var output = _formatter.Format([], CreateMetadata());

        await Assert.That(output).Contains("# Test Specifications");
    }

    [Test]
    public async Task Format_EmptySpecs_ShowsZeroInSummary()
    {
        var output = _formatter.Format([], CreateMetadata());

        await Assert.That(output).Contains("0 specs");
    }

    [Test]
    public async Task Format_ContainsGeneratedTimestamp()
    {
        var metadata = CreateMetadata(generatedAt: new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc));

        var output = _formatter.Format([], metadata);

        await Assert.That(output).Contains("2025-06-15 10:30:00 UTC");
    }

    [Test]
    public async Task Format_ContainsSourcePath()
    {
        var metadata = CreateMetadata(source: "./specs");

        var output = _formatter.Format([], metadata);

        await Assert.That(output).Contains("`./specs`");
    }

    #endregion

    #region Spec Rendering

    [Test]
    public async Task Format_SingleSpec_ShowsDescription()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("adds numbers", ["Calculator"])
        };

        var output = _formatter.Format(specs, CreateMetadata());

        await Assert.That(output).Contains("adds numbers");
    }

    [Test]
    public async Task Format_NestedContext_ShowsHierarchy()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("adds numbers", ["Calculator", "Operations", "Math"])
        };

        var output = _formatter.Format(specs, CreateMetadata());

        await Assert.That(output).Contains("## Calculator");
        await Assert.That(output).Contains("### Operations");
        await Assert.That(output).Contains("#### Math");
    }

    [Test]
    public async Task Format_MultipleContextLevels_LimitedToFourHeadings()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("spec", ["L1", "L2", "L3", "L4", "L5", "L6"])
        };

        var output = _formatter.Format(specs, CreateMetadata());

        // After level 4, should use lists not headings
        await Assert.That(output).Contains("## L1");
        await Assert.That(output).Contains("### L2");
        await Assert.That(output).Contains("#### L3");
        // L4 onwards handled differently
    }

    #endregion

    #region Spec Status Rendering

    [Test]
    public async Task Format_RegularSpec_ShowsEmptyCheckbox()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("regular spec", ["Feature"])
        };

        var output = _formatter.Format(specs, CreateMetadata());

        await Assert.That(output).Contains("- [ ] regular spec");
    }

    [Test]
    public async Task Format_PendingSpec_ShowsMarker()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("pending spec", ["Feature"], isPending: true)
        };

        var output = _formatter.Format(specs, CreateMetadata());

        await Assert.That(output).Contains("*(pending)*");
    }

    [Test]
    public async Task Format_SkippedSpec_ShowsMarker()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("skipped spec", ["Feature"], isSkipped: true)
        };

        var output = _formatter.Format(specs, CreateMetadata());

        await Assert.That(output).Contains("*(skipped)*");
    }

    [Test]
    public async Task Format_FocusedSpec_ShowsInSummary()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("focused spec", ["Feature"], isFocused: true)
        };

        var output = _formatter.Format(specs, CreateMetadata());

        // Focused specs are counted in summary, not marked individually
        await Assert.That(output).Contains("1 focused");
    }

    #endregion

    #region Results Integration

    [Test]
    public async Task Format_WithPassedResult_ShowsCheckedCheckbox()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("passing spec", ["Feature"])
        };
        var results = new Dictionary<string, string>
        {
            { specs[0].Id, "passed" }
        };
        var metadata = CreateMetadata(results: results);

        var output = _formatter.Format(specs, metadata);

        await Assert.That(output).Contains("- [x] passing spec");
    }

    [Test]
    public async Task Format_WithFailedResult_ShowsFailedCheckbox()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("failing spec", ["Feature"])
        };
        var results = new Dictionary<string, string>
        {
            { specs[0].Id, "failed" }
        };
        var metadata = CreateMetadata(results: results);

        var output = _formatter.Format(specs, metadata);

        await Assert.That(output).Contains("- [x] failing spec **FAILED**");
    }

    [Test]
    public async Task Format_WithResults_ShowsSummaryStats()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("spec1", ["Feature"]),
            CreateSpec("spec2", ["Feature"])
        };
        var results = new Dictionary<string, string>
        {
            { specs[0].Id, "passed" },
            { specs[1].Id, "failed" }
        };
        var metadata = CreateMetadata(results: results);

        var output = _formatter.Format(specs, metadata);

        await Assert.That(output).Contains("1 passed");
        await Assert.That(output).Contains("1 failed");
    }

    #endregion

    #region Summary Statistics

    [Test]
    public async Task Format_MixedSpecs_ShowsCorrectCounts()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("regular", ["Feature"]),
            CreateSpec("pending", ["Feature"], isPending: true),
            CreateSpec("skipped", ["Feature"], isSkipped: true),
            CreateSpec("focused", ["Feature"], isFocused: true)
        };

        var output = _formatter.Format(specs, CreateMetadata());

        await Assert.That(output).Contains("4 specs");
        await Assert.That(output).Contains("1 pending");
        await Assert.That(output).Contains("1 skipped");
        await Assert.That(output).Contains("1 focused");
    }

    #endregion

    #region Helper Methods

    private static DocsMetadata CreateMetadata(
        DateTime? generatedAt = null,
        string? source = null,
        IReadOnlyDictionary<string, string>? results = null)
    {
        return new DocsMetadata(
            generatedAt ?? DateTime.UtcNow,
            source,
            results);
    }

    private static DiscoveredSpec CreateSpec(
        string description,
        string[] contextPath,
        bool isPending = false,
        bool isSkipped = false,
        bool isFocused = false)
    {
        var relativePath = "test.spec.csx";
        var id = $"{relativePath}:{string.Join("/", contextPath)}/{description}";

        return new DiscoveredSpec
        {
            Id = id,
            Description = description,
            DisplayName = string.Join(" > ", contextPath) + " > " + description,
            ContextPath = contextPath.ToList(),
            SourceFile = "/full/path/" + relativePath,
            RelativeSourceFile = relativePath,
            LineNumber = 1,
            IsPending = isPending,
            IsSkipped = isSkipped,
            IsFocused = isFocused,
            Tags = []
        };
    }

    #endregion
}
