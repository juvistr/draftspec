using DraftSpec.Cli.Coverage;

namespace DraftSpec.Tests.Cli.Coverage;

/// <summary>
/// Tests for Cobertura XML parsing.
/// </summary>
public class CoberturaParserTests
{
    #region Parse Valid XML

    [Test]
    public async Task Parse_ValidXml_ReturnsReport()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage timestamp="1703721600">
                <sources>
                    <source>/src</source>
                </sources>
                <packages>
                    <package name="MyApp">
                        <classes>
                            <class name="Calculator" filename="Calculator.cs">
                                <lines>
                                    <line number="1" hits="1"/>
                                    <line number="2" hits="1"/>
                                    <line number="3" hits="0"/>
                                </lines>
                            </class>
                        </classes>
                    </package>
                </packages>
            </coverage>
            """;

        var report = CoberturaParser.Parse(xml);

        await Assert.That(report).IsNotNull();
        await Assert.That(report.Source).IsEqualTo("/src");
        await Assert.That(report.Files).HasCount().EqualTo(1);
    }

    [Test]
    public async Task Parse_CalculatesLineCoverage()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
                <packages>
                    <package name="MyApp">
                        <classes>
                            <class name="Calculator" filename="Calculator.cs">
                                <lines>
                                    <line number="1" hits="1"/>
                                    <line number="2" hits="1"/>
                                    <line number="3" hits="0"/>
                                    <line number="4" hits="0"/>
                                </lines>
                            </class>
                        </classes>
                    </package>
                </packages>
            </coverage>
            """;

        var report = CoberturaParser.Parse(xml);

        await Assert.That(report.Summary.TotalLines).IsEqualTo(4);
        await Assert.That(report.Summary.CoveredLines).IsEqualTo(2);
        await Assert.That(report.Summary.LinePercent).IsEqualTo(50.0);
    }

    [Test]
    public async Task Parse_CalculatesBranchCoverage()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
                <packages>
                    <package name="MyApp">
                        <classes>
                            <class name="Calculator" filename="Calculator.cs">
                                <lines>
                                    <line number="1" hits="1" branch="true" condition-coverage="50% (2/4)"/>
                                    <line number="2" hits="1"/>
                                </lines>
                            </class>
                        </classes>
                    </package>
                </packages>
            </coverage>
            """;

        var report = CoberturaParser.Parse(xml);

        await Assert.That(report.Summary.TotalBranches).IsEqualTo(4);
        await Assert.That(report.Summary.CoveredBranches).IsEqualTo(2);
        await Assert.That(report.Summary.BranchPercent).IsEqualTo(50.0);
    }

    [Test]
    public async Task Parse_MultipleFiles_AggregatesCoverage()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
                <packages>
                    <package name="MyApp">
                        <classes>
                            <class name="Calculator" filename="Calculator.cs">
                                <lines>
                                    <line number="1" hits="1"/>
                                    <line number="2" hits="0"/>
                                </lines>
                            </class>
                            <class name="Parser" filename="Parser.cs">
                                <lines>
                                    <line number="1" hits="1"/>
                                    <line number="2" hits="1"/>
                                </lines>
                            </class>
                        </classes>
                    </package>
                </packages>
            </coverage>
            """;

        var report = CoberturaParser.Parse(xml);

        await Assert.That(report.Files).HasCount().EqualTo(2);
        await Assert.That(report.Summary.TotalLines).IsEqualTo(4);
        await Assert.That(report.Summary.CoveredLines).IsEqualTo(3);
        await Assert.That(report.Summary.LinePercent).IsEqualTo(75.0);
    }

    [Test]
    public async Task Parse_ParsesTimestamp()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage timestamp="1703721600">
                <packages/>
            </coverage>
            """;

        var report = CoberturaParser.Parse(xml);

        // 1703721600 = 2023-12-28 00:00:00 UTC
        await Assert.That(report.Timestamp).IsEqualTo(new DateTime(2023, 12, 28, 0, 0, 0, DateTimeKind.Utc));
    }

    #endregion

    #region File Coverage Details

    [Test]
    public async Task Parse_SetsFilePath()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
                <packages>
                    <package name="MyApp.Services">
                        <classes>
                            <class name="Calculator" filename="src/Services/Calculator.cs">
                                <lines>
                                    <line number="1" hits="1"/>
                                </lines>
                            </class>
                        </classes>
                    </package>
                </packages>
            </coverage>
            """;

        var report = CoberturaParser.Parse(xml);

        await Assert.That(report.Files[0].FilePath).IsEqualTo("src/Services/Calculator.cs");
        await Assert.That(report.Files[0].PackageName).IsEqualTo("MyApp.Services");
    }

    [Test]
    public async Task Parse_SetsFileLineCoverage()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
                <packages>
                    <package name="MyApp">
                        <classes>
                            <class name="Calculator" filename="Calculator.cs">
                                <lines>
                                    <line number="10" hits="5"/>
                                    <line number="11" hits="0"/>
                                </lines>
                            </class>
                        </classes>
                    </package>
                </packages>
            </coverage>
            """;

        var report = CoberturaParser.Parse(xml);
        var file = report.Files[0];

        await Assert.That(file.Lines).HasCount().EqualTo(2);
        await Assert.That(file.Lines[0].LineNumber).IsEqualTo(10);
        await Assert.That(file.Lines[0].Hits).IsEqualTo(5);
        await Assert.That(file.Lines[1].LineNumber).IsEqualTo(11);
        await Assert.That(file.Lines[1].Hits).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_SetsBranchPointInfo()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
                <packages>
                    <package name="MyApp">
                        <classes>
                            <class name="Calculator" filename="Calculator.cs">
                                <lines>
                                    <line number="10" hits="1" branch="true" condition-coverage="75% (3/4)"/>
                                </lines>
                            </class>
                        </classes>
                    </package>
                </packages>
            </coverage>
            """;

        var report = CoberturaParser.Parse(xml);
        var line = report.Files[0].Lines[0];

        await Assert.That(line.IsBranchPoint).IsTrue();
        await Assert.That(line.BranchesCovered).IsEqualTo(3);
        await Assert.That(line.BranchesTotal).IsEqualTo(4);
    }

    #endregion

    #region Line Coverage Status

    [Test]
    public async Task LineCoverage_Status_Covered_WhenHitsGreaterThanZero()
    {
        var line = new LineCoverage { LineNumber = 1, Hits = 5, IsBranchPoint = false };

        await Assert.That(line.Status).IsEqualTo(CoverageStatus.Covered);
    }

    [Test]
    public async Task LineCoverage_Status_Uncovered_WhenHitsIsZero()
    {
        var line = new LineCoverage { LineNumber = 1, Hits = 0, IsBranchPoint = false };

        await Assert.That(line.Status).IsEqualTo(CoverageStatus.Uncovered);
    }

    [Test]
    public async Task LineCoverage_Status_Partial_WhenBranchPartialCoverage()
    {
        var line = new LineCoverage
        {
            LineNumber = 1,
            Hits = 1,
            IsBranchPoint = true,
            BranchesCovered = 2,
            BranchesTotal = 4
        };

        await Assert.That(line.Status).IsEqualTo(CoverageStatus.Partial);
    }

    [Test]
    public async Task LineCoverage_Status_Covered_WhenAllBranchesCovered()
    {
        var line = new LineCoverage
        {
            LineNumber = 1,
            Hits = 1,
            IsBranchPoint = true,
            BranchesCovered = 4,
            BranchesTotal = 4
        };

        await Assert.That(line.Status).IsEqualTo(CoverageStatus.Covered);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Parse_EmptyPackages_ReturnsEmptyReport()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
                <packages/>
            </coverage>
            """;

        var report = CoberturaParser.Parse(xml);

        await Assert.That(report.Files).IsEmpty();
        await Assert.That(report.Summary.TotalLines).IsEqualTo(0);
        await Assert.That(report.Summary.LinePercent).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_NoLines_ReturnsZeroPercent()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
                <packages>
                    <package name="MyApp">
                        <classes>
                            <class name="Empty" filename="Empty.cs">
                                <lines/>
                            </class>
                        </classes>
                    </package>
                </packages>
            </coverage>
            """;

        var report = CoberturaParser.Parse(xml);

        await Assert.That(report.Files[0].TotalLines).IsEqualTo(0);
        await Assert.That(report.Files[0].LinePercent).IsEqualTo(0);
    }

    [Test]
    public void Parse_InvalidXml_Throws()
    {
        var xml = "not valid xml";

        Assert.Throws<System.Xml.XmlException>(() => CoberturaParser.Parse(xml));
    }

    [Test]
    public async Task TryParseFile_NonExistentFile_ReturnsNull()
    {
        var result = CoberturaParser.TryParseFile("/nonexistent/path/coverage.xml");

        await Assert.That(result).IsNull();
    }

    #endregion
}
