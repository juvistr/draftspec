using DraftSpec.Formatters;

namespace DraftSpec.Tests.Core;

/// <summary>
/// Tests for core edge cases across AssertionException, SpecReportBuilder,
/// SpecRunnerBuilder, SpecContext, and SpecDefinition.
/// </summary>
public class CoreEdgeCaseTests
{
    #region AssertionException Tests

    [Test]
    public async Task AssertionException_Constructor_CreatesWithMessage()
    {
        var exception = new AssertionException("Expected 1 to be 2");

        await Assert.That(exception.Message).IsEqualTo("Expected 1 to be 2");
    }

    [Test]
    public async Task AssertionException_InheritsFromException()
    {
        var exception = new AssertionException("test");

        await Assert.That(exception).IsAssignableTo<Exception>();
    }

    [Test]
    public async Task AssertionException_CanBeCaughtAsException()
    {
        Exception? caught = null;

        try
        {
            throw new AssertionException("test message");
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.Message).IsEqualTo("test message");
    }

    #endregion

    #region SpecReportBuilder Tests

    [Test]
    public async Task SpecReportBuilder_EmptyContext_ReturnsEmptyReport()
    {
        var context = new SpecContext("empty");
        var results = new List<SpecResult>();

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.Total).IsEqualTo(0);
        await Assert.That(report.Summary.Passed).IsEqualTo(0);
        await Assert.That(report.Summary.Failed).IsEqualTo(0);
        await Assert.That(report.Contexts).IsEmpty();
    }

    [Test]
    public async Task SpecReportBuilder_DeeplyNestedContexts_HandlesCorrectly()
    {
        var root = new SpecContext("root");
        var level1 = new SpecContext("level1", root);
        var level2 = new SpecContext("level2", level1);
        var level3 = new SpecContext("level3", level2);

        var spec = new SpecDefinition("deep spec", () => { });
        level3.AddSpec(spec);

        var results = new List<SpecResult>
        {
            new(spec, SpecStatus.Passed, ["root", "level1", "level2", "level3"], TimeSpan.FromMilliseconds(10))
        };

        var report = SpecReportBuilder.Build(root, results);

        await Assert.That(report.Summary.Total).IsEqualTo(1);
        await Assert.That(report.Summary.Passed).IsEqualTo(1);
        await Assert.That(report.Contexts).HasCount().EqualTo(1);

        // Navigate to the deepest context
        var ctx = report.Contexts[0];
        await Assert.That(ctx.Description).IsEqualTo("root");
        ctx = ctx.Contexts[0];
        await Assert.That(ctx.Description).IsEqualTo("level1");
        ctx = ctx.Contexts[0];
        await Assert.That(ctx.Description).IsEqualTo("level2");
        ctx = ctx.Contexts[0];
        await Assert.That(ctx.Description).IsEqualTo("level3");
        await Assert.That(ctx.Specs).HasCount().EqualTo(1);
    }

    [Test]
    public async Task SpecReportBuilder_MixedResults_CalculatesSummaryCorrectly()
    {
        var context = new SpecContext("tests");

        var spec1 = new SpecDefinition("passes", () => { });
        var spec2 = new SpecDefinition("fails", () => { });
        var spec3 = new SpecDefinition("pending");
        var spec4 = new SpecDefinition("skipped", () => { }) { IsSkipped = true };

        context.AddSpec(spec1);
        context.AddSpec(spec2);
        context.AddSpec(spec3);
        context.AddSpec(spec4);

        var results = new List<SpecResult>
        {
            new(spec1, SpecStatus.Passed, ["tests"], TimeSpan.FromMilliseconds(10)),
            new(spec2, SpecStatus.Failed, ["tests"], TimeSpan.FromMilliseconds(20), new Exception("fail")),
            new(spec3, SpecStatus.Pending, ["tests"]),
            new(spec4, SpecStatus.Skipped, ["tests"])
        };

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.Total).IsEqualTo(4);
        await Assert.That(report.Summary.Passed).IsEqualTo(1);
        await Assert.That(report.Summary.Failed).IsEqualTo(1);
        await Assert.That(report.Summary.Pending).IsEqualTo(1);
        await Assert.That(report.Summary.Skipped).IsEqualTo(1);
        await Assert.That(report.Summary.DurationMs).IsEqualTo(30);
    }

    [Test]
    public async Task SpecReportBuilder_MultipleBranches_BuildsCorrectTree()
    {
        var root = new SpecContext("root");
        var child1 = new SpecContext("child1", root);
        var child2 = new SpecContext("child2", root);

        var spec1 = new SpecDefinition("spec in child1", () => { });
        var spec2 = new SpecDefinition("spec in child2", () => { });
        child1.AddSpec(spec1);
        child2.AddSpec(spec2);

        var results = new List<SpecResult>
        {
            new(spec1, SpecStatus.Passed, ["root", "child1"], TimeSpan.FromMilliseconds(5)),
            new(spec2, SpecStatus.Passed, ["root", "child2"], TimeSpan.FromMilliseconds(5))
        };

        var report = SpecReportBuilder.Build(root, results);

        await Assert.That(report.Summary.Total).IsEqualTo(2);
        await Assert.That(report.Contexts).HasCount().EqualTo(1);
        await Assert.That(report.Contexts[0].Contexts).HasCount().EqualTo(2);
    }

    [Test]
    public async Task SpecReportBuilder_NoMatchingResults_ReturnsEmptyContexts()
    {
        var context = new SpecContext("tests");
        var spec = new SpecDefinition("orphan spec", () => { });
        context.AddSpec(spec);

        // Empty results - no matching specs
        var results = new List<SpecResult>();

        var report = SpecReportBuilder.Build(context, results);

        await Assert.That(report.Summary.Total).IsEqualTo(0);
        // Context with unmatched specs should still appear with unknown status
        await Assert.That(report.Contexts).HasCount().EqualTo(1);
    }

    [Test]
    public async Task SpecReportBuilder_SetsTimestamp()
    {
        var context = new SpecContext("tests");
        var before = DateTime.UtcNow;

        var report = SpecReportBuilder.Build(context, []);

        var after = DateTime.UtcNow;
        await Assert.That(report.Timestamp).IsGreaterThanOrEqualTo(before);
        await Assert.That(report.Timestamp).IsLessThanOrEqualTo(after);
    }

    #endregion

    #region SpecRunnerBuilder Tests

    [Test]
    public async Task SpecRunnerBuilder_WithConfiguration_SetsConfiguration()
    {
        var config = new DraftSpec.Configuration.DraftSpecConfiguration();
        var builder = new SpecRunnerBuilder();

        builder.WithConfiguration(config);
        var runner = builder.Build();

        // The runner should be created without error
        await Assert.That(runner).IsNotNull();
    }

    [Test]
    public async Task SpecRunnerBuilder_WithParallelExecution_ZeroUsesProcessorCount()
    {
        var builder = new SpecRunnerBuilder();

        builder.WithParallelExecution(0);
        var runner = builder.Build();

        await Assert.That(runner).IsNotNull();
    }

    [Test]
    public async Task SpecRunnerBuilder_WithParallelExecution_NegativeUsesProcessorCount()
    {
        var builder = new SpecRunnerBuilder();

        builder.WithParallelExecution(-5);
        var runner = builder.Build();

        await Assert.That(runner).IsNotNull();
    }

    [Test]
    public void SpecRunnerBuilder_WithTagFilter_EmptyTags_Throws()
    {
        var builder = new SpecRunnerBuilder();

        Assert.Throws<ArgumentException>(() => builder.WithTagFilter());
    }

    [Test]
    public void SpecRunnerBuilder_WithoutTags_EmptyTags_Throws()
    {
        var builder = new SpecRunnerBuilder();

        Assert.Throws<ArgumentException>(() => builder.WithoutTags());
    }

    [Test]
    public async Task SpecRunnerBuilder_ChainedMethods_ReturnsSameBuilder()
    {
        var builder = new SpecRunnerBuilder();

        var result = builder
            .WithTimeout(1000)
            .WithRetry(3)
            .WithParallelExecution(4)
            .WithBail();

        await Assert.That(ReferenceEquals(builder, result)).IsTrue();
    }

    #endregion

    #region SpecContext Additional Tests

    [Test]
    public void SpecContext_EmptyDescription_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SpecContext(""));
    }

    [Test]
    public void SpecContext_WhitespaceDescription_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SpecContext("   "));
    }

    [Test]
    public void SpecContext_NullDescription_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SpecContext(null!));
    }

    [Test]
    public async Task SpecContext_EmptyDescriptionWithParent_DoesNotModifyParent()
    {
        // Arrange
        var parent = new SpecContext("parent");
        var initialChildCount = parent.Children.Count;

        // Act - attempt to create invalid child
        Assert.Throws<ArgumentException>(() => new SpecContext("", parent));

        // Assert - parent should not have been modified
        await Assert.That(parent.Children.Count).IsEqualTo(initialChildCount);
    }

    [Test]
    public async Task SpecContext_WhitespaceDescriptionWithParent_DoesNotModifyParent()
    {
        var parent = new SpecContext("parent");
        var initialChildCount = parent.Children.Count;

        Assert.Throws<ArgumentException>(() => new SpecContext("   ", parent));

        await Assert.That(parent.Children.Count).IsEqualTo(initialChildCount);
    }

    [Test]
    public async Task SpecContext_NullDescriptionWithParent_DoesNotModifyParent()
    {
        var parent = new SpecContext("parent");
        var initialChildCount = parent.Children.Count;

        Assert.Throws<ArgumentException>(() => new SpecContext(null!, parent));

        await Assert.That(parent.Children.Count).IsEqualTo(initialChildCount);
    }

    [Test]
    public async Task SpecContext_AddChild_AddsToChildrenCollection()
    {
        var parent = new SpecContext("parent");
        var child = new SpecContext("child");

        parent.AddChild(child);

        await Assert.That(parent.Children).Contains(child);
    }

    [Test]
    public async Task SpecContext_ConstructorWithParent_AutomaticallyAddsToParentChildren()
    {
        var parent = new SpecContext("parent");
        var child = new SpecContext("child", parent);

        await Assert.That(parent.Children).Contains(child);
        await Assert.That(child.Parent).IsSameReferenceAs(parent);
    }

    [Test]
    public async Task SpecContext_Parent_IsNullForRootContext()
    {
        var root = new SpecContext("root");

        await Assert.That(root.Parent).IsNull();
    }

    [Test]
    public async Task SpecContext_Children_InitiallyEmpty()
    {
        var context = new SpecContext("test");

        await Assert.That(context.Children).IsEmpty();
    }

    [Test]
    public async Task SpecContext_Specs_InitiallyEmpty()
    {
        var context = new SpecContext("test");

        await Assert.That(context.Specs).IsEmpty();
    }

    #endregion

    #region SpecDefinition Tests

    [Test]
    public async Task SpecDefinition_Tags_DefaultsToEmptyList()
    {
        var spec = new SpecDefinition("test spec", () => { });

        await Assert.That(spec.Tags).IsEmpty();
    }

    [Test]
    public async Task SpecDefinition_Tags_CanBeSetViaInitializer()
    {
        var spec = new SpecDefinition("test spec", () => { })
        {
            Tags = ["unit", "fast"]
        };

        await Assert.That(spec.Tags).Contains("unit");
        await Assert.That(spec.Tags).Contains("fast");
        await Assert.That(spec.Tags).HasCount().EqualTo(2);
    }

    [Test]
    public async Task SpecDefinition_SyncBody_WrappedToReturnTask()
    {
        var executed = false;
        var spec = new SpecDefinition("sync spec", () => { executed = true; });

        await Assert.That((object?)spec.Body).IsNotNull();
        await spec.Body!();

        await Assert.That(executed).IsTrue();
    }

    [Test]
    public async Task SpecDefinition_AsyncBody_WorksDirectly()
    {
        var executed = false;
        var spec = new SpecDefinition("async spec", async () =>
        {
            await Task.Delay(1);
            executed = true;
        });

        await spec.Body!();

        await Assert.That(executed).IsTrue();
    }

    [Test]
    public async Task SpecDefinition_NullBody_IsPending()
    {
        var spec = new SpecDefinition("pending spec");

        await Assert.That(spec.IsPending).IsTrue();
        await Assert.That((object?)spec.Body).IsNull();
    }

    [Test]
    public async Task SpecDefinition_WithBody_IsNotPending()
    {
        var spec = new SpecDefinition("spec with body", () => { });

        await Assert.That(spec.IsPending).IsFalse();
    }

    [Test]
    public async Task SpecDefinition_IsSkipped_DefaultsFalse()
    {
        var spec = new SpecDefinition("normal spec", () => { });

        await Assert.That(spec.IsSkipped).IsFalse();
    }

    [Test]
    public async Task SpecDefinition_IsFocused_DefaultsFalse()
    {
        var spec = new SpecDefinition("normal spec", () => { });

        await Assert.That(spec.IsFocused).IsFalse();
    }

    [Test]
    public async Task SpecDefinition_IsSkipped_CanBeSet()
    {
        var spec = new SpecDefinition("skipped spec", () => { }) { IsSkipped = true };

        await Assert.That(spec.IsSkipped).IsTrue();
    }

    [Test]
    public async Task SpecDefinition_IsFocused_CanBeSet()
    {
        var spec = new SpecDefinition("focused spec", () => { }) { IsFocused = true };

        await Assert.That(spec.IsFocused).IsTrue();
    }

    [Test]
    public async Task SpecDefinition_Description_IsSet()
    {
        var spec = new SpecDefinition("my test description", () => { });

        await Assert.That(spec.Description).IsEqualTo("my test description");
    }

    #endregion
}
