using DraftSpec.Cli.Interactive;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Cli.Interactive;

/// <summary>
/// Tests for SelectableSpec view model.
/// </summary>
public class SelectableSpecTests
{
    #region FormattedDisplay

    [Test]
    public async Task FormattedDisplay_RegularSpec_ShowsGreenDotIcon()
    {
        var spec = CreateSpec();

        await Assert.That(spec.FormattedDisplay).Contains("[green].[/]");
    }

    [Test]
    public async Task FormattedDisplay_RegularSpec_NoStatusSuffix()
    {
        var spec = CreateSpec();

        await Assert.That(spec.FormattedDisplay).IsEqualTo("[green].[/] Test > spec");
    }

    [Test]
    public async Task FormattedDisplay_FocusedSpec_ShowsYellowStarIcon()
    {
        var spec = CreateSpec(isFocused: true);

        await Assert.That(spec.FormattedDisplay).Contains("[yellow]*[/]");
    }

    [Test]
    public async Task FormattedDisplay_FocusedSpec_ShowsFocusedSuffix()
    {
        var spec = CreateSpec(isFocused: true);

        await Assert.That(spec.FormattedDisplay).Contains("[yellow](focused)[/]");
    }

    [Test]
    public async Task FormattedDisplay_SkippedSpec_ShowsDimDashIcon()
    {
        var spec = CreateSpec(isSkipped: true);

        await Assert.That(spec.FormattedDisplay).Contains("[dim]-[/]");
    }

    [Test]
    public async Task FormattedDisplay_SkippedSpec_ShowsSkippedSuffix()
    {
        var spec = CreateSpec(isSkipped: true);

        await Assert.That(spec.FormattedDisplay).Contains("[dim](skipped)[/]");
    }

    [Test]
    public async Task FormattedDisplay_PendingSpec_ShowsBlueQuestionIcon()
    {
        var spec = CreateSpec(isPending: true);

        await Assert.That(spec.FormattedDisplay).Contains("[blue]?[/]");
    }

    [Test]
    public async Task FormattedDisplay_PendingSpec_ShowsPendingSuffix()
    {
        var spec = CreateSpec(isPending: true);

        await Assert.That(spec.FormattedDisplay).Contains("[blue](pending)[/]");
    }

    [Test]
    public async Task FormattedDisplay_FocusedTakesPrecedenceOverSkipped()
    {
        // When multiple flags are set, focused should take precedence
        var spec = CreateSpec(isFocused: true, isSkipped: true);

        await Assert.That(spec.FormattedDisplay).Contains("[yellow]*[/]");
        await Assert.That(spec.FormattedDisplay).Contains("(focused)");
    }

    [Test]
    public async Task FormattedDisplay_SkippedTakesPrecedenceOverPending()
    {
        var spec = CreateSpec(isSkipped: true, isPending: true);

        await Assert.That(spec.FormattedDisplay).Contains("[dim]-[/]");
        await Assert.That(spec.FormattedDisplay).Contains("(skipped)");
    }

    [Test]
    public async Task FormattedDisplay_IncludesDisplayName()
    {
        var spec = CreateSpec(displayName: "Context > nested > spec name");

        await Assert.That(spec.FormattedDisplay).Contains("Context > nested > spec name");
    }

    #endregion

    #region FromDiscoveredSpec

    [Test]
    public async Task FromDiscoveredSpec_CopiesAllProperties()
    {
        var discovered = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec description",
            DisplayName = "Context > spec description",
            ContextPath = ["Context"],
            SourceFile = "/path/to/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 42,
            IsPending = true,
            IsSkipped = false,
            IsFocused = true,
            Tags = ["slow", "integration"]
        };

        var selectable = SelectableSpec.FromDiscoveredSpec(discovered);

        await Assert.That(selectable.Id).IsEqualTo("test.spec.csx:Context/spec");
        await Assert.That(selectable.Description).IsEqualTo("spec description");
        await Assert.That(selectable.DisplayName).IsEqualTo("Context > spec description");
        await Assert.That(selectable.ContextPath).IsEquivalentTo(new[] { "Context" });
        await Assert.That(selectable.RelativeSourceFile).IsEqualTo("test.spec.csx");
        await Assert.That(selectable.LineNumber).IsEqualTo(42);
        await Assert.That(selectable.IsPending).IsTrue();
        await Assert.That(selectable.IsSkipped).IsFalse();
        await Assert.That(selectable.IsFocused).IsTrue();
    }

    [Test]
    public async Task FromDiscoveredSpec_EmptyContextPath_HandlesCorrectly()
    {
        var discovered = new DiscoveredSpec
        {
            Id = "test.spec.csx:spec",
            Description = "top level spec",
            DisplayName = "top level spec",
            ContextPath = [],
            SourceFile = "/path/to/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 1,
            Tags = []
        };

        var selectable = SelectableSpec.FromDiscoveredSpec(discovered);

        await Assert.That(selectable.ContextPath).IsEmpty();
    }

    #endregion

    #region Helpers

    private static SelectableSpec CreateSpec(
        string displayName = "Test > spec",
        bool isFocused = false,
        bool isSkipped = false,
        bool isPending = false)
    {
        return new SelectableSpec
        {
            Id = "test.spec.csx:Test/spec",
            DisplayName = displayName,
            Description = "spec",
            ContextPath = ["Test"],
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10,
            IsFocused = isFocused,
            IsSkipped = isSkipped,
            IsPending = isPending
        };
    }

    #endregion
}
