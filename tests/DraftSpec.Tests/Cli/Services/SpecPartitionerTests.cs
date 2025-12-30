using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Services;

namespace DraftSpec.Tests.Cli.Services;

/// <summary>
/// Tests for SpecPartitioner partitioning strategies.
/// </summary>
public class SpecPartitionerTests
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_partition_{Guid.NewGuid():N}");
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
    public async Task PartitionAsync_EmptyList_ReturnsEmptyPartition()
    {
        var partitioner = new SpecPartitioner();
        var files = Array.Empty<string>();

        var result = await partitioner.PartitionAsync(files, 3, 0, PartitionStrategy.File, _tempDir);

        await Assert.That(result.Files).IsEmpty();
        await Assert.That(result.TotalFiles).IsEqualTo(0);
    }

    #endregion

    #region File Strategy (Round-Robin)

    [Test]
    public async Task PartitionAsync_FileStrategy_RoundRobinDistribution()
    {
        var partitioner = new SpecPartitioner();
        var files = new[]
        {
            Path.Combine(_tempDir, "a.spec.csx"),
            Path.Combine(_tempDir, "b.spec.csx"),
            Path.Combine(_tempDir, "c.spec.csx"),
            Path.Combine(_tempDir, "d.spec.csx"),
            Path.Combine(_tempDir, "e.spec.csx"),
            Path.Combine(_tempDir, "f.spec.csx")
        };

        // Create empty files (file strategy doesn't parse them)
        foreach (var file in files)
            await File.WriteAllTextAsync(file, "");

        // 6 files, 3 partitions: each should get 2 files
        var partition0 = await partitioner.PartitionAsync(files, 3, 0, PartitionStrategy.File, _tempDir);
        var partition1 = await partitioner.PartitionAsync(files, 3, 1, PartitionStrategy.File, _tempDir);
        var partition2 = await partitioner.PartitionAsync(files, 3, 2, PartitionStrategy.File, _tempDir);

        await Assert.That(partition0.Files.Count).IsEqualTo(2);
        await Assert.That(partition1.Files.Count).IsEqualTo(2);
        await Assert.That(partition2.Files.Count).IsEqualTo(2);
        await Assert.That(partition0.TotalFiles).IsEqualTo(6);
    }

    [Test]
    public async Task PartitionAsync_FileStrategy_DeterministicOrder()
    {
        var partitioner = new SpecPartitioner();

        // Create files in non-alphabetical order
        var files = new[]
        {
            Path.Combine(_tempDir, "z.spec.csx"),
            Path.Combine(_tempDir, "a.spec.csx"),
            Path.Combine(_tempDir, "m.spec.csx")
        };

        foreach (var file in files)
            await File.WriteAllTextAsync(file, "");

        // Run multiple times - should always get same result
        var result1 = await partitioner.PartitionAsync(files, 2, 0, PartitionStrategy.File, _tempDir);
        var result2 = await partitioner.PartitionAsync(files, 2, 0, PartitionStrategy.File, _tempDir);

        await Assert.That(result1.Files).IsEquivalentTo(result2.Files);
    }

    #endregion

    #region Spec-Count Strategy

    [Test]
    public async Task PartitionAsync_SpecCountStrategy_ParsesRealFiles()
    {
        var partitioner = new SpecPartitioner();

        // Create files with different spec counts
        var file1 = Path.Combine(_tempDir, "one.spec.csx");
        var file2 = Path.Combine(_tempDir, "two.spec.csx");

        await File.WriteAllTextAsync(file1, """
            using static DraftSpec.Dsl;
            describe("One", () =>
            {
                it("spec1", () => { });
            });
            """);

        await File.WriteAllTextAsync(file2, """
            using static DraftSpec.Dsl;
            describe("Two", () =>
            {
                it("spec1", () => { });
                it("spec2", () => { });
                it("spec3", () => { });
            });
            """);

        var files = new[] { file1, file2 };

        // With 2 partitions, each file goes to separate partition
        var partition0 = await partitioner.PartitionAsync(files, 2, 0, PartitionStrategy.SpecCount, _tempDir);
        var partition1 = await partitioner.PartitionAsync(files, 2, 1, PartitionStrategy.SpecCount, _tempDir);

        // Total specs should be 4 (TotalSpecs is the sum across all files, same in both partitions)
        await Assert.That(partition0.TotalSpecs ?? 0).IsEqualTo(4);
        await Assert.That(partition1.TotalSpecs ?? 0).IsEqualTo(4);
    }

    [Test]
    public async Task PartitionAsync_SpecCountStrategy_BalancesBySpecCount()
    {
        var partitioner = new SpecPartitioner();

        // Create files: one large (10 specs), two small (2 specs each)
        var largeFile = Path.Combine(_tempDir, "large.spec.csx");
        var small1 = Path.Combine(_tempDir, "small1.spec.csx");
        var small2 = Path.Combine(_tempDir, "small2.spec.csx");

        await File.WriteAllTextAsync(largeFile, """
            using static DraftSpec.Dsl;
            describe("Large", () =>
            {
                it("spec1", () => { });
                it("spec2", () => { });
                it("spec3", () => { });
                it("spec4", () => { });
                it("spec5", () => { });
                it("spec6", () => { });
                it("spec7", () => { });
                it("spec8", () => { });
                it("spec9", () => { });
                it("spec10", () => { });
            });
            """);

        await File.WriteAllTextAsync(small1, """
            using static DraftSpec.Dsl;
            describe("Small1", () =>
            {
                it("spec1", () => { });
                it("spec2", () => { });
            });
            """);

        await File.WriteAllTextAsync(small2, """
            using static DraftSpec.Dsl;
            describe("Small2", () =>
            {
                it("spec1", () => { });
                it("spec2", () => { });
            });
            """);

        var files = new[] { largeFile, small1, small2 };

        // With 2 partitions and greedy balancing:
        // - Large file (10) assigned to partition with lowest total (0)
        // - Small1 (2) assigned to partition with lowest total (1)
        // - Small2 (2) assigned to partition with lowest total (1)
        // Result: Partition 0 has 10 specs, Partition 1 has 4 specs
        var partition0 = await partitioner.PartitionAsync(files, 2, 0, PartitionStrategy.SpecCount, _tempDir);
        var partition1 = await partitioner.PartitionAsync(files, 2, 1, PartitionStrategy.SpecCount, _tempDir);

        // Large file should be alone in one partition
        await Assert.That(partition0.PartitionSpecs ?? 0).IsEqualTo(10);
        await Assert.That(partition1.PartitionSpecs ?? 0).IsEqualTo(4);
        await Assert.That(partition0.Files.Count).IsEqualTo(1);
        await Assert.That(partition1.Files.Count).IsEqualTo(2);
    }

    [Test]
    public async Task PartitionAsync_SpecCountStrategy_GreedyMinimizesMaxPartitionSize()
    {
        var partitioner = new SpecPartitioner();

        // Create 4 files with spec counts: 8, 6, 4, 2 (total: 20)
        // Optimal 2-partition split: {8,2} and {6,4} = 10 each
        var file8 = Path.Combine(_tempDir, "file8.spec.csx");
        var file6 = Path.Combine(_tempDir, "file6.spec.csx");
        var file4 = Path.Combine(_tempDir, "file4.spec.csx");
        var file2 = Path.Combine(_tempDir, "file2.spec.csx");

        await WriteSpecFile(file8, 8);
        await WriteSpecFile(file6, 6);
        await WriteSpecFile(file4, 4);
        await WriteSpecFile(file2, 2);

        var files = new[] { file8, file6, file4, file2 };

        var partition0 = await partitioner.PartitionAsync(files, 2, 0, PartitionStrategy.SpecCount, _tempDir);
        var partition1 = await partitioner.PartitionAsync(files, 2, 1, PartitionStrategy.SpecCount, _tempDir);

        // Greedy assignment should give balanced result
        var maxSpecs = Math.Max(partition0.PartitionSpecs ?? 0, partition1.PartitionSpecs ?? 0);
        await Assert.That(maxSpecs).IsLessThanOrEqualTo(12); // Not perfectly balanced, but reasonable
        await Assert.That(partition0.TotalSpecs ?? 0).IsEqualTo(20);
    }

    [Test]
    public async Task PartitionAsync_SpecCountStrategy_ReturnsCorrectTotals()
    {
        var partitioner = new SpecPartitioner();

        var file1 = Path.Combine(_tempDir, "a.spec.csx");
        var file2 = Path.Combine(_tempDir, "b.spec.csx");
        var file3 = Path.Combine(_tempDir, "c.spec.csx");

        await WriteSpecFile(file1, 5);
        await WriteSpecFile(file2, 3);
        await WriteSpecFile(file3, 7);

        var files = new[] { file1, file2, file3 };

        var result = await partitioner.PartitionAsync(files, 3, 0, PartitionStrategy.SpecCount, _tempDir);

        await Assert.That(result.TotalFiles).IsEqualTo(3);
        await Assert.That(result.TotalSpecs ?? 0).IsEqualTo(15);
    }

    [Test]
    public async Task PartitionAsync_SpecCountStrategy_HandlesEmptySpecFiles()
    {
        var partitioner = new SpecPartitioner();

        var emptyFile = Path.Combine(_tempDir, "empty.spec.csx");
        var normalFile = Path.Combine(_tempDir, "normal.spec.csx");

        await File.WriteAllTextAsync(emptyFile, """
            using static DraftSpec.Dsl;
            // No specs
            """);

        await WriteSpecFile(normalFile, 3);

        var files = new[] { emptyFile, normalFile };

        var partition0 = await partitioner.PartitionAsync(files, 2, 0, PartitionStrategy.SpecCount, _tempDir);
        var partition1 = await partitioner.PartitionAsync(files, 2, 1, PartitionStrategy.SpecCount, _tempDir);

        // Both files should be distributed
        await Assert.That(partition0.Files.Count + partition1.Files.Count).IsEqualTo(2);
        // TotalSpecs is the sum across all files (3), same in both partitions
        await Assert.That(partition0.TotalSpecs ?? 0).IsEqualTo(3);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task PartitionAsync_SingleFile_GoesToPartitionZero()
    {
        var partitioner = new SpecPartitioner();
        var file = Path.Combine(_tempDir, "only.spec.csx");
        await WriteSpecFile(file, 5);

        var partition0 = await partitioner.PartitionAsync([file], 3, 0, PartitionStrategy.SpecCount, _tempDir);
        var partition1 = await partitioner.PartitionAsync([file], 3, 1, PartitionStrategy.SpecCount, _tempDir);
        var partition2 = await partitioner.PartitionAsync([file], 3, 2, PartitionStrategy.SpecCount, _tempDir);

        await Assert.That(partition0.Files.Count).IsEqualTo(1);
        await Assert.That(partition1.Files).IsEmpty();
        await Assert.That(partition2.Files).IsEmpty();
    }

    [Test]
    public async Task PartitionAsync_MorePartitionsThanFiles_SomePartitionsEmpty()
    {
        var partitioner = new SpecPartitioner();
        var file1 = Path.Combine(_tempDir, "a.spec.csx");
        var file2 = Path.Combine(_tempDir, "b.spec.csx");
        await WriteSpecFile(file1, 3);
        await WriteSpecFile(file2, 2);

        var files = new[] { file1, file2 };

        // 5 partitions for 2 files
        var results = new List<PartitionResult>();
        for (var i = 0; i < 5; i++)
        {
            results.Add(await partitioner.PartitionAsync(files, 5, i, PartitionStrategy.SpecCount, _tempDir));
        }

        var totalFilesAcrossPartitions = results.Sum(r => r.Files.Count);
        await Assert.That(totalFilesAcrossPartitions).IsEqualTo(2);

        var emptyPartitions = results.Count(r => r.Files.Count == 0);
        await Assert.That(emptyPartitions).IsEqualTo(3);
    }

    #endregion

    #region Helpers

    private static async Task WriteSpecFile(string path, int specCount)
    {
        var specs = string.Join("\n", Enumerable.Range(1, specCount)
            .Select(i => $"    it(\"spec{i}\", () => {{ }});"));

        var content = $$"""
            using static DraftSpec.Dsl;
            describe("Test", () =>
            {
            {{specs}}
            });
            """;

        await File.WriteAllTextAsync(path, content);
    }

    #endregion
}
