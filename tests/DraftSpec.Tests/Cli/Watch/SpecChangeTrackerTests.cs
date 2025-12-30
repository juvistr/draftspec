using DraftSpec.Cli.Watch;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Cli.Watch;

/// <summary>
/// Tests for the SpecChangeTracker class.
/// </summary>
public class SpecChangeTrackerTests
{
    #region RecordState

    [Test]
    public async Task RecordState_StoresParseResult()
    {
        var tracker = new SpecChangeTracker();
        var parseResult = CreateParseResult(CreateSpec("test", 10));

        tracker.RecordState("/path/test.spec.csx", parseResult);

        await Assert.That(tracker.HasState("/path/test.spec.csx")).IsTrue();
    }

    [Test]
    public async Task RecordState_OverwritesPreviousState()
    {
        var tracker = new SpecChangeTracker();
        var firstResult = CreateParseResult(CreateSpec("first", 10));
        var secondResult = CreateParseResult(CreateSpec("second", 20));

        tracker.RecordState("/path/test.spec.csx", firstResult);
        tracker.RecordState("/path/test.spec.csx", secondResult);

        // Get changes with the first result - should show it as added since we overwrote with second
        var changes = tracker.GetChanges("/path/test.spec.csx", firstResult, dependencyChanged: false);

        // Should detect the change from second to first
        await Assert.That(changes.Changes.Count).IsEqualTo(2); // first added, second deleted
    }

    #endregion

    #region GetChanges

    [Test]
    public async Task GetChanges_FirstRun_ReturnsAllAsAdded()
    {
        var tracker = new SpecChangeTracker();
        var parseResult = CreateParseResult(
            CreateSpec("spec 1", 10),
            CreateSpec("spec 2", 20));

        var changes = tracker.GetChanges("/path/test.spec.csx", parseResult, dependencyChanged: false);

        await Assert.That(changes.Changes.Count).IsEqualTo(2);
        await Assert.That(changes.Changes.All(c => c.ChangeType == SpecChangeType.Added)).IsTrue();
    }

    [Test]
    public async Task GetChanges_AfterRecord_DetectsChanges()
    {
        var tracker = new SpecChangeTracker();
        var originalSpec = CreateSpec("spec 1", 10);
        var modifiedSpec = CreateSpec("spec 1", 15);

        // Record initial state
        tracker.RecordState("/path/test.spec.csx", CreateParseResult(originalSpec));

        // Get changes with modified state
        var changes = tracker.GetChanges("/path/test.spec.csx", CreateParseResult(modifiedSpec), dependencyChanged: false);

        await Assert.That(changes.Changes.Count).IsEqualTo(1);
        await Assert.That(changes.Changes.Single().ChangeType).IsEqualTo(SpecChangeType.Modified);
    }

    [Test]
    public async Task GetChanges_NoChanges_ReturnsEmptySet()
    {
        var tracker = new SpecChangeTracker();
        var spec = CreateSpec("spec 1", 10);
        var parseResult = CreateParseResult(spec);

        tracker.RecordState("/path/test.spec.csx", parseResult);
        var changes = tracker.GetChanges("/path/test.spec.csx", parseResult, dependencyChanged: false);

        await Assert.That(changes.Changes).IsEmpty();
        await Assert.That(changes.HasChanges).IsFalse();
    }

    #endregion

    #region HasState

    [Test]
    public async Task HasState_NoState_ReturnsFalse()
    {
        var tracker = new SpecChangeTracker();

        await Assert.That(tracker.HasState("/path/unknown.spec.csx")).IsFalse();
    }

    [Test]
    public async Task HasState_CaseInsensitive()
    {
        var tracker = new SpecChangeTracker();
        tracker.RecordState("/Path/Test.spec.csx", CreateParseResult());

        await Assert.That(tracker.HasState("/path/test.spec.csx")).IsTrue();
    }

    #endregion

    #region Clear

    [Test]
    public async Task Clear_RemovesAllState()
    {
        var tracker = new SpecChangeTracker();
        tracker.RecordState("/path/test1.spec.csx", CreateParseResult());
        tracker.RecordState("/path/test2.spec.csx", CreateParseResult());

        tracker.Clear();

        await Assert.That(tracker.HasState("/path/test1.spec.csx")).IsFalse();
        await Assert.That(tracker.HasState("/path/test2.spec.csx")).IsFalse();
    }

    [Test]
    public async Task Clear_RemovesDependencies()
    {
        var tracker = new SpecChangeTracker();
        var timestamp = DateTime.UtcNow;
        tracker.RecordDependency("/path/spec_helper.csx", timestamp);

        tracker.Clear();

        // After clear, the same timestamp should be considered as changed (new)
        await Assert.That(tracker.HasDependencyChanged("/path/spec_helper.csx", timestamp)).IsTrue();
    }

    #endregion

    #region Dependency Tracking

    [Test]
    public async Task HasDependencyChanged_NewDependency_ReturnsTrue()
    {
        var tracker = new SpecChangeTracker();
        var timestamp = DateTime.UtcNow;

        await Assert.That(tracker.HasDependencyChanged("/path/spec_helper.csx", timestamp)).IsTrue();
    }

    [Test]
    public async Task HasDependencyChanged_SameTimestamp_ReturnsFalse()
    {
        var tracker = new SpecChangeTracker();
        var timestamp = DateTime.UtcNow;

        tracker.RecordDependency("/path/spec_helper.csx", timestamp);

        await Assert.That(tracker.HasDependencyChanged("/path/spec_helper.csx", timestamp)).IsFalse();
    }

    [Test]
    public async Task HasDependencyChanged_NewerTimestamp_ReturnsTrue()
    {
        var tracker = new SpecChangeTracker();
        var oldTimestamp = DateTime.UtcNow;
        var newTimestamp = oldTimestamp.AddSeconds(1);

        tracker.RecordDependency("/path/spec_helper.csx", oldTimestamp);

        await Assert.That(tracker.HasDependencyChanged("/path/spec_helper.csx", newTimestamp)).IsTrue();
    }

    [Test]
    public async Task HasDependencyChanged_OlderTimestamp_ReturnsFalse()
    {
        var tracker = new SpecChangeTracker();
        var newTimestamp = DateTime.UtcNow;
        var oldTimestamp = newTimestamp.AddSeconds(-1);

        tracker.RecordDependency("/path/spec_helper.csx", newTimestamp);

        await Assert.That(tracker.HasDependencyChanged("/path/spec_helper.csx", oldTimestamp)).IsFalse();
    }

    [Test]
    public async Task RecordDependency_UpdatesExisting()
    {
        var tracker = new SpecChangeTracker();
        var oldTimestamp = DateTime.UtcNow;
        var newTimestamp = oldTimestamp.AddSeconds(1);

        tracker.RecordDependency("/path/spec_helper.csx", oldTimestamp);
        tracker.RecordDependency("/path/spec_helper.csx", newTimestamp);

        await Assert.That(tracker.HasDependencyChanged("/path/spec_helper.csx", newTimestamp)).IsFalse();
    }

    [Test]
    public async Task HasDependencyChanged_CaseInsensitive()
    {
        var tracker = new SpecChangeTracker();
        var timestamp = DateTime.UtcNow;

        tracker.RecordDependency("/Path/Spec_Helper.csx", timestamp);

        await Assert.That(tracker.HasDependencyChanged("/path/spec_helper.csx", timestamp)).IsFalse();
    }

    #endregion

    #region Helpers

    private static StaticSpec CreateSpec(
        string description,
        int lineNumber,
        StaticSpecType type = StaticSpecType.Regular)
    {
        return new StaticSpec
        {
            Description = description,
            LineNumber = lineNumber,
            Type = type,
            IsPending = false,
            ContextPath = ["describe"]
        };
    }

    private static StaticParseResult CreateParseResult(params StaticSpec[] specs)
    {
        return new StaticParseResult
        {
            Specs = specs,
            IsComplete = true
        };
    }

    #endregion
}
