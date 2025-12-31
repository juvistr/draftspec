using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;

namespace DraftSpec.Cli;

/// <summary>
/// Merges multiple spec reports into a single combined report.
/// Extracted for testability.
/// </summary>
public static class ReportMerger
{
    /// <summary>
    /// Merge multiple JSON report strings into a single combined report.
    /// </summary>
    public static SpecReport Merge(IEnumerable<string> jsonOutputs, string source)
    {
        var outputs = jsonOutputs.ToList();

        if (outputs.Count == 0)
            return CreateEmptyReport(source);

        // Parse all reports
        var reports = outputs
            .Where(json => !string.IsNullOrWhiteSpace(json))
            .Select(SpecReport.FromJson)
            .ToList();

        if (reports.Count == 0)
            return CreateEmptyReport(source);

        // Single report - just update source
        if (reports.Count == 1)
        {
            reports[0].Source = source;
            return reports[0];
        }

        // Merge multiple reports
        return new SpecReport
        {
            Timestamp = reports.Min(r => r.Timestamp),
            Source = source,
            Contexts = reports.SelectMany(r => r.Contexts).ToList(),
            Summary = new SpecSummary
            {
                Total = reports.Sum(r => r.Summary.Total),
                Passed = reports.Sum(r => r.Summary.Passed),
                Failed = reports.Sum(r => r.Summary.Failed),
                Pending = reports.Sum(r => r.Summary.Pending),
                Skipped = reports.Sum(r => r.Summary.Skipped),
                DurationMs = reports.Sum(r => r.Summary.DurationMs)
            }
        };
    }

    private static SpecReport CreateEmptyReport(string source) => new()
    {
        Timestamp = DateTime.UtcNow,
        Source = source,
        Summary = new SpecSummary(),
        Contexts = []
    };
}
