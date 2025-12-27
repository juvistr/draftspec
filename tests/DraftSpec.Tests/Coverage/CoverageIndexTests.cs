using DraftSpec.Coverage;

namespace DraftSpec.Tests.Coverage;

/// <summary>
/// Tests for CoverageIndex reverse lookup.
/// </summary>
public class CoverageIndexTests
{
    #region AddSpecCoverage

    [Test]
    public async Task AddSpecCoverage_IndexesLines()
    {
        var index = new CoverageIndex();
        var data = CreateCoverageData("spec-1", "test.cs", [10, 20, 30]);

        index.AddSpecCoverage(data);

        var specs = index.GetSpecsForLine("test.cs", 10);
        await Assert.That(specs).Contains("spec-1");
    }

    [Test]
    public async Task AddSpecCoverage_MultipleSpecs_SameLine()
    {
        var index = new CoverageIndex();
        index.AddSpecCoverage(CreateCoverageData("spec-1", "test.cs", [10]));
        index.AddSpecCoverage(CreateCoverageData("spec-2", "test.cs", [10]));
        index.AddSpecCoverage(CreateCoverageData("spec-3", "test.cs", [10]));

        var specs = index.GetSpecsForLine("test.cs", 10);

        await Assert.That(specs).Contains("spec-1");
        await Assert.That(specs).Contains("spec-2");
        await Assert.That(specs).Contains("spec-3");
    }

    #endregion

    #region GetSpecsForLine

    [Test]
    public async Task GetSpecsForLine_NonExistentFile_ReturnsEmpty()
    {
        var index = new CoverageIndex();

        var specs = index.GetSpecsForLine("nonexistent.cs", 10);

        await Assert.That(specs).IsEmpty();
    }

    [Test]
    public async Task GetSpecsForLine_NonExistentLine_ReturnsEmpty()
    {
        var index = new CoverageIndex();
        index.AddSpecCoverage(CreateCoverageData("spec-1", "test.cs", [10]));

        var specs = index.GetSpecsForLine("test.cs", 999);

        await Assert.That(specs).IsEmpty();
    }

    #endregion

    #region GetSpecsForFile

    [Test]
    public async Task GetSpecsForFile_ReturnsAllSpecsCoveringFile()
    {
        var index = new CoverageIndex();
        index.AddSpecCoverage(CreateCoverageData("spec-1", "test.cs", [10, 20]));
        index.AddSpecCoverage(CreateCoverageData("spec-2", "test.cs", [30, 40]));
        index.AddSpecCoverage(CreateCoverageData("spec-3", "other.cs", [10])); // Different file

        var specs = index.GetSpecsForFile("test.cs");

        await Assert.That(specs).Contains("spec-1");
        await Assert.That(specs).Contains("spec-2");
        await Assert.That(specs).DoesNotContain("spec-3");
    }

    [Test]
    public async Task GetSpecsForFile_ReturnsDedupedSpecs()
    {
        var index = new CoverageIndex();
        // Same spec covers multiple lines
        index.AddSpecCoverage(CreateCoverageData("spec-1", "test.cs", [10, 20, 30]));

        var specs = index.GetSpecsForFile("test.cs");

        // Should only contain spec-1 once (as a set)
        await Assert.That(specs.Count).IsEqualTo(1);
    }

    #endregion

    #region GetSpecsForLines

    [Test]
    public async Task GetSpecsForLines_ReturnsSpecsCoveringAnyLine()
    {
        var index = new CoverageIndex();
        index.AddSpecCoverage(CreateCoverageData("spec-1", "test.cs", [10]));
        index.AddSpecCoverage(CreateCoverageData("spec-2", "test.cs", [20]));
        index.AddSpecCoverage(CreateCoverageData("spec-3", "test.cs", [30]));

        var specs = index.GetSpecsForLines("test.cs", [10, 20]);

        await Assert.That(specs).Contains("spec-1");
        await Assert.That(specs).Contains("spec-2");
        await Assert.That(specs).DoesNotContain("spec-3");
    }

    #endregion

    #region GetLineCoverageCounts

    [Test]
    public async Task GetLineCoverageCounts_ReturnsSpecCountPerLine()
    {
        var index = new CoverageIndex();
        index.AddSpecCoverage(CreateCoverageData("spec-1", "test.cs", [10, 20]));
        index.AddSpecCoverage(CreateCoverageData("spec-2", "test.cs", [10]));
        index.AddSpecCoverage(CreateCoverageData("spec-3", "test.cs", [10]));

        var counts = index.GetLineCoverageCounts("test.cs");

        await Assert.That(counts[10]).IsEqualTo(3); // 3 specs cover line 10
        await Assert.That(counts[20]).IsEqualTo(1); // 1 spec covers line 20
    }

    #endregion

    #region Statistics

    [Test]
    public async Task TotalLinesCovered_ReturnsUniqueLineCount()
    {
        var index = new CoverageIndex();
        index.AddSpecCoverage(CreateCoverageData("spec-1", "test.cs", [10, 20]));
        index.AddSpecCoverage(CreateCoverageData("spec-2", "test.cs", [10, 30])); // Line 10 already covered

        await Assert.That(index.TotalLinesCovered).IsEqualTo(3); // Lines 10, 20, 30
    }

    [Test]
    public async Task TotalFilesCovered_ReturnsFileCount()
    {
        var index = new CoverageIndex();
        index.AddSpecCoverage(CreateCoverageData("spec-1", "file1.cs", [10]));
        index.AddSpecCoverage(CreateCoverageData("spec-2", "file2.cs", [10]));
        index.AddSpecCoverage(CreateCoverageData("spec-3", "file3.cs", [10]));

        await Assert.That(index.TotalFilesCovered).IsEqualTo(3);
    }

    [Test]
    public async Task GetCoveredFiles_ReturnsAllFiles()
    {
        var index = new CoverageIndex();
        index.AddSpecCoverage(CreateCoverageData("spec-1", "file1.cs", [10]));
        index.AddSpecCoverage(CreateCoverageData("spec-2", "file2.cs", [10]));

        var files = index.GetCoveredFiles();

        await Assert.That(files).Contains("file1.cs");
        await Assert.That(files).Contains("file2.cs");
    }

    #endregion

    #region Clear

    [Test]
    public async Task Clear_RemovesAllData()
    {
        var index = new CoverageIndex();
        index.AddSpecCoverage(CreateCoverageData("spec-1", "test.cs", [10, 20]));

        index.Clear();

        await Assert.That(index.TotalLinesCovered).IsEqualTo(0);
        await Assert.That(index.TotalFilesCovered).IsEqualTo(0);
    }

    #endregion

    #region Thread Safety

    [Test]
    public async Task AddSpecCoverage_ThreadSafe()
    {
        var index = new CoverageIndex();

        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() =>
            {
                var data = CreateCoverageData($"spec-{i}", "test.cs", [i, i + 100, i + 200]);
                index.AddSpecCoverage(data);
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Should have 300 unique lines (each spec covers 3 unique lines)
        await Assert.That(index.TotalLinesCovered).IsEqualTo(300);
    }

    #endregion

    #region Helper Methods

    private static SpecCoverageData CreateCoverageData(string specId, string filePath, int[] lineNumbers)
    {
        var lineHits = lineNumbers.ToDictionary(ln => ln, _ => 1);

        return new SpecCoverageData
        {
            SpecId = specId,
            FilesCovered = new Dictionary<string, CoveredFile>
            {
                [filePath] = new CoveredFile
                {
                    FilePath = filePath,
                    LineHits = lineHits
                }
            },
            Summary = new SpecCoverageSummary
            {
                LinesCovered = lineNumbers.Length,
                FilesTouched = 1
            }
        };
    }

    #endregion
}
