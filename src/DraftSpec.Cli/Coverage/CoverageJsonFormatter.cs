using System.Text.Json;
using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Formats coverage reports as JSON.
/// </summary>
public class CoverageJsonFormatter : ICoverageFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <inheritdoc />
    public string FileExtension => ".json";

    /// <inheritdoc />
    public string FormatName => "json";

    /// <inheritdoc />
    public string Format(CoverageReport report)
    {
        var dto = new CoverageReportDto
        {
            Timestamp = report.Timestamp,
            Source = report.Source,
            Summary = new CoverageSummaryDto
            {
                TotalLines = report.Summary.TotalLines,
                CoveredLines = report.Summary.CoveredLines,
                LinePercent = Math.Round(report.Summary.LinePercent, 2),
                TotalBranches = report.Summary.TotalBranches,
                CoveredBranches = report.Summary.CoveredBranches,
                BranchPercent = Math.Round(report.Summary.BranchPercent, 2)
            },
            Files = report.Files.Select(f => new FileCoverageDto
            {
                FilePath = f.FilePath,
                PackageName = f.PackageName,
                TotalLines = f.TotalLines,
                CoveredLines = f.CoveredLines,
                LinePercent = Math.Round(f.LinePercent, 2),
                TotalBranches = f.TotalBranches,
                CoveredBranches = f.CoveredBranches,
                Lines = f.Lines.Select(l => new LineCoverageDto
                {
                    LineNumber = l.LineNumber,
                    Hits = l.Hits,
                    Status = l.Status.ToString().ToLowerInvariant(),
                    IsBranchPoint = l.IsBranchPoint ? true : null,
                    BranchesCovered = l.BranchesCovered,
                    BranchesTotal = l.BranchesTotal
                }).ToList()
            }).ToList()
        };

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    #region DTOs for JSON Serialization

    private sealed class CoverageReportDto
    {
        public DateTime Timestamp { get; set; }
        public string? Source { get; set; }
        public CoverageSummaryDto Summary { get; set; } = new();
        public List<FileCoverageDto> Files { get; set; } = [];
    }

    private sealed class CoverageSummaryDto
    {
        public int TotalLines { get; set; }
        public int CoveredLines { get; set; }
        public double LinePercent { get; set; }
        public int TotalBranches { get; set; }
        public int CoveredBranches { get; set; }
        public double BranchPercent { get; set; }
    }

    private sealed class FileCoverageDto
    {
        public string FilePath { get; set; } = "";
        public string? PackageName { get; set; }
        public int TotalLines { get; set; }
        public int CoveredLines { get; set; }
        public double LinePercent { get; set; }
        public int TotalBranches { get; set; }
        public int CoveredBranches { get; set; }
        public List<LineCoverageDto> Lines { get; set; } = [];
    }

    private sealed class LineCoverageDto
    {
        public int LineNumber { get; set; }
        public int Hits { get; set; }
        public string Status { get; set; } = "";
        public bool? IsBranchPoint { get; set; }
        public int? BranchesCovered { get; set; }
        public int? BranchesTotal { get; set; }
    }

    #endregion
}
