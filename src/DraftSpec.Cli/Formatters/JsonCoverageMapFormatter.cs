using System.Text.Json;
using System.Text.Json.Serialization;
using DraftSpec.Cli.CoverageMap;

namespace DraftSpec.Cli.Formatters;

/// <summary>
/// Formats coverage map results as JSON for tooling integration.
/// </summary>
public sealed class JsonCoverageMapFormatter : ICoverageMapFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Format(CoverageMapResult result, bool gapsOnly)
    {
        var methodsToShow = gapsOnly ? result.UncoveredMethods : result.AllMethods;

        var output = new CoverageMapOutputDto
        {
            Summary = new CoverageMapSummaryDto
            {
                TotalMethods = result.Summary.TotalMethods,
                CoveragePercentage = Math.Round(result.Summary.CoveragePercentage, 1),
                ByConfidence = new Dictionary<string, int>
                {
                    ["high"] = result.Summary.HighConfidence,
                    ["medium"] = result.Summary.MediumConfidence,
                    ["low"] = result.Summary.LowConfidence,
                    ["none"] = result.Summary.Uncovered
                }
            },
            SourcePath = result.SourcePath,
            SpecPath = result.SpecPath,
            GapsOnly = gapsOnly,
            Methods = methodsToShow.Select(ToDto).ToList()
        };

        return JsonSerializer.Serialize(output, JsonOptions);
    }

    private static MethodCoverageDto ToDto(MethodCoverage coverage)
    {
        return new MethodCoverageDto
        {
            FullyQualifiedName = coverage.Method.FullyQualifiedName,
            ClassName = coverage.Method.ClassName,
            MethodName = coverage.Method.MethodName,
            Signature = coverage.Method.Signature,
            Namespace = coverage.Method.Namespace,
            SourceFile = coverage.Method.SourceFile,
            LineNumber = coverage.Method.LineNumber,
            IsAsync = coverage.Method.IsAsync,
            Confidence = coverage.Confidence.ToString().ToLowerInvariant(),
            CoveringSpecs = coverage.CoveringSpecs.Select(s => new SpecCoverageDto
            {
                SpecId = s.SpecId,
                DisplayName = s.DisplayName,
                Confidence = s.Confidence.ToString().ToLowerInvariant(),
                MatchReason = s.MatchReason,
                SpecFile = s.SpecFile,
                LineNumber = s.LineNumber
            }).ToList()
        };
    }

    // DTOs for JSON serialization
    private sealed class CoverageMapOutputDto
    {
        public CoverageMapSummaryDto Summary { get; init; } = new();
        public string? SourcePath { get; init; }
        public string? SpecPath { get; init; }
        public bool GapsOnly { get; init; }
        public List<MethodCoverageDto> Methods { get; init; } = [];
    }

    private sealed class CoverageMapSummaryDto
    {
        public int TotalMethods { get; init; }
        public double CoveragePercentage { get; init; }
        public Dictionary<string, int> ByConfidence { get; init; } = [];
    }

    private sealed class MethodCoverageDto
    {
        public string FullyQualifiedName { get; init; } = "";
        public string ClassName { get; init; } = "";
        public string MethodName { get; init; } = "";
        public string Signature { get; init; } = "";
        public string Namespace { get; init; } = "";
        public string SourceFile { get; init; } = "";
        public int LineNumber { get; init; }
        public bool IsAsync { get; init; }
        public string Confidence { get; init; } = "";
        public List<SpecCoverageDto> CoveringSpecs { get; init; } = [];
    }

    private sealed class SpecCoverageDto
    {
        public string SpecId { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string Confidence { get; init; } = "";
        public string? MatchReason { get; init; }
        public string? SpecFile { get; init; }
        public int LineNumber { get; init; }
    }
}
