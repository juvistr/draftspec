using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Parses Cobertura XML coverage reports.
/// </summary>
public static partial class CoberturaParser
{
    /// <summary>
    /// Parse a Cobertura XML file into a CoverageReport.
    /// </summary>
    public static CoverageReport ParseFile(string filePath)
    {
        var xml = File.ReadAllText(filePath);
        return Parse(xml);
    }

    /// <summary>
    /// Parse Cobertura XML content into a CoverageReport.
    /// </summary>
    public static CoverageReport Parse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var coverage = doc.Root ?? throw new InvalidOperationException("Invalid Cobertura XML: missing root element");

        var report = new CoverageReport();

        // Parse timestamp
        var timestampAttr = coverage.Attribute("timestamp");
        if (timestampAttr != null && long.TryParse(timestampAttr.Value, out var timestamp))
        {
            report.Timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        }

        // Parse sources
        var source = coverage.Element("sources")?.Element("source")?.Value;
        report.Source = source;

        // Parse packages/classes/lines
        foreach (var package in coverage.Descendants("package"))
        {
            var packageName = package.Attribute("name")?.Value;

            foreach (var cls in package.Descendants("class"))
            {
                var fileCoverage = new FileCoverage
                {
                    FilePath = cls.Attribute("filename")?.Value ?? "",
                    PackageName = packageName
                };

                foreach (var line in cls.Descendants("line"))
                {
                    var lineCov = new LineCoverage
                    {
                        LineNumber = ParseInt(line.Attribute("number")?.Value, 0),
                        Hits = ParseInt(line.Attribute("hits")?.Value, 0),
                        IsBranchPoint = string.Equals(line.Attribute("branch")?.Value, "true", StringComparison.Ordinal)
                    };

                    if (lineCov.IsBranchPoint)
                    {
                        // Parse condition-coverage like "75% (3/4)"
                        var condCov = line.Attribute("condition-coverage")?.Value;
                        if (condCov != null)
                        {
                            var match = ConditionCoveragePattern().Match(condCov);
                            if (match.Success)
                            {
                                lineCov.BranchesCovered = ParseInt(match.Groups["covered"].Value, 0);
                                lineCov.BranchesTotal = ParseInt(match.Groups["total"].Value, 0);
                            }
                        }
                    }

                    fileCoverage.Lines.Add(lineCov);
                }

                // Calculate file stats
                fileCoverage.TotalLines = fileCoverage.Lines.Count;
                fileCoverage.CoveredLines = fileCoverage.Lines.Count(l => l.Hits > 0);
                fileCoverage.TotalBranches = fileCoverage.Lines
                    .Where(l => l.IsBranchPoint)
                    .Sum(l => l.BranchesTotal ?? 0);
                fileCoverage.CoveredBranches = fileCoverage.Lines
                    .Where(l => l.IsBranchPoint)
                    .Sum(l => l.BranchesCovered ?? 0);

                report.Files.Add(fileCoverage);
            }
        }

        // Calculate summary
        report.Summary.TotalLines = report.Files.Sum(f => f.TotalLines);
        report.Summary.CoveredLines = report.Files.Sum(f => f.CoveredLines);
        report.Summary.TotalBranches = report.Files.Sum(f => f.TotalBranches);
        report.Summary.CoveredBranches = report.Files.Sum(f => f.CoveredBranches);

        return report;
    }

    /// <summary>
    /// Try to parse a Cobertura XML file, returning null on failure.
    /// </summary>
    public static CoverageReport? TryParseFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            return ParseFile(filePath);
        }
        catch
        {
            return null;
        }
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Regex to match condition-coverage like "75% (3/4)".
    /// </summary>
    [GeneratedRegex(@"\((?<covered>\d+)/(?<total>\d+)\)", RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture)]
    private static partial Regex ConditionCoveragePattern();
}
