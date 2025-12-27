using System.Text.Json;
using DraftSpec.Cli.Coverage;

namespace DraftSpec.Tests.Cli.Coverage;

/// <summary>
/// Tests for coverage formatters (HTML and JSON).
/// </summary>
public class CoverageFormatterTests
{
    private static CoverageReport CreateSampleReport(
        int totalLines = 100,
        int coveredLines = 80,
        int totalBranches = 20,
        int coveredBranches = 15)
    {
        return new CoverageReport
        {
            Timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Source = "/src",
            Summary = new CoverageSummary
            {
                TotalLines = totalLines,
                CoveredLines = coveredLines,
                TotalBranches = totalBranches,
                CoveredBranches = coveredBranches
            },
            Files =
            [
                new FileCoverage
                {
                    FilePath = "src/Calculator.cs",
                    PackageName = "MyApp",
                    TotalLines = 50,
                    CoveredLines = 45,
                    TotalBranches = 10,
                    CoveredBranches = 8,
                    Lines =
                    [
                        new LineCoverage { LineNumber = 1, Hits = 5 },
                        new LineCoverage { LineNumber = 2, Hits = 0 },
                        new LineCoverage
                        {
                            LineNumber = 3,
                            Hits = 2,
                            IsBranchPoint = true,
                            BranchesCovered = 1,
                            BranchesTotal = 2
                        }
                    ]
                },
                new FileCoverage
                {
                    FilePath = "src/Utils.cs",
                    PackageName = "MyApp",
                    TotalLines = 50,
                    CoveredLines = 35,
                    TotalBranches = 10,
                    CoveredBranches = 7,
                    Lines =
                    [
                        new LineCoverage { LineNumber = 10, Hits = 1 }
                    ]
                }
            ]
        };
    }

    #region CoverageHtmlFormatter Tests

    [Test]
    public async Task HtmlFormatter_FileExtension_ReturnsHtml()
    {
        var formatter = new CoverageHtmlFormatter();

        await Assert.That(formatter.FileExtension).IsEqualTo(".html");
    }

    [Test]
    public async Task HtmlFormatter_FormatName_ReturnsHtml()
    {
        var formatter = new CoverageHtmlFormatter();

        await Assert.That(formatter.FormatName).IsEqualTo("html");
    }

    [Test]
    public async Task HtmlFormatter_Format_ProducesValidHtml()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        await Assert.That(html).StartsWith("<!DOCTYPE html>");
        await Assert.That(html).Contains("</html>");
    }

    [Test]
    public async Task HtmlFormatter_Format_IncludesTitle()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        await Assert.That(html).Contains("<title>Coverage Report</title>");
        await Assert.That(html).Contains("<h1>Coverage Report</h1>");
    }

    [Test]
    public async Task HtmlFormatter_Format_IncludesLineCoverage()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        await Assert.That(html).Contains("Line Coverage");
        await Assert.That(html).Contains("80.0%");
    }

    [Test]
    public async Task HtmlFormatter_Format_IncludesBranchCoverage()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        await Assert.That(html).Contains("Branch Coverage");
        await Assert.That(html).Contains("75.0%");
    }

    [Test]
    public async Task HtmlFormatter_Format_IncludesFileNames()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        await Assert.That(html).Contains("Calculator.cs");
        await Assert.That(html).Contains("Utils.cs");
    }

    [Test]
    public async Task HtmlFormatter_Format_IncludesLineDetails()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        // Check for line numbers and hit counts
        await Assert.That(html).Contains("Hits");
        await Assert.That(html).Contains("Status");
    }

    [Test]
    public async Task HtmlFormatter_Format_IncludesStatusIndicators()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        // Check for status icons (covered, uncovered, partial)
        await Assert.That(html).Contains("✓"); // covered
        await Assert.That(html).Contains("✗"); // uncovered
        await Assert.That(html).Contains("◐"); // partial
    }

    [Test]
    public async Task HtmlFormatter_Format_IncludesProgressBars()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        await Assert.That(html).Contains("progress-bar");
        await Assert.That(html).Contains("progress-fill");
    }

    [Test]
    public async Task HtmlFormatter_Format_IncludesStyles()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        await Assert.That(html).Contains("<style>");
        await Assert.That(html).Contains("</style>");
    }

    [Test]
    public async Task HtmlFormatter_Format_IncludesDarkModeSupport()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        await Assert.That(html).Contains("prefers-color-scheme: dark");
    }

    [Test]
    public async Task HtmlFormatter_Format_IncludesSource()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        await Assert.That(html).Contains("Source:");
        await Assert.That(html).Contains("/src");
    }

    [Test]
    public async Task HtmlFormatter_Format_IncludesTimestamp()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        await Assert.That(html).Contains("Generated");
        await Assert.That(html).Contains("2024-01-15");
    }

    [Test]
    public async Task HtmlFormatter_Format_UsesCollapsibleSections()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        await Assert.That(html).Contains("<details");
        await Assert.That(html).Contains("</details>");
        await Assert.That(html).Contains("<summary");
    }

    [Test]
    public async Task HtmlFormatter_Format_EscapesHtmlCharacters()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = new CoverageReport
        {
            Timestamp = DateTime.UtcNow,
            Source = "<script>alert('xss')</script>",
            Summary = new CoverageSummary(),
            Files = []
        };

        var html = formatter.Format(report);

        await Assert.That(html).DoesNotContain("<script>alert");
        await Assert.That(html).Contains("&lt;script&gt;");
    }

    [Test]
    public async Task HtmlFormatter_Format_HandlesEmptyReport()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = new CoverageReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new CoverageSummary(),
            Files = []
        };

        var html = formatter.Format(report);

        await Assert.That(html).Contains("No coverage data available");
    }

    [Test]
    public async Task HtmlFormatter_Format_IncludesBranchInfo()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        // Check for branch info on partial line
        await Assert.That(html).Contains("(1/2 branches)");
    }

    [Test]
    public async Task HtmlFormatter_Format_SortsByLowestCoverage()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        // Utils.cs (70%) should appear before Calculator.cs (90%)
        var utilsIndex = html.IndexOf("Utils.cs", StringComparison.Ordinal);
        var calculatorIndex = html.IndexOf("Calculator.cs", StringComparison.Ordinal);

        await Assert.That(utilsIndex).IsLessThan(calculatorIndex);
    }

    [Test]
    public async Task HtmlFormatter_Format_IncludesStatusClasses()
    {
        var formatter = new CoverageHtmlFormatter();
        var report = CreateSampleReport();

        var html = formatter.Format(report);

        // 80% line coverage should be "good" status
        await Assert.That(html).Contains("class=\"metric-value good\"");
    }

    #endregion

    #region CoverageJsonFormatter Tests

    [Test]
    public async Task JsonFormatter_FileExtension_ReturnsJson()
    {
        var formatter = new CoverageJsonFormatter();

        await Assert.That(formatter.FileExtension).IsEqualTo(".json");
    }

    [Test]
    public async Task JsonFormatter_FormatName_ReturnsJson()
    {
        var formatter = new CoverageJsonFormatter();

        await Assert.That(formatter.FormatName).IsEqualTo("json");
    }

    [Test]
    public async Task JsonFormatter_Format_ProducesValidJson()
    {
        var formatter = new CoverageJsonFormatter();
        var report = CreateSampleReport();

        var json = formatter.Format(report);

        // Should not throw
        var parsed = JsonDocument.Parse(json);
        await Assert.That(parsed).IsNotNull();
    }

    [Test]
    public async Task JsonFormatter_Format_IncludesSummary()
    {
        var formatter = new CoverageJsonFormatter();
        var report = CreateSampleReport();

        var json = formatter.Format(report);
        using var doc = JsonDocument.Parse(json);
        var summary = doc.RootElement.GetProperty("summary");

        await Assert.That(summary.GetProperty("totalLines").GetInt32()).IsEqualTo(100);
        await Assert.That(summary.GetProperty("coveredLines").GetInt32()).IsEqualTo(80);
        await Assert.That(summary.GetProperty("linePercent").GetDouble()).IsEqualTo(80);
    }

    [Test]
    public async Task JsonFormatter_Format_IncludesBranchSummary()
    {
        var formatter = new CoverageJsonFormatter();
        var report = CreateSampleReport();

        var json = formatter.Format(report);
        using var doc = JsonDocument.Parse(json);
        var summary = doc.RootElement.GetProperty("summary");

        await Assert.That(summary.GetProperty("totalBranches").GetInt32()).IsEqualTo(20);
        await Assert.That(summary.GetProperty("coveredBranches").GetInt32()).IsEqualTo(15);
        await Assert.That(summary.GetProperty("branchPercent").GetDouble()).IsEqualTo(75);
    }

    [Test]
    public async Task JsonFormatter_Format_IncludesFiles()
    {
        var formatter = new CoverageJsonFormatter();
        var report = CreateSampleReport();

        var json = formatter.Format(report);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("files");

        await Assert.That(files.GetArrayLength()).IsEqualTo(2);
    }

    [Test]
    public async Task JsonFormatter_Format_IncludesFileDetails()
    {
        var formatter = new CoverageJsonFormatter();
        var report = CreateSampleReport();

        var json = formatter.Format(report);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("files");
        var firstFile = files[0];

        await Assert.That(firstFile.GetProperty("filePath").GetString()).IsEqualTo("src/Calculator.cs");
        await Assert.That(firstFile.GetProperty("packageName").GetString()).IsEqualTo("MyApp");
        await Assert.That(firstFile.GetProperty("totalLines").GetInt32()).IsEqualTo(50);
        await Assert.That(firstFile.GetProperty("coveredLines").GetInt32()).IsEqualTo(45);
    }

    [Test]
    public async Task JsonFormatter_Format_IncludesLineDetails()
    {
        var formatter = new CoverageJsonFormatter();
        var report = CreateSampleReport();

        var json = formatter.Format(report);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("files");
        var firstFile = files[0];
        var lines = firstFile.GetProperty("lines");

        await Assert.That(lines.GetArrayLength()).IsEqualTo(3);

        var firstLine = lines[0];
        await Assert.That(firstLine.GetProperty("lineNumber").GetInt32()).IsEqualTo(1);
        await Assert.That(firstLine.GetProperty("hits").GetInt32()).IsEqualTo(5);
        await Assert.That(firstLine.GetProperty("status").GetString()).IsEqualTo("covered");
    }

    [Test]
    public async Task JsonFormatter_Format_IncludesLineStatus()
    {
        var formatter = new CoverageJsonFormatter();
        var report = CreateSampleReport();

        var json = formatter.Format(report);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("files");
        var firstFile = files[0];
        var lines = firstFile.GetProperty("lines");

        // Line 1: covered, Line 2: uncovered, Line 3: partial
        await Assert.That(lines[0].GetProperty("status").GetString()).IsEqualTo("covered");
        await Assert.That(lines[1].GetProperty("status").GetString()).IsEqualTo("uncovered");
        await Assert.That(lines[2].GetProperty("status").GetString()).IsEqualTo("partial");
    }

    [Test]
    public async Task JsonFormatter_Format_IncludesBranchInfo()
    {
        var formatter = new CoverageJsonFormatter();
        var report = CreateSampleReport();

        var json = formatter.Format(report);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("files");
        var firstFile = files[0];
        var lines = firstFile.GetProperty("lines");
        var branchLine = lines[2]; // Third line is a branch point

        await Assert.That(branchLine.GetProperty("isBranchPoint").GetBoolean()).IsTrue();
        await Assert.That(branchLine.GetProperty("branchesCovered").GetInt32()).IsEqualTo(1);
        await Assert.That(branchLine.GetProperty("branchesTotal").GetInt32()).IsEqualTo(2);
    }

    [Test]
    public async Task JsonFormatter_Format_OmitsNullBranchInfo()
    {
        var formatter = new CoverageJsonFormatter();
        var report = CreateSampleReport();

        var json = formatter.Format(report);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("files");
        var firstFile = files[0];
        var lines = firstFile.GetProperty("lines");
        var nonBranchLine = lines[0];

        // isBranchPoint should not be present when false (WhenWritingNull)
        await Assert.That(nonBranchLine.TryGetProperty("isBranchPoint", out _)).IsFalse();
    }

    [Test]
    public async Task JsonFormatter_Format_IncludesSource()
    {
        var formatter = new CoverageJsonFormatter();
        var report = CreateSampleReport();

        var json = formatter.Format(report);
        using var doc = JsonDocument.Parse(json);

        await Assert.That(doc.RootElement.GetProperty("source").GetString()).IsEqualTo("/src");
    }

    [Test]
    public async Task JsonFormatter_Format_IncludesTimestamp()
    {
        var formatter = new CoverageJsonFormatter();
        var report = CreateSampleReport();

        var json = formatter.Format(report);
        using var doc = JsonDocument.Parse(json);

        var timestamp = doc.RootElement.GetProperty("timestamp").GetDateTime();
        await Assert.That(timestamp.Year).IsEqualTo(2024);
        await Assert.That(timestamp.Month).IsEqualTo(1);
        await Assert.That(timestamp.Day).IsEqualTo(15);
    }

    [Test]
    public async Task JsonFormatter_Format_UsesCamelCase()
    {
        var formatter = new CoverageJsonFormatter();
        var report = CreateSampleReport();

        var json = formatter.Format(report);

        await Assert.That(json).Contains("\"totalLines\":");
        await Assert.That(json).Contains("\"coveredLines\":");
        await Assert.That(json).Contains("\"linePercent\":");
        await Assert.That(json).DoesNotContain("\"TotalLines\":");
    }

    [Test]
    public async Task JsonFormatter_Format_RoundsPercentages()
    {
        var formatter = new CoverageJsonFormatter();
        var report = new CoverageReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new CoverageSummary
            {
                TotalLines = 3,
                CoveredLines = 1,
                TotalBranches = 0,
                CoveredBranches = 0
            },
            Files = []
        };

        var json = formatter.Format(report);
        using var doc = JsonDocument.Parse(json);
        var summary = doc.RootElement.GetProperty("summary");

        // Should be rounded to 2 decimal places (1/3 = 33.333... -> 33.33)
        await Assert.That(summary.GetProperty("linePercent").GetDouble()).IsEqualTo(33.33);
    }

    [Test]
    public async Task JsonFormatter_Format_ProducesIndentedOutput()
    {
        var formatter = new CoverageJsonFormatter();
        var report = CreateSampleReport();

        var json = formatter.Format(report);

        // Indented JSON will have newlines and spaces
        await Assert.That(json).Contains("\n");
        await Assert.That(json).Contains("  "); // At least 2-space indentation
    }

    [Test]
    public async Task JsonFormatter_Format_HandlesEmptyReport()
    {
        var formatter = new CoverageJsonFormatter();
        var report = new CoverageReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new CoverageSummary(),
            Files = []
        };

        var json = formatter.Format(report);
        using var doc = JsonDocument.Parse(json);

        var files = doc.RootElement.GetProperty("files");
        await Assert.That(files.GetArrayLength()).IsEqualTo(0);
    }

    #endregion

    #region ICoverageFormatter Interface Tests

    [Test]
    public async Task AllFormatters_ImplementInterface()
    {
        ICoverageFormatter htmlFormatter = new CoverageHtmlFormatter();
        ICoverageFormatter jsonFormatter = new CoverageJsonFormatter();

        await Assert.That(htmlFormatter.FileExtension).IsNotNull();
        await Assert.That(jsonFormatter.FileExtension).IsNotNull();

        await Assert.That(htmlFormatter.FormatName).IsNotNull();
        await Assert.That(jsonFormatter.FormatName).IsNotNull();
    }

    [Test]
    public async Task AllFormatters_HaveUniqueExtensions()
    {
        var htmlFormatter = new CoverageHtmlFormatter();
        var jsonFormatter = new CoverageJsonFormatter();

        await Assert.That(htmlFormatter.FileExtension).IsNotEqualTo(jsonFormatter.FileExtension);
    }

    [Test]
    public async Task AllFormatters_HaveUniqueFormatNames()
    {
        var htmlFormatter = new CoverageHtmlFormatter();
        var jsonFormatter = new CoverageJsonFormatter();

        await Assert.That(htmlFormatter.FormatName).IsNotEqualTo(jsonFormatter.FormatName);
    }

    #endregion
}
