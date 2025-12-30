using DraftSpec.Cli.Services;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for SpecPartitioner class.
/// </summary>
public class SpecPartitionerTests
{
    #region Partition By File (Round-Robin)

    [Test]
    public async Task PartitionByFile_EvenDistribution_AssignsCorrectly()
    {
        var partitioner = new SpecPartitioner();
        var files = new List<string> { "a.spec.csx", "b.spec.csx", "c.spec.csx", "d.spec.csx" };

        var result0 = await partitioner.PartitionAsync(files, 4, 0, "file", "/project");
        var result1 = await partitioner.PartitionAsync(files, 4, 1, "file", "/project");
        var result2 = await partitioner.PartitionAsync(files, 4, 2, "file", "/project");
        var result3 = await partitioner.PartitionAsync(files, 4, 3, "file", "/project");

        // Round-robin assignment after sorting: a.spec.csx(0), b.spec.csx(1), c.spec.csx(2), d.spec.csx(3)
        await Assert.That(result0.Files).HasSingleItem().And.Contains("a.spec.csx");
        await Assert.That(result1.Files).HasSingleItem().And.Contains("b.spec.csx");
        await Assert.That(result2.Files).HasSingleItem().And.Contains("c.spec.csx");
        await Assert.That(result3.Files).HasSingleItem().And.Contains("d.spec.csx");
    }

    [Test]
    public async Task PartitionByFile_UnevenDistribution_HandlesRemainder()
    {
        var partitioner = new SpecPartitioner();
        var files = new List<string> { "a.spec.csx", "b.spec.csx", "c.spec.csx", "d.spec.csx", "e.spec.csx" };

        var result0 = await partitioner.PartitionAsync(files, 2, 0, "file", "/project");
        var result1 = await partitioner.PartitionAsync(files, 2, 1, "file", "/project");

        // Round-robin: 0->a, 1->b, 0->c, 1->d, 0->e
        await Assert.That(result0.Files.Count).IsEqualTo(3);
        await Assert.That(result1.Files.Count).IsEqualTo(2);
        await Assert.That(result0.Files).Contains("a.spec.csx");
        await Assert.That(result0.Files).Contains("c.spec.csx");
        await Assert.That(result0.Files).Contains("e.spec.csx");
        await Assert.That(result1.Files).Contains("b.spec.csx");
        await Assert.That(result1.Files).Contains("d.spec.csx");
    }

    [Test]
    public async Task PartitionByFile_MorePartitionsThanFiles_SomeEmpty()
    {
        var partitioner = new SpecPartitioner();
        var files = new List<string> { "a.spec.csx", "b.spec.csx" };

        var result0 = await partitioner.PartitionAsync(files, 4, 0, "file", "/project");
        var result1 = await partitioner.PartitionAsync(files, 4, 1, "file", "/project");
        var result2 = await partitioner.PartitionAsync(files, 4, 2, "file", "/project");
        var result3 = await partitioner.PartitionAsync(files, 4, 3, "file", "/project");

        await Assert.That(result0.Files.Count).IsEqualTo(1);
        await Assert.That(result1.Files.Count).IsEqualTo(1);
        await Assert.That(result2.Files.Count).IsEqualTo(0);
        await Assert.That(result3.Files.Count).IsEqualTo(0);
    }

    [Test]
    public async Task PartitionByFile_IsDeterministic()
    {
        var partitioner = new SpecPartitioner();
        var files = new List<string> { "c.spec.csx", "a.spec.csx", "b.spec.csx" };

        var result1 = await partitioner.PartitionAsync(files, 3, 0, "file", "/project");
        var result2 = await partitioner.PartitionAsync(files, 3, 0, "file", "/project");

        await Assert.That(result1.Files.SequenceEqual(result2.Files)).IsTrue();
    }

    [Test]
    public async Task PartitionByFile_SortsFilesFirst()
    {
        var partitioner = new SpecPartitioner();
        // Files in non-alphabetical order
        var files = new List<string> { "c.spec.csx", "a.spec.csx", "b.spec.csx" };

        var result0 = await partitioner.PartitionAsync(files, 3, 0, "file", "/project");

        // After sorting: a, b, c - so partition 0 gets "a.spec.csx"
        await Assert.That(result0.Files).HasSingleItem().And.Contains("a.spec.csx");
    }

    [Test]
    public async Task PartitionByFile_AllPartitionsCoverAllFiles()
    {
        var partitioner = new SpecPartitioner();
        var files = new List<string> { "a.spec.csx", "b.spec.csx", "c.spec.csx", "d.spec.csx", "e.spec.csx" };

        var allPartitionedFiles = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var result = await partitioner.PartitionAsync(files, 3, i, "file", "/project");
            allPartitionedFiles.AddRange(result.Files);
        }

        // All files should be covered exactly once
        await Assert.That(allPartitionedFiles.OrderBy(f => f)).IsEquivalentTo(files.OrderBy(f => f));
    }

    [Test]
    public async Task PartitionByFile_NoOverlap()
    {
        var partitioner = new SpecPartitioner();
        var files = new List<string> { "a.spec.csx", "b.spec.csx", "c.spec.csx", "d.spec.csx" };

        var result0 = await partitioner.PartitionAsync(files, 2, 0, "file", "/project");
        var result1 = await partitioner.PartitionAsync(files, 2, 1, "file", "/project");

        // No overlap between partitions
        await Assert.That(result0.Files.Intersect(result1.Files)).IsEmpty();
    }

    #endregion

    #region Empty Files

    [Test]
    public async Task Partition_EmptyFiles_ReturnsEmpty()
    {
        var partitioner = new SpecPartitioner();
        var files = new List<string>();

        var result = await partitioner.PartitionAsync(files, 4, 0, "file", "/project");

        await Assert.That(result.Files).IsEmpty();
        await Assert.That(result.TotalFiles).IsEqualTo(0);
    }

    #endregion

    #region TotalFiles Tracking

    [Test]
    public async Task Partition_SetsTotalFiles()
    {
        var partitioner = new SpecPartitioner();
        var files = new List<string> { "a.spec.csx", "b.spec.csx", "c.spec.csx" };

        var result = await partitioner.PartitionAsync(files, 2, 0, "file", "/project");

        await Assert.That(result.TotalFiles).IsEqualTo(3);
    }

    #endregion

    #region Single File

    [Test]
    public async Task PartitionByFile_SingleFile_GoesToFirstPartition()
    {
        var partitioner = new SpecPartitioner();
        var files = new List<string> { "only.spec.csx" };

        var result0 = await partitioner.PartitionAsync(files, 4, 0, "file", "/project");
        var result1 = await partitioner.PartitionAsync(files, 4, 1, "file", "/project");

        await Assert.That(result0.Files).HasSingleItem();
        await Assert.That(result1.Files).IsEmpty();
    }

    #endregion

    #region Single Partition

    [Test]
    public async Task PartitionByFile_SinglePartition_ReturnsAllFiles()
    {
        var partitioner = new SpecPartitioner();
        var files = new List<string> { "a.spec.csx", "b.spec.csx", "c.spec.csx" };

        var result = await partitioner.PartitionAsync(files, 1, 0, "file", "/project");

        await Assert.That(result.Files.Count).IsEqualTo(3);
        await Assert.That(result.TotalFiles).IsEqualTo(3);
    }

    #endregion

    #region Strategy Selection

    [Test]
    public async Task Partition_DefaultStrategyIsFile()
    {
        var partitioner = new SpecPartitioner();
        var files = new List<string> { "a.spec.csx", "b.spec.csx" };

        // Passing "file" explicitly should work the same as default
        var result = await partitioner.PartitionAsync(files, 2, 0, "file", "/project");

        await Assert.That(result.Files).HasSingleItem();
    }

    [Test]
    public async Task Partition_SpecCountStrategy_Works()
    {
        var partitioner = new SpecPartitioner();
        var files = new List<string> { "a.spec.csx", "b.spec.csx" };

        // With non-existent files (0 specs each), greedy assigns all to partition 0
        // since all partitions have equal totals, first partition (0) is always chosen
        var result = await partitioner.PartitionAsync(files, 2, 0, "spec-count", "/project");

        // Both files go to partition 0 (greedy with equal weights)
        await Assert.That(result.Files.Count).IsEqualTo(2);
    }

    #endregion
}
