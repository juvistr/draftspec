using DraftSpec.Coverage;

namespace DraftSpec.Tests.Coverage;

/// <summary>
/// Tests for InProcessCoverageTracker.
/// </summary>
public class InProcessCoverageTrackerTests
{
    #region Start/Stop

    [Test]
    public async Task Start_SetsIsActiveTrue()
    {
        using var tracker = new InProcessCoverageTracker();

        tracker.Start();

        await Assert.That(tracker.IsActive).IsTrue();
    }

    [Test]
    public async Task Stop_SetsIsActiveFalse()
    {
        using var tracker = new InProcessCoverageTracker();
        tracker.Start();

        tracker.StopTracking();

        await Assert.That(tracker.IsActive).IsFalse();
    }

    [Test]
    public async Task IsActive_DefaultsFalse()
    {
        using var tracker = new InProcessCoverageTracker();

        await Assert.That(tracker.IsActive).IsFalse();
    }

    #endregion

    #region RecordLineHit

    [Test]
    public async Task RecordLineHit_WhenActive_RecordsHit()
    {
        using var tracker = new InProcessCoverageTracker();
        tracker.Start();

        tracker.RecordLineHit("test.cs", 10);
        tracker.RecordLineHit("test.cs", 10);
        tracker.RecordLineHit("test.cs", 20);

        var coverage = tracker.GetTotalCoverage();
        await Assert.That(coverage).ContainsKey("test.cs");
        await Assert.That(coverage["test.cs"].LineHits[10]).IsEqualTo(2);
        await Assert.That(coverage["test.cs"].LineHits[20]).IsEqualTo(1);
    }

    [Test]
    public async Task RecordLineHit_WhenInactive_DoesNotRecord()
    {
        using var tracker = new InProcessCoverageTracker();
        // Not started

        tracker.RecordLineHit("test.cs", 10);

        var coverage = tracker.GetTotalCoverage();
        await Assert.That(coverage).IsEmpty();
    }

    [Test]
    public async Task RecordLineHit_MultipleFiles_TracksAll()
    {
        using var tracker = new InProcessCoverageTracker();
        tracker.Start();

        tracker.RecordLineHit("file1.cs", 10);
        tracker.RecordLineHit("file2.cs", 20);
        tracker.RecordLineHit("file3.cs", 30);

        var coverage = tracker.GetTotalCoverage();
        await Assert.That(coverage).Count().IsEqualTo(3);
    }

    #endregion

    #region Snapshots and Deltas

    [Test]
    public async Task TakeSnapshot_ReturnsUniqueId()
    {
        using var tracker = new InProcessCoverageTracker();

        var snapshot1 = tracker.TakeSnapshot();
        var snapshot2 = tracker.TakeSnapshot();

        await Assert.That(snapshot1.Id).IsNotEqualTo(snapshot2.Id);
    }

    [Test]
    public async Task GetCoverageSince_ReturnsDelta()
    {
        using var tracker = new InProcessCoverageTracker();
        tracker.Start();

        // Record some initial coverage
        tracker.RecordLineHit("test.cs", 1);
        tracker.RecordLineHit("test.cs", 2);

        // Take snapshot
        var snapshot = tracker.TakeSnapshot();

        // Record more coverage
        tracker.RecordLineHit("test.cs", 3);
        tracker.RecordLineHit("test.cs", 4);

        // Get delta
        var delta = tracker.GetCoverageSince(snapshot, "spec-1");

        await Assert.That(delta.SpecId).IsEqualTo("spec-1");
        await Assert.That(delta.Summary.LinesCovered).IsEqualTo(2); // Lines 3 and 4
        await Assert.That(delta.FilesCovered["test.cs"].LineHits).ContainsKey(3);
        await Assert.That(delta.FilesCovered["test.cs"].LineHits).ContainsKey(4);
    }

    [Test]
    public async Task GetCoverageSince_ExcludesPreExistingCoverage()
    {
        using var tracker = new InProcessCoverageTracker();
        tracker.Start();

        tracker.RecordLineHit("test.cs", 1);
        var snapshot = tracker.TakeSnapshot();
        // No new coverage after snapshot

        var delta = tracker.GetCoverageSince(snapshot, "spec-1");

        await Assert.That(delta.FilesCovered).IsEmpty();
        await Assert.That(delta.Summary.LinesCovered).IsEqualTo(0);
    }

    [Test]
    public async Task GetCoverageSince_HandlesNewFiles()
    {
        using var tracker = new InProcessCoverageTracker();
        tracker.Start();

        var snapshot = tracker.TakeSnapshot();
        tracker.RecordLineHit("newfile.cs", 100);

        var delta = tracker.GetCoverageSince(snapshot, "spec-1");

        await Assert.That(delta.FilesCovered).ContainsKey("newfile.cs");
        await Assert.That(delta.Summary.FilesTouched).IsEqualTo(1);
    }

    #endregion

    #region Branch Coverage

    [Test]
    public async Task RecordBranchHit_TracksBranchCoverage()
    {
        using var tracker = new InProcessCoverageTracker();
        tracker.Start();

        // A branch hit typically comes with a line hit (the line containing the branch)
        tracker.RecordLineHit("test.cs", 10);
        tracker.RecordBranchHit("test.cs", 10, 2, 4);

        var coverage = tracker.GetTotalCoverage()!;
        await Assert.That(coverage["test.cs"].BranchHits!).IsNotNull();
        await Assert.That(coverage["test.cs"].BranchHits![10].Covered).IsEqualTo(2);
        await Assert.That(coverage["test.cs"].BranchHits![10].Total).IsEqualTo(4);
    }

    #endregion

    #region Reset

    [Test]
    public async Task Reset_ClearsAllCoverage()
    {
        using var tracker = new InProcessCoverageTracker();
        tracker.Start();
        tracker.RecordLineHit("test.cs", 1);
        tracker.RecordLineHit("test.cs", 2);

        tracker.Reset();

        var coverage = tracker.GetTotalCoverage();
        await Assert.That(coverage).IsEmpty();
    }

    #endregion

    #region Thread Safety

    [Test]
    public async Task RecordLineHit_ThreadSafe()
    {
        using var tracker = new InProcessCoverageTracker();
        tracker.Start();

        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() =>
            {
                for (var j = 0; j < 100; j++)
                {
                    tracker.RecordLineHit($"file{i}.cs", j);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        var coverage = tracker.GetTotalCoverage();
        await Assert.That(coverage).Count().IsEqualTo(100); // 100 files
    }

    #endregion

    #region Dispose

    [Test]
    public async Task Dispose_StopsTracking()
    {
        var tracker = new InProcessCoverageTracker();
        tracker.Start();

        tracker.Dispose();

        await Assert.That(tracker.IsActive).IsFalse();
    }

    [Test]
    public void Dispose_ThrowsOnSubsequentCalls()
    {
        var tracker = new InProcessCoverageTracker();
        tracker.Dispose();

        Assert.Throws<ObjectDisposedException>(() => tracker.Start());
    }

    #endregion
}
