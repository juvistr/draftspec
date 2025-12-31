using DraftSpec.Cli.Configuration;

namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Checks coverage results against configured thresholds.
/// </summary>
public class CoverageThresholdChecker
{
    /// <summary>
    /// Check coverage against thresholds.
    /// </summary>
    public CoverageThresholdResult Check(CoverageReport report, ThresholdsConfig thresholds)
    {
        var failures = new List<string>();

        if (thresholds.Line.HasValue && report.Summary.LinePercent < thresholds.Line.Value)
        {
            failures.Add($"Line coverage {report.Summary.LinePercent:F1}% is below threshold of {thresholds.Line.Value}%");
        }

        if (thresholds.Branch.HasValue && report.Summary.BranchPercent < thresholds.Branch.Value)
        {
            failures.Add($"Branch coverage {report.Summary.BranchPercent:F1}% is below threshold of {thresholds.Branch.Value}%");
        }

        return new CoverageThresholdResult
        {
            Passed = failures.Count == 0,
            ActualLinePercent = report.Summary.LinePercent,
            RequiredLinePercent = thresholds.Line,
            ActualBranchPercent = report.Summary.BranchPercent,
            RequiredBranchPercent = thresholds.Branch,
            Failures = failures
        };
    }

    /// <summary>
    /// Check coverage file against thresholds.
    /// </summary>
    public CoverageThresholdResult? CheckFile(string coverageFilePath, ThresholdsConfig thresholds)
    {
        var report = CoberturaParser.TryParseFile(coverageFilePath);
        if (report == null)
            return null;

        return Check(report, thresholds);
    }
}
