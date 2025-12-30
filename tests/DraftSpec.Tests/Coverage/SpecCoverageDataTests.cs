using DraftSpec.Coverage;

namespace DraftSpec.Tests.Coverage;

/// <summary>
/// Tests for SpecCoverageData, CoveredFile, BranchCoverage, and SpecCoverageSummary records.
/// </summary>
public class SpecCoverageDataTests
{
    #region CoveredFile.LinesCovered

    [Test]
    public async Task CoveredFile_LinesCovered_CountsHitLines()
    {
        var file = new CoveredFile
        {
            FilePath = "/test.cs",
            LineHits = new Dictionary<int, int>
            {
                [1] = 1,  // hit once
                [2] = 5,  // hit 5 times
                [3] = 10  // hit 10 times
            }
        };

        await Assert.That(file.LinesCovered).IsEqualTo(3);
    }

    [Test]
    public async Task CoveredFile_LinesCovered_ExcludesZeroHits()
    {
        var file = new CoveredFile
        {
            FilePath = "/test.cs",
            LineHits = new Dictionary<int, int>
            {
                [1] = 1,  // hit
                [2] = 0,  // not hit
                [3] = 5,  // hit
                [4] = 0   // not hit
            }
        };

        await Assert.That(file.LinesCovered).IsEqualTo(2);
    }

    [Test]
    public async Task CoveredFile_LinesCovered_EmptyDictionary_ReturnsZero()
    {
        var file = new CoveredFile
        {
            FilePath = "/test.cs",
            LineHits = new Dictionary<int, int>()
        };

        await Assert.That(file.LinesCovered).IsEqualTo(0);
    }

    [Test]
    public async Task CoveredFile_LinesCovered_AllZeroHits_ReturnsZero()
    {
        var file = new CoveredFile
        {
            FilePath = "/test.cs",
            LineHits = new Dictionary<int, int>
            {
                [1] = 0,
                [2] = 0,
                [3] = 0
            }
        };

        await Assert.That(file.LinesCovered).IsEqualTo(0);
    }

    #endregion

    #region BranchCoverage.Percent

    [Test]
    public async Task BranchCoverage_Percent_CalculatesCorrectly()
    {
        var branch = new BranchCoverage { Covered = 3, Total = 4 };

        await Assert.That(branch.Percent).IsEqualTo(75.0);
    }

    [Test]
    public async Task BranchCoverage_Percent_AllCovered_Returns100()
    {
        var branch = new BranchCoverage { Covered = 5, Total = 5 };

        await Assert.That(branch.Percent).IsEqualTo(100.0);
    }

    [Test]
    public async Task BranchCoverage_Percent_NoneCovered_ReturnsZero()
    {
        var branch = new BranchCoverage { Covered = 0, Total = 4 };

        await Assert.That(branch.Percent).IsEqualTo(0.0);
    }

    [Test]
    public async Task BranchCoverage_Percent_ZeroTotal_ReturnsZero()
    {
        var branch = new BranchCoverage { Covered = 0, Total = 0 };

        await Assert.That(branch.Percent).IsEqualTo(0.0);
    }

    [Test]
    public async Task BranchCoverage_Percent_PartialCoverage_CorrectDecimal()
    {
        var branch = new BranchCoverage { Covered = 1, Total = 3 };

        // 1/3 = 33.333...%
        await Assert.That(branch.Percent).IsGreaterThan(33.3);
        await Assert.That(branch.Percent).IsLessThan(33.4);
    }

    #endregion

    #region SpecCoverageData Record

    [Test]
    public async Task SpecCoverageData_RequiredProperties_SetCorrectly()
    {
        var data = new SpecCoverageData
        {
            SpecId = "Calculator adds numbers",
            FilesCovered = new Dictionary<string, CoveredFile>(),
            Summary = new SpecCoverageSummary { LinesCovered = 10, FilesTouched = 2 }
        };

        await Assert.That(data.SpecId).IsEqualTo("Calculator adds numbers");
        await Assert.That(data.FilesCovered).IsNotNull();
        await Assert.That(data.Summary.LinesCovered).IsEqualTo(10);
    }

    #endregion

    #region SpecCoverageSummary Record

    [Test]
    public async Task SpecCoverageSummary_RequiredProperties_SetCorrectly()
    {
        var summary = new SpecCoverageSummary
        {
            LinesCovered = 50,
            BranchesCovered = 10,
            FilesTouched = 5
        };

        await Assert.That(summary.LinesCovered).IsEqualTo(50);
        await Assert.That(summary.BranchesCovered).IsEqualTo(10);
        await Assert.That(summary.FilesTouched).IsEqualTo(5);
    }

    [Test]
    public async Task SpecCoverageSummary_BranchesCovered_DefaultsToZero()
    {
        var summary = new SpecCoverageSummary
        {
            LinesCovered = 10,
            FilesTouched = 1
        };

        await Assert.That(summary.BranchesCovered).IsEqualTo(0);
    }

    #endregion

    #region CoveredFile with BranchHits

    [Test]
    public async Task CoveredFile_BranchHits_CanBeNull()
    {
        var file = new CoveredFile
        {
            FilePath = "/test.cs",
            LineHits = new Dictionary<int, int> { [1] = 1 }
        };

        await Assert.That(file.BranchHits is null).IsTrue();
    }

    [Test]
    public async Task CoveredFile_BranchHits_CanContainData()
    {
        var file = new CoveredFile
        {
            FilePath = "/test.cs",
            LineHits = new Dictionary<int, int> { [1] = 1 },
            BranchHits = new Dictionary<int, BranchCoverage>
            {
                [1] = new BranchCoverage { Covered = 2, Total = 4 }
            }
        };

        await Assert.That(file.BranchHits is not null).IsTrue();
        await Assert.That(file.BranchHits![1].Covered).IsEqualTo(2);
        await Assert.That(file.BranchHits[1].Total).IsEqualTo(4);
    }

    #endregion
}
