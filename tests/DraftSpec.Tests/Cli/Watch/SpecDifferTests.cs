using DraftSpec.Cli.Watch;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Cli.Watch;

/// <summary>
/// Tests for the SpecDiffer class.
/// </summary>
public class SpecDifferTests
{
    private readonly SpecDiffer _differ = new();

    #region First Run (No Previous State)

    [Test]
    public async Task Diff_NewFile_AllSpecsAdded()
    {
        var newResult = new StaticParseResult
        {
            Specs =
            [
                CreateSpec("spec 1", 10),
                CreateSpec("spec 2", 20)
            ],
            IsComplete = true
        };

        var changes = _differ.Diff("/path/test.spec.csx", null, newResult, dependencyChanged: false);

        await Assert.That(changes.Changes.Count).IsEqualTo(2);
        await Assert.That(changes.Changes.All(c => c.ChangeType == SpecChangeType.Added)).IsTrue();
        await Assert.That(changes.HasChanges).IsTrue();
        await Assert.That(changes.RequiresFullRun).IsFalse();
    }

    #endregion

    #region No Changes

    [Test]
    public async Task Diff_NoChanges_EmptyChangeSet()
    {
        var spec = CreateSpec("spec 1", 10);

        var oldResult = new StaticParseResult { Specs = [spec], IsComplete = true };
        var newResult = new StaticParseResult { Specs = [spec], IsComplete = true };

        var changes = _differ.Diff("/path/test.spec.csx", oldResult, newResult, dependencyChanged: false);

        await Assert.That(changes.Changes).IsEmpty();
        await Assert.That(changes.HasChanges).IsFalse();
    }

    #endregion

    #region Spec Added

    [Test]
    public async Task Diff_SpecAdded_DetectsAddition()
    {
        var existingSpec = CreateSpec("existing", 10);
        var newSpec = CreateSpec("new spec", 20);

        var oldResult = new StaticParseResult { Specs = [existingSpec], IsComplete = true };
        var newResult = new StaticParseResult { Specs = [existingSpec, newSpec], IsComplete = true };

        var changes = _differ.Diff("/path/test.spec.csx", oldResult, newResult, dependencyChanged: false);

        await Assert.That(changes.Changes.Count).IsEqualTo(1);
        var added = changes.Changes.Single();
        await Assert.That(added.ChangeType).IsEqualTo(SpecChangeType.Added);
        await Assert.That(added.Description).IsEqualTo("new spec");
        await Assert.That(added.NewLineNumber).IsEqualTo(20);
    }

    #endregion

    #region Spec Modified

    [Test]
    public async Task Diff_SpecModified_DetectsLineNumberChange()
    {
        var oldSpec = CreateSpec("spec 1", 10);
        var newSpec = CreateSpec("spec 1", 15); // Line number changed

        var oldResult = new StaticParseResult { Specs = [oldSpec], IsComplete = true };
        var newResult = new StaticParseResult { Specs = [newSpec], IsComplete = true };

        var changes = _differ.Diff("/path/test.spec.csx", oldResult, newResult, dependencyChanged: false);

        await Assert.That(changes.Changes.Count).IsEqualTo(1);
        var modified = changes.Changes.Single();
        await Assert.That(modified.ChangeType).IsEqualTo(SpecChangeType.Modified);
        await Assert.That(modified.Description).IsEqualTo("spec 1");
        await Assert.That(modified.OldLineNumber).IsEqualTo(10);
        await Assert.That(modified.NewLineNumber).IsEqualTo(15);
    }

    [Test]
    public async Task Diff_SpecTypeChanged_DetectsModification()
    {
        var oldSpec = CreateSpec("spec 1", 10, StaticSpecType.Regular);
        var newSpec = CreateSpec("spec 1", 10, StaticSpecType.Focused);

        var oldResult = new StaticParseResult { Specs = [oldSpec], IsComplete = true };
        var newResult = new StaticParseResult { Specs = [newSpec], IsComplete = true };

        var changes = _differ.Diff("/path/test.spec.csx", oldResult, newResult, dependencyChanged: false);

        await Assert.That(changes.Changes.Count).IsEqualTo(1);
        await Assert.That(changes.Changes.Single().ChangeType).IsEqualTo(SpecChangeType.Modified);
    }

    [Test]
    public async Task Diff_SpecPendingChanged_DetectsModification()
    {
        var oldSpec = CreateSpec("spec 1", 10, isPending: false);
        var newSpec = CreateSpec("spec 1", 10, isPending: true);

        var oldResult = new StaticParseResult { Specs = [oldSpec], IsComplete = true };
        var newResult = new StaticParseResult { Specs = [newSpec], IsComplete = true };

        var changes = _differ.Diff("/path/test.spec.csx", oldResult, newResult, dependencyChanged: false);

        await Assert.That(changes.Changes.Count).IsEqualTo(1);
        await Assert.That(changes.Changes.Single().ChangeType).IsEqualTo(SpecChangeType.Modified);
    }

    #endregion

    #region Spec Deleted

    [Test]
    public async Task Diff_SpecDeleted_DetectsDeletion()
    {
        var spec1 = CreateSpec("spec 1", 10);
        var spec2 = CreateSpec("spec 2", 20);

        var oldResult = new StaticParseResult { Specs = [spec1, spec2], IsComplete = true };
        var newResult = new StaticParseResult { Specs = [spec1], IsComplete = true };

        var changes = _differ.Diff("/path/test.spec.csx", oldResult, newResult, dependencyChanged: false);

        await Assert.That(changes.Changes.Count).IsEqualTo(1);
        var deleted = changes.Changes.Single();
        await Assert.That(deleted.ChangeType).IsEqualTo(SpecChangeType.Deleted);
        await Assert.That(deleted.Description).IsEqualTo("spec 2");
        await Assert.That(deleted.OldLineNumber).IsEqualTo(20);
    }

    #endregion

    #region Dynamic Specs

    [Test]
    public async Task Diff_DynamicSpecs_RequiresFullRun()
    {
        var oldResult = new StaticParseResult { Specs = [], IsComplete = true };
        var newResult = new StaticParseResult { Specs = [], IsComplete = false }; // Dynamic specs detected

        var changes = _differ.Diff("/path/test.spec.csx", oldResult, newResult, dependencyChanged: false);

        await Assert.That(changes.HasDynamicSpecs).IsTrue();
        await Assert.That(changes.RequiresFullRun).IsTrue();
    }

    [Test]
    public async Task Diff_OldResultHadDynamicSpecs_RequiresFullRun()
    {
        var oldResult = new StaticParseResult { Specs = [], IsComplete = false }; // Dynamic specs
        var newResult = new StaticParseResult { Specs = [], IsComplete = true };

        var changes = _differ.Diff("/path/test.spec.csx", oldResult, newResult, dependencyChanged: false);

        await Assert.That(changes.HasDynamicSpecs).IsTrue();
        await Assert.That(changes.RequiresFullRun).IsTrue();
    }

    #endregion

    #region Dependency Changed

    [Test]
    public async Task Diff_DependencyChanged_RequiresFullRun()
    {
        var spec = CreateSpec("spec 1", 10);
        var oldResult = new StaticParseResult { Specs = [spec], IsComplete = true };
        var newResult = new StaticParseResult { Specs = [spec], IsComplete = true };

        var changes = _differ.Diff("/path/test.spec.csx", oldResult, newResult, dependencyChanged: true);

        await Assert.That(changes.DependencyChanged).IsTrue();
        await Assert.That(changes.RequiresFullRun).IsTrue();
    }

    #endregion

    #region Multiple Changes

    [Test]
    public async Task Diff_MultipleChanges_TracksAll()
    {
        var spec1 = CreateSpec("unchanged", 10);
        var spec2 = CreateSpec("modified", 20);
        var spec3 = CreateSpec("deleted", 30);

        var newSpec2 = CreateSpec("modified", 25); // Line changed
        var spec4 = CreateSpec("added", 40);

        var oldResult = new StaticParseResult { Specs = [spec1, spec2, spec3], IsComplete = true };
        var newResult = new StaticParseResult { Specs = [spec1, newSpec2, spec4], IsComplete = true };

        var changes = _differ.Diff("/path/test.spec.csx", oldResult, newResult, dependencyChanged: false);

        await Assert.That(changes.Changes.Count).IsEqualTo(3);
        await Assert.That(changes.Changes.Count(c => c.ChangeType == SpecChangeType.Added)).IsEqualTo(1);
        await Assert.That(changes.Changes.Count(c => c.ChangeType == SpecChangeType.Modified)).IsEqualTo(1);
        await Assert.That(changes.Changes.Count(c => c.ChangeType == SpecChangeType.Deleted)).IsEqualTo(1);
    }

    #endregion

    #region SpecsToRun

    [Test]
    public async Task SpecsToRun_ExcludesDeleted()
    {
        var spec1 = CreateSpec("kept", 10);
        var spec2 = CreateSpec("deleted", 20);

        var oldResult = new StaticParseResult { Specs = [spec1, spec2], IsComplete = true };
        var newResult = new StaticParseResult { Specs = [spec1], IsComplete = true };

        var changes = _differ.Diff("/path/test.spec.csx", oldResult, newResult, dependencyChanged: false);

        await Assert.That(changes.SpecsToRun).IsEmpty();
    }

    [Test]
    public async Task SpecsToRun_IncludesAddedAndModified()
    {
        var spec1 = CreateSpec("modified", 10);
        var newSpec1 = CreateSpec("modified", 15);
        var spec2 = CreateSpec("added", 20);

        var oldResult = new StaticParseResult { Specs = [spec1], IsComplete = true };
        var newResult = new StaticParseResult { Specs = [newSpec1, spec2], IsComplete = true };

        var changes = _differ.Diff("/path/test.spec.csx", oldResult, newResult, dependencyChanged: false);

        await Assert.That(changes.SpecsToRun.Count).IsEqualTo(2);
    }

    #endregion

    #region Nested Context

    [Test]
    public async Task Diff_NestedContext_IdentifiesByFullPath()
    {
        var spec1 = CreateSpec("same name", 10, contextPath: ["Context1"]);
        var spec2 = CreateSpec("same name", 20, contextPath: ["Context2"]); // Same name, different context

        var oldResult = new StaticParseResult { Specs = [spec1], IsComplete = true };
        var newResult = new StaticParseResult { Specs = [spec1, spec2], IsComplete = true };

        var changes = _differ.Diff("/path/test.spec.csx", oldResult, newResult, dependencyChanged: false);

        await Assert.That(changes.Changes.Count).IsEqualTo(1);
        await Assert.That(changes.Changes.Single().ChangeType).IsEqualTo(SpecChangeType.Added);
        await Assert.That(changes.Changes.Single().ContextPath).Contains("Context2");
    }

    #endregion

    #region Helpers

    private static StaticSpec CreateSpec(
        string description,
        int lineNumber,
        StaticSpecType type = StaticSpecType.Regular,
        bool isPending = false,
        string[]? contextPath = null)
    {
        return new StaticSpec
        {
            Description = description,
            LineNumber = lineNumber,
            Type = type,
            IsPending = isPending,
            ContextPath = contextPath ?? ["describe"]
        };
    }

    #endregion
}
