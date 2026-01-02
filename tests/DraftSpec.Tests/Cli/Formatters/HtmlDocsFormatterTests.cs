using DraftSpec.Cli.Formatters;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Cli.Formatters;

/// <summary>
/// Unit tests for HtmlDocsFormatter.
/// </summary>
public class HtmlDocsFormatterTests
{
    private readonly HtmlDocsFormatter _formatter = new();

    #region Basic HTML Structure

    [Test]
    public async Task Format_EmptySpecs_ContainsHtmlStructure()
    {
        var output = _formatter.Format([], CreateMetadata());

        await Assert.That(output).Contains("<!DOCTYPE html>");
        await Assert.That(output).Contains("<html");
        await Assert.That(output).Contains("</html>");
    }

    [Test]
    public async Task Format_ContainsTitle()
    {
        var output = _formatter.Format([], CreateMetadata());

        await Assert.That(output).Contains("<title>Test Specifications</title>");
    }

    [Test]
    public async Task Format_ContainsStylesheet()
    {
        var output = _formatter.Format([], CreateMetadata());

        await Assert.That(output).Contains("<style>");
        await Assert.That(output).Contains(".passed");
        await Assert.That(output).Contains(".failed");
    }

    [Test]
    public async Task Format_ContainsSimpleCssLink()
    {
        var output = _formatter.Format([], CreateMetadata());

        await Assert.That(output).Contains("cdn.simplecss.org");
    }

    #endregion

    #region Spec Rendering

    [Test]
    public async Task Format_SingleSpec_ShowsInList()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("adds numbers", ["Calculator"])
        };

        var output = _formatter.Format(specs, CreateMetadata());

        await Assert.That(output).Contains("adds numbers");
        await Assert.That(output).Contains("<li");
    }

    [Test]
    public async Task Format_NestedContext_ShowsDetailsElement()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("adds numbers", ["Calculator", "Math"])
        };

        var output = _formatter.Format(specs, CreateMetadata());

        await Assert.That(output).Contains("<details");
        await Assert.That(output).Contains("<summary>");
        await Assert.That(output).Contains("Calculator");
    }

    [Test]
    public async Task Format_Context_ShowsSpecCount()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("spec1", ["Feature"]),
            CreateSpec("spec2", ["Feature"])
        };

        var output = _formatter.Format(specs, CreateMetadata());

        await Assert.That(output).Contains("2 specs");
    }

    #endregion

    #region Status Badges

    [Test]
    public async Task Format_PendingSpec_ShowsBadge()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("pending spec", ["Feature"], isPending: true)
        };

        var output = _formatter.Format(specs, CreateMetadata());

        await Assert.That(output).Contains("PENDING");
        await Assert.That(output).Contains("badge-pending");
    }

    [Test]
    public async Task Format_SkippedSpec_ShowsBadge()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("skipped spec", ["Feature"], isSkipped: true)
        };

        var output = _formatter.Format(specs, CreateMetadata());

        await Assert.That(output).Contains("SKIPPED");
        await Assert.That(output).Contains("badge-skipped");
    }

    [Test]
    public async Task Format_FocusedSpec_ShowsBadge()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("focused spec", ["Feature"], isFocused: true)
        };

        var output = _formatter.Format(specs, CreateMetadata());

        await Assert.That(output).Contains("FOCUSED");
        await Assert.That(output).Contains("badge-focused");
    }

    #endregion

    #region Results Integration

    [Test]
    public async Task Format_WithPassedResult_ShowsCheckmark()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("passing spec", ["Feature"])
        };
        var results = new Dictionary<string, string>
        {
            { specs[0].Id, "passed" }
        };

        var output = _formatter.Format(specs, CreateMetadata(results: results));

        await Assert.That(output).Contains("class=\"passed\"");
    }

    [Test]
    public async Task Format_WithFailedResult_ShowsFailedBadge()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("failing spec", ["Feature"])
        };
        var results = new Dictionary<string, string>
        {
            { specs[0].Id, "failed" }
        };

        var output = _formatter.Format(specs, CreateMetadata(results: results));

        await Assert.That(output).Contains("FAILED");
        await Assert.That(output).Contains("badge-failed");
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

        var output = _formatter.Format(specs, CreateMetadata(results: results));

        await Assert.That(output).Contains("1 passed");
        await Assert.That(output).Contains("1 failed");
    }

    #endregion

    #region Summary Statistics

    [Test]
    public async Task Format_MixedSpecs_ShowsCorrectSummary()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("regular", ["Feature"]),
            CreateSpec("pending", ["Feature"], isPending: true),
            CreateSpec("skipped", ["Feature"], isSkipped: true),
            CreateSpec("focused", ["Feature"], isFocused: true)
        };

        var output = _formatter.Format(specs, CreateMetadata());

        await Assert.That(output).Contains("<strong>4 specs</strong>");
        await Assert.That(output).Contains("1 pending");
        await Assert.That(output).Contains("1 skipped");
        await Assert.That(output).Contains("1 focused");
    }

    [Test]
    public async Task Format_ContainsGeneratedTimestamp()
    {
        var metadata = CreateMetadata(generatedAt: new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc));

        var output = _formatter.Format([], metadata);

        await Assert.That(output).Contains("2025-06-15 10:30:00");
        await Assert.That(output).Contains("UTC");
    }

    [Test]
    public async Task Format_ContainsSourcePath()
    {
        var metadata = CreateMetadata(source: "./specs");

        var output = _formatter.Format([], metadata);

        await Assert.That(output).Contains("<code>./specs</code>");
    }

    #endregion

    #region Symbols

    [Test]
    public async Task Format_PassedSpec_ShowsCheckSymbol()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("passed spec", ["Feature"])
        };
        var results = new Dictionary<string, string>
        {
            { specs[0].Id, "passed" }
        };

        var output = _formatter.Format(specs, CreateMetadata(results: results));

        // U+2713 checkmark
        await Assert.That(output).Contains("✓");
    }

    [Test]
    public async Task Format_FailedSpec_ShowsXSymbol()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("failed spec", ["Feature"])
        };
        var results = new Dictionary<string, string>
        {
            { specs[0].Id, "failed" }
        };

        var output = _formatter.Format(specs, CreateMetadata(results: results));

        // U+2717 ballot x
        await Assert.That(output).Contains("✗");
    }

    [Test]
    public async Task Format_PendingSpec_ShowsCircleSymbol()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("pending spec", ["Feature"], isPending: true)
        };

        var output = _formatter.Format(specs, CreateMetadata());

        // U+25CB circle
        await Assert.That(output).Contains("○");
    }

    [Test]
    public async Task Format_SkippedSpec_ShowsDashSymbol()
    {
        var specs = new List<DiscoveredSpec>
        {
            CreateSpec("skipped spec", ["Feature"], isSkipped: true)
        };

        var output = _formatter.Format(specs, CreateMetadata());

        // U+2212 minus
        await Assert.That(output).Contains("−");
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
