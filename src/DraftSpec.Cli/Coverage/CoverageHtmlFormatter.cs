using System.Text;
using System.Web;

namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Formats coverage reports as single-file HTML with collapsible file sections.
/// </summary>
/// <remarks>
/// Generates a self-contained HTML document with:
/// - Summary statistics with visual progress bars
/// - Collapsible file sections using &lt;details&gt;/&lt;summary&gt; (no JavaScript required)
/// - Line-by-line coverage highlighting (green=covered, red=uncovered, yellow=partial)
/// - Responsive design for various screen sizes
/// </remarks>
public class CoverageHtmlFormatter : ICoverageFormatter
{
    /// <inheritdoc />
    public string FileExtension => ".html";

    /// <inheritdoc />
    public string FormatName => "html";

    /// <inheritdoc />
    public string Format(CoverageReport report)
    {
        var sb = new StringBuilder();

        WriteHeader(sb, report);
        WriteSummary(sb, report);
        WriteFileList(sb, report);
        WriteFooter(sb, report);

        return sb.ToString();
    }

    private static void WriteHeader(StringBuilder sb, CoverageReport report)
    {
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("  <title>Coverage Report</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine(GetStyles());
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"container\">");
        sb.AppendLine("    <h1>Coverage Report</h1>");
    }

    private static void WriteSummary(StringBuilder sb, CoverageReport report)
    {
        var summary = report.Summary;

        sb.AppendLine("    <div class=\"summary\">");
        sb.AppendLine("      <div class=\"summary-grid\">");

        // Line coverage
        WriteMetricCard(sb, "Line Coverage",
            summary.LinePercent,
            summary.CoveredLines,
            summary.TotalLines);

        // Branch coverage
        WriteMetricCard(sb, "Branch Coverage",
            summary.BranchPercent,
            summary.CoveredBranches,
            summary.TotalBranches);

        // Files
        sb.AppendLine("        <div class=\"metric-card\">");
        sb.AppendLine("          <div class=\"metric-label\">Files</div>");
        sb.AppendLine($"          <div class=\"metric-value\">{report.Files.Count}</div>");
        sb.AppendLine("        </div>");

        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");
    }

    private static void WriteMetricCard(StringBuilder sb, string label, double percent, int covered, int total)
    {
        var statusClass = GetStatusClass(percent);

        sb.AppendLine("        <div class=\"metric-card\">");
        sb.AppendLine($"          <div class=\"metric-label\">{Escape(label)}</div>");
        sb.AppendLine($"          <div class=\"metric-value {statusClass}\">{percent:F1}%</div>");
        sb.AppendLine($"          <div class=\"metric-detail\">{covered} / {total}</div>");
        sb.AppendLine("          <div class=\"progress-bar\">");
        sb.AppendLine($"            <div class=\"progress-fill {statusClass}\" style=\"width: {Math.Min(percent, 100):F1}%\"></div>");
        sb.AppendLine("          </div>");
        sb.AppendLine("        </div>");
    }

    private static void WriteFileList(StringBuilder sb, CoverageReport report)
    {
        if (report.Files.Count == 0)
        {
            sb.AppendLine("    <p class=\"no-data\">No coverage data available.</p>");
            return;
        }

        sb.AppendLine("    <h2>Files</h2>");
        sb.AppendLine("    <div class=\"file-list\">");

        // Sort files by coverage percentage (lowest first to highlight issues)
        var sortedFiles = report.Files
            .OrderBy(f => f.LinePercent)
            .ThenBy(f => f.FilePath);

        foreach (var file in sortedFiles)
        {
            WriteFileSection(sb, file);
        }

        sb.AppendLine("    </div>");
    }

    private static void WriteFileSection(StringBuilder sb, FileCoverage file)
    {
        var statusClass = GetStatusClass(file.LinePercent);
        var fileName = Path.GetFileName(file.FilePath);
        var dirPath = Path.GetDirectoryName(file.FilePath) ?? "";

        sb.AppendLine("      <details class=\"file-section\">");
        sb.AppendLine("        <summary class=\"file-header\">");
        sb.AppendLine("          <div class=\"file-info\">");
        sb.AppendLine($"            <span class=\"file-name\">{Escape(fileName)}</span>");
        if (!string.IsNullOrEmpty(dirPath))
        {
            sb.AppendLine($"            <span class=\"file-path\">{Escape(dirPath)}</span>");
        }
        sb.AppendLine("          </div>");
        sb.AppendLine("          <div class=\"file-stats\">");
        sb.AppendLine($"            <span class=\"file-coverage {statusClass}\">{file.LinePercent:F1}%</span>");
        sb.AppendLine($"            <span class=\"file-lines\">{file.CoveredLines}/{file.TotalLines} lines</span>");
        sb.AppendLine("          </div>");
        sb.AppendLine("        </summary>");

        WriteLineDetails(sb, file);

        sb.AppendLine("      </details>");
    }

    private static void WriteLineDetails(StringBuilder sb, FileCoverage file)
    {
        if (file.Lines.Count == 0)
        {
            sb.AppendLine("        <div class=\"no-lines\">No line data available</div>");
            return;
        }

        sb.AppendLine("        <div class=\"line-table\">");
        sb.AppendLine("          <table>");
        sb.AppendLine("            <thead>");
        sb.AppendLine("              <tr>");
        sb.AppendLine("                <th class=\"line-num\">Line</th>");
        sb.AppendLine("                <th class=\"line-hits\">Hits</th>");
        sb.AppendLine("                <th class=\"line-status\">Status</th>");
        sb.AppendLine("              </tr>");
        sb.AppendLine("            </thead>");
        sb.AppendLine("            <tbody>");

        foreach (var line in file.Lines.OrderBy(l => l.LineNumber))
        {
            var rowClass = line.Status switch
            {
                CoverageStatus.Covered => "covered",
                CoverageStatus.Uncovered => "uncovered",
                CoverageStatus.Partial => "partial",
                _ => ""
            };

            var statusIcon = line.Status switch
            {
                CoverageStatus.Covered => "✓",
                CoverageStatus.Uncovered => "✗",
                CoverageStatus.Partial => "◐",
                _ => "?"
            };

            var branchInfo = "";
            if (line.IsBranchPoint && line.BranchesCovered.HasValue && line.BranchesTotal.HasValue)
            {
                branchInfo = $" ({line.BranchesCovered}/{line.BranchesTotal} branches)";
            }

            sb.AppendLine($"              <tr class=\"{rowClass}\">");
            sb.AppendLine($"                <td class=\"line-num\">{line.LineNumber}</td>");
            sb.AppendLine($"                <td class=\"line-hits\">{line.Hits}</td>");
            sb.AppendLine($"                <td class=\"line-status\">{statusIcon}{Escape(branchInfo)}</td>");
            sb.AppendLine("              </tr>");
        }

        sb.AppendLine("            </tbody>");
        sb.AppendLine("          </table>");
        sb.AppendLine("        </div>");
    }

    private static void WriteFooter(StringBuilder sb, CoverageReport report)
    {
        sb.AppendLine("    <div class=\"footer\">");
        sb.AppendLine($"      <p>Generated {report.Timestamp:yyyy-MM-dd HH:mm:ss} UTC by DraftSpec</p>");
        if (!string.IsNullOrEmpty(report.Source))
        {
            sb.AppendLine($"      <p>Source: <code>{Escape(report.Source)}</code></p>");
        }
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
    }

    private static string GetStatusClass(double percent) => percent switch
    {
        >= 80 => "good",
        >= 50 => "warning",
        _ => "danger"
    };

    private static string Escape(string text) => HttpUtility.HtmlEncode(text);

    private static string GetStyles() => """
        :root {
          --good: #22c55e;
          --warning: #eab308;
          --danger: #ef4444;
          --bg: #ffffff;
          --text: #1f2937;
          --border: #e5e7eb;
          --muted: #6b7280;
          --covered-bg: #dcfce7;
          --uncovered-bg: #fee2e2;
          --partial-bg: #fef3c7;
        }

        @media (prefers-color-scheme: dark) {
          :root {
            --bg: #1f2937;
            --text: #f9fafb;
            --border: #374151;
            --muted: #9ca3af;
            --covered-bg: #166534;
            --uncovered-bg: #991b1b;
            --partial-bg: #854d0e;
          }
        }

        * { box-sizing: border-box; margin: 0; padding: 0; }

        body {
          font-family: system-ui, -apple-system, sans-serif;
          background: var(--bg);
          color: var(--text);
          line-height: 1.5;
        }

        .container {
          max-width: 1200px;
          margin: 0 auto;
          padding: 2rem;
        }

        h1 { margin-bottom: 1.5rem; }
        h2 { margin: 2rem 0 1rem; font-size: 1.25rem; }

        .summary { margin-bottom: 2rem; }

        .summary-grid {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
          gap: 1rem;
        }

        .metric-card {
          background: var(--bg);
          border: 1px solid var(--border);
          border-radius: 8px;
          padding: 1rem;
        }

        .metric-label {
          color: var(--muted);
          font-size: 0.875rem;
          margin-bottom: 0.25rem;
        }

        .metric-value {
          font-size: 2rem;
          font-weight: bold;
        }

        .metric-detail {
          color: var(--muted);
          font-size: 0.875rem;
          margin-bottom: 0.5rem;
        }

        .progress-bar {
          height: 8px;
          background: var(--border);
          border-radius: 4px;
          overflow: hidden;
        }

        .progress-fill {
          height: 100%;
          transition: width 0.3s;
        }

        .good { color: var(--good); }
        .good.progress-fill { background: var(--good); }
        .warning { color: var(--warning); }
        .warning.progress-fill { background: var(--warning); }
        .danger { color: var(--danger); }
        .danger.progress-fill { background: var(--danger); }

        .file-list { display: flex; flex-direction: column; gap: 0.5rem; }

        .file-section {
          border: 1px solid var(--border);
          border-radius: 8px;
          overflow: hidden;
        }

        .file-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding: 0.75rem 1rem;
          cursor: pointer;
          background: var(--bg);
        }

        .file-header:hover { background: var(--border); }

        .file-info { display: flex; flex-direction: column; }
        .file-name { font-weight: 500; }
        .file-path { font-size: 0.75rem; color: var(--muted); }

        .file-stats { display: flex; gap: 1rem; align-items: center; }
        .file-coverage { font-weight: bold; }
        .file-lines { color: var(--muted); font-size: 0.875rem; }

        .line-table { overflow-x: auto; }

        .line-table table {
          width: 100%;
          border-collapse: collapse;
          font-family: ui-monospace, monospace;
          font-size: 0.875rem;
        }

        .line-table th {
          text-align: left;
          padding: 0.5rem;
          background: var(--border);
          font-weight: 500;
        }

        .line-table td {
          padding: 0.25rem 0.5rem;
          border-bottom: 1px solid var(--border);
        }

        .line-num { width: 60px; text-align: right; color: var(--muted); }
        .line-hits { width: 60px; text-align: right; }
        .line-status { width: 150px; }

        tr.covered { background: var(--covered-bg); }
        tr.uncovered { background: var(--uncovered-bg); }
        tr.partial { background: var(--partial-bg); }

        .no-data, .no-lines {
          padding: 2rem;
          text-align: center;
          color: var(--muted);
        }

        .footer {
          margin-top: 3rem;
          padding-top: 1rem;
          border-top: 1px solid var(--border);
          color: var(--muted);
          font-size: 0.875rem;
        }

        .footer code {
          background: var(--border);
          padding: 0.125rem 0.25rem;
          border-radius: 4px;
          font-size: 0.75rem;
        }
        """;
}
