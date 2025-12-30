using DraftSpec.Cli;
using DraftSpec.Cli.Services;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for SpecStatsCollector service.
/// </summary>
public class SpecStatsCollectorTests
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_stats_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region Empty Input

    [Test]
    public async Task CollectAsync_WithNoFiles_ReturnsZeroStats()
    {
        var collector = new SpecStatsCollector();

        var stats = await collector.CollectAsync([], "/some/path");

        await Assert.That(stats.Total).IsEqualTo(0);
        await Assert.That(stats.Regular).IsEqualTo(0);
        await Assert.That(stats.Focused).IsEqualTo(0);
        await Assert.That(stats.Skipped).IsEqualTo(0);
        await Assert.That(stats.Pending).IsEqualTo(0);
        await Assert.That(stats.HasFocusMode).IsFalse();
        await Assert.That(stats.FileCount).IsEqualTo(0);
    }

    #endregion

    #region Real File Parsing

    [Test]
    public async Task CollectAsync_WithRegularSpecs_CountsCorrectly()
    {
        var collector = new SpecStatsCollector();
        var specFile = Path.Combine(_tempDir, "regular.spec.csx");

        await File.WriteAllTextAsync(specFile, """
            using static DraftSpec.Dsl;
            describe("Calculator", () =>
            {
                it("adds numbers", () => { });
                it("subtracts numbers", () => { });
                it("multiplies numbers", () => { });
            });
            """);

        var stats = await collector.CollectAsync([specFile], _tempDir);

        await Assert.That(stats.Total).IsEqualTo(3);
        await Assert.That(stats.Regular).IsEqualTo(3);
        await Assert.That(stats.Focused).IsEqualTo(0);
        await Assert.That(stats.Skipped).IsEqualTo(0);
        await Assert.That(stats.HasFocusMode).IsFalse();
        await Assert.That(stats.FileCount).IsEqualTo(1);
    }

    [Test]
    public async Task CollectAsync_WithFocusedSpecs_SetsHasFocusMode()
    {
        var collector = new SpecStatsCollector();
        var specFile = Path.Combine(_tempDir, "focused.spec.csx");

        await File.WriteAllTextAsync(specFile, """
            using static DraftSpec.Dsl;
            describe("Tests", () =>
            {
                it("regular spec", () => { });
                fit("focused spec", () => { });
                it("another regular", () => { });
            });
            """);

        var stats = await collector.CollectAsync([specFile], _tempDir);

        await Assert.That(stats.Total).IsEqualTo(3);
        await Assert.That(stats.Regular).IsEqualTo(2);
        await Assert.That(stats.Focused).IsEqualTo(1);
        await Assert.That(stats.HasFocusMode).IsTrue();
    }

    [Test]
    public async Task CollectAsync_WithSkippedSpecs_CountsSkipped()
    {
        var collector = new SpecStatsCollector();
        var specFile = Path.Combine(_tempDir, "skipped.spec.csx");

        await File.WriteAllTextAsync(specFile, """
            using static DraftSpec.Dsl;
            describe("Tests", () =>
            {
                it("regular spec", () => { });
                xit("skipped spec", () => { });
                xit("another skipped", () => { });
            });
            """);

        var stats = await collector.CollectAsync([specFile], _tempDir);

        await Assert.That(stats.Total).IsEqualTo(3);
        await Assert.That(stats.Regular).IsEqualTo(1);
        await Assert.That(stats.Skipped).IsEqualTo(2);
    }

    [Test]
    public async Task CollectAsync_WithPendingSpecs_CountsPending()
    {
        var collector = new SpecStatsCollector();
        var specFile = Path.Combine(_tempDir, "pending.spec.csx");

        await File.WriteAllTextAsync(specFile, """
            using static DraftSpec.Dsl;
            describe("Tests", () =>
            {
                it("regular spec", () => { });
                it("pending spec");
                it("another pending");
            });
            """);

        var stats = await collector.CollectAsync([specFile], _tempDir);

        await Assert.That(stats.Total).IsEqualTo(3);
        await Assert.That(stats.Pending).IsEqualTo(2);
    }

    [Test]
    public async Task CollectAsync_WithMixedSpecs_CountsAllTypes()
    {
        var collector = new SpecStatsCollector();
        var specFile = Path.Combine(_tempDir, "mixed.spec.csx");

        await File.WriteAllTextAsync(specFile, """
            using static DraftSpec.Dsl;
            describe("Tests", () =>
            {
                it("regular1", () => { });
                it("regular2", () => { });
                fit("focused1", () => { });
                fit("focused2", () => { });
                xit("skipped1", () => { });
                it("pending1");
            });
            """);

        var stats = await collector.CollectAsync([specFile], _tempDir);

        await Assert.That(stats.Total).IsEqualTo(6);
        await Assert.That(stats.Regular).IsEqualTo(3); // 2 regular + 1 pending (pending is regular type)
        await Assert.That(stats.Focused).IsEqualTo(2);
        await Assert.That(stats.Skipped).IsEqualTo(1);
        await Assert.That(stats.Pending).IsEqualTo(1);
        await Assert.That(stats.HasFocusMode).IsTrue();
    }

    [Test]
    public async Task CollectAsync_WithMultipleFiles_AggregatesStats()
    {
        var collector = new SpecStatsCollector();
        var file1 = Path.Combine(_tempDir, "file1.spec.csx");
        var file2 = Path.Combine(_tempDir, "file2.spec.csx");

        await File.WriteAllTextAsync(file1, """
            using static DraftSpec.Dsl;
            describe("File1", () =>
            {
                it("spec1", () => { });
                it("spec2", () => { });
            });
            """);

        await File.WriteAllTextAsync(file2, """
            using static DraftSpec.Dsl;
            describe("File2", () =>
            {
                it("spec3", () => { });
                fit("focused", () => { });
            });
            """);

        var stats = await collector.CollectAsync([file1, file2], _tempDir);

        await Assert.That(stats.Total).IsEqualTo(4);
        await Assert.That(stats.FileCount).IsEqualTo(2);
        await Assert.That(stats.Focused).IsEqualTo(1);
        await Assert.That(stats.HasFocusMode).IsTrue();
    }

    [Test]
    public async Task CollectAsync_WithNestedContexts_CountsAllSpecs()
    {
        var collector = new SpecStatsCollector();
        var specFile = Path.Combine(_tempDir, "nested.spec.csx");

        await File.WriteAllTextAsync(specFile, """
            using static DraftSpec.Dsl;
            describe("Outer", () =>
            {
                it("outer spec", () => { });

                describe("Inner", () =>
                {
                    it("inner spec 1", () => { });
                    it("inner spec 2", () => { });
                });
            });
            """);

        var stats = await collector.CollectAsync([specFile], _tempDir);

        await Assert.That(stats.Total).IsEqualTo(3);
    }

    [Test]
    public async Task CollectAsync_WithCancellation_ThrowsOperationCanceled()
    {
        var collector = new SpecStatsCollector();
        var specFile = Path.Combine(_tempDir, "test.spec.csx");

        await File.WriteAllTextAsync(specFile, """
            using static DraftSpec.Dsl;
            describe("Test", () => { it("spec", () => { }); });
            """);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var action = async () => await collector.CollectAsync([specFile], _tempDir, cts.Token);

        await Assert.That(action).Throws<OperationCanceledException>();
    }

    #endregion

    [Test]
    public async Task SpecStats_Record_HasCorrectProperties()
    {
        var stats = new SpecStats(
            Total: 10,
            Regular: 5,
            Focused: 2,
            Skipped: 1,
            Pending: 2,
            HasFocusMode: true,
            FileCount: 3);

        await Assert.That(stats.Total).IsEqualTo(10);
        await Assert.That(stats.Regular).IsEqualTo(5);
        await Assert.That(stats.Focused).IsEqualTo(2);
        await Assert.That(stats.Skipped).IsEqualTo(1);
        await Assert.That(stats.Pending).IsEqualTo(2);
        await Assert.That(stats.HasFocusMode).IsTrue();
        await Assert.That(stats.FileCount).IsEqualTo(3);
    }

    [Test]
    public async Task SpecStats_HasFocusMode_TrueWhenFocusedGreaterThanZero()
    {
        var statsWithFocus = new SpecStats(10, 8, 2, 0, 0, true, 1);
        var statsWithoutFocus = new SpecStats(10, 10, 0, 0, 0, false, 1);

        await Assert.That(statsWithFocus.HasFocusMode).IsTrue();
        await Assert.That(statsWithoutFocus.HasFocusMode).IsFalse();
    }
}
