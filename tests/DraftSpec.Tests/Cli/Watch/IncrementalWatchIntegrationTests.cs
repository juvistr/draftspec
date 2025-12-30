using DraftSpec.Cli.Watch;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Cli.Watch;

/// <summary>
/// Integration tests for the incremental watch mode components working together.
/// </summary>
public class IncrementalWatchIntegrationTests
{
    #region End-to-End Change Detection

    [Test]
    public async Task ChangeTracker_WithDiffer_DetectsMultipleChangeTypes()
    {
        // Arrange: Set up tracker with initial state
        var tracker = new SpecChangeTracker();
        const string filePath = "/project/specs/feature.spec.csx";

        var originalState = new StaticParseResult
        {
            Specs =
            [
                CreateSpec("spec to keep", 10),
                CreateSpec("spec to modify", 20),
                CreateSpec("spec to delete", 30)
            ],
            IsComplete = true
        };

        tracker.RecordState(filePath, originalState);

        // Act: Simulate file changes
        var modifiedState = new StaticParseResult
        {
            Specs =
            [
                CreateSpec("spec to keep", 10),        // unchanged
                CreateSpec("spec to modify", 25),      // line changed (modified)
                CreateSpec("new spec", 40)             // added
                // "spec to delete" removed
            ],
            IsComplete = true
        };

        var changes = tracker.GetChanges(filePath, modifiedState, dependencyChanged: false);

        // Assert: Verify all change types detected
        await Assert.That(changes.HasChanges).IsTrue();
        await Assert.That(changes.RequiresFullRun).IsFalse();
        await Assert.That(changes.Changes.Count).IsEqualTo(3);

        var added = changes.Changes.FirstOrDefault(c => c.ChangeType == SpecChangeType.Added);
        var modified = changes.Changes.FirstOrDefault(c => c.ChangeType == SpecChangeType.Modified);
        var deleted = changes.Changes.FirstOrDefault(c => c.ChangeType == SpecChangeType.Deleted);

        await Assert.That(added).IsNotNull();
        await Assert.That(added!.Description).IsEqualTo("new spec");

        await Assert.That(modified).IsNotNull();
        await Assert.That(modified!.Description).IsEqualTo("spec to modify");

        await Assert.That(deleted).IsNotNull();
        await Assert.That(deleted!.Description).IsEqualTo("spec to delete");

        // Only added and modified should be in SpecsToRun (not deleted)
        await Assert.That(changes.SpecsToRun.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ChangeTracker_WithDynamicSpecs_RequiresFullRun()
    {
        // Arrange
        var tracker = new SpecChangeTracker();
        const string filePath = "/project/specs/dynamic.spec.csx";

        var initialState = new StaticParseResult
        {
            Specs = [CreateSpec("static spec", 10)],
            IsComplete = true
        };

        tracker.RecordState(filePath, initialState);

        // Act: New state has dynamic specs (loops, interpolated strings, etc.)
        var dynamicState = new StaticParseResult
        {
            Specs = [CreateSpec("static spec", 10)],
            IsComplete = false // Dynamic specs detected
        };

        var changes = tracker.GetChanges(filePath, dynamicState, dependencyChanged: false);

        // Assert: Should require full run despite no individual spec changes
        await Assert.That(changes.HasDynamicSpecs).IsTrue();
        await Assert.That(changes.RequiresFullRun).IsTrue();
        await Assert.That(changes.Changes).IsEmpty(); // No specific changes tracked
    }

    [Test]
    public async Task ChangeTracker_WithDependencyChange_RequiresFullRun()
    {
        // Arrange
        var tracker = new SpecChangeTracker();
        const string filePath = "/project/specs/uses_helper.spec.csx";

        var initialState = new StaticParseResult
        {
            Specs = [CreateSpec("test spec", 10)],
            IsComplete = true
        };

        tracker.RecordState(filePath, initialState);

        // Act: Same file content but dependency (spec_helper.csx) changed
        var changes = tracker.GetChanges(filePath, initialState, dependencyChanged: true);

        // Assert: Should require full run
        await Assert.That(changes.DependencyChanged).IsTrue();
        await Assert.That(changes.RequiresFullRun).IsTrue();
    }

    #endregion

    #region State Persistence

    [Test]
    public async Task ChangeTracker_AfterRecordingNewState_DetectsSubsequentChanges()
    {
        // Arrange
        var tracker = new SpecChangeTracker();
        const string filePath = "/project/specs/evolving.spec.csx";

        // First state
        var state1 = new StaticParseResult
        {
            Specs = [CreateSpec("spec 1", 10)],
            IsComplete = true
        };
        tracker.RecordState(filePath, state1);

        // Second state (add spec 2)
        var state2 = new StaticParseResult
        {
            Specs = [CreateSpec("spec 1", 10), CreateSpec("spec 2", 20)],
            IsComplete = true
        };

        var changes1 = tracker.GetChanges(filePath, state2, dependencyChanged: false);
        await Assert.That(changes1.Changes.Count).IsEqualTo(1);
        await Assert.That(changes1.Changes[0].ChangeType).IsEqualTo(SpecChangeType.Added);

        // Record state2
        tracker.RecordState(filePath, state2);

        // Third state (modify spec 2)
        var state3 = new StaticParseResult
        {
            Specs = [CreateSpec("spec 1", 10), CreateSpec("spec 2", 25)],
            IsComplete = true
        };

        var changes2 = tracker.GetChanges(filePath, state3, dependencyChanged: false);

        // Assert: Should detect modification of spec 2
        await Assert.That(changes2.Changes.Count).IsEqualTo(1);
        await Assert.That(changes2.Changes[0].ChangeType).IsEqualTo(SpecChangeType.Modified);
        await Assert.That(changes2.Changes[0].Description).IsEqualTo("spec 2");
    }

    [Test]
    public async Task ChangeTracker_MultipleFiles_TracksIndependently()
    {
        // Arrange
        var tracker = new SpecChangeTracker();

        var file1 = "/project/specs/file1.spec.csx";
        var file2 = "/project/specs/file2.spec.csx";

        var state1 = new StaticParseResult
        {
            Specs = [CreateSpec("spec in file 1", 10)],
            IsComplete = true
        };

        var state2 = new StaticParseResult
        {
            Specs = [CreateSpec("spec in file 2", 10)],
            IsComplete = true
        };

        tracker.RecordState(file1, state1);
        tracker.RecordState(file2, state2);

        // Act: Modify only file1
        var modifiedState1 = new StaticParseResult
        {
            Specs = [CreateSpec("spec in file 1", 15)],
            IsComplete = true
        };

        var changesFile1 = tracker.GetChanges(file1, modifiedState1, dependencyChanged: false);
        var changesFile2 = tracker.GetChanges(file2, state2, dependencyChanged: false);

        // Assert: Only file1 should have changes
        await Assert.That(changesFile1.HasChanges).IsTrue();
        await Assert.That(changesFile2.HasChanges).IsFalse();
    }

    #endregion

    #region Dependency Tracking Integration

    [Test]
    public async Task ChangeTracker_DependencyTracking_WorksAcrossRuns()
    {
        // Arrange
        var tracker = new SpecChangeTracker();
        var helperPath = "/project/spec_helper.csx";
        var initialTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // First run: record dependency
        tracker.RecordDependency(helperPath, initialTime);

        // Check with same timestamp
        await Assert.That(tracker.HasDependencyChanged(helperPath, initialTime)).IsFalse();

        // Check with newer timestamp (file was modified)
        var laterTime = initialTime.AddMinutes(5);
        await Assert.That(tracker.HasDependencyChanged(helperPath, laterTime)).IsTrue();

        // Update the recorded time
        tracker.RecordDependency(helperPath, laterTime);

        // Now the later time should not be considered changed
        await Assert.That(tracker.HasDependencyChanged(helperPath, laterTime)).IsFalse();
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task ChangeTracker_FirstRun_AllSpecsAddedButCanBeIncremental()
    {
        // Arrange
        var tracker = new SpecChangeTracker();
        const string filePath = "/project/specs/new_file.spec.csx";

        var newFile = new StaticParseResult
        {
            Specs =
            [
                CreateSpec("new spec 1", 10),
                CreateSpec("new spec 2", 20)
            ],
            IsComplete = true
        };

        // Act: First run has no prior state
        var changes = tracker.GetChanges(filePath, newFile, dependencyChanged: false);

        // Assert: All specs are "added" but this is still incremental-capable
        await Assert.That(changes.HasChanges).IsTrue();
        await Assert.That(changes.RequiresFullRun).IsFalse();
        await Assert.That(changes.Changes.Count).IsEqualTo(2);
        await Assert.That(changes.Changes.All(c => c.ChangeType == SpecChangeType.Added)).IsTrue();
    }

    [Test]
    public async Task ChangeTracker_NestedContextChanges_DetectedCorrectly()
    {
        // Arrange
        var tracker = new SpecChangeTracker();
        const string filePath = "/project/specs/nested.spec.csx";

        var initial = new StaticParseResult
        {
            Specs =
            [
                CreateSpec("shared name", 10, ["Parent", "Child1"]),
                CreateSpec("shared name", 20, ["Parent", "Child2"])
            ],
            IsComplete = true
        };

        tracker.RecordState(filePath, initial);

        // Act: Modify only the second "shared name" spec
        var modified = new StaticParseResult
        {
            Specs =
            [
                CreateSpec("shared name", 10, ["Parent", "Child1"]),   // unchanged
                CreateSpec("shared name", 25, ["Parent", "Child2"])    // line changed
            ],
            IsComplete = true
        };

        var changes = tracker.GetChanges(filePath, modified, dependencyChanged: false);

        // Assert: Only one spec should be modified
        await Assert.That(changes.Changes.Count).IsEqualTo(1);
        var change = changes.Changes[0];
        await Assert.That(change.ChangeType).IsEqualTo(SpecChangeType.Modified);
        await Assert.That(change.ContextPath).Contains("Child2");
    }

    #endregion

    #region Helpers

    private static StaticSpec CreateSpec(
        string description,
        int lineNumber,
        string[]? contextPath = null)
    {
        return new StaticSpec
        {
            Description = description,
            LineNumber = lineNumber,
            Type = StaticSpecType.Regular,
            IsPending = false,
            ContextPath = contextPath ?? ["describe"]
        };
    }

    #endregion
}
