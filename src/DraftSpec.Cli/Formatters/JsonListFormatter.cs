using System.Text.Json;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Formatters;

/// <summary>
/// Formats specs as JSON for tooling integration.
/// </summary>
public sealed class JsonListFormatter : IListFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Format(IReadOnlyList<DiscoveredSpec> specs, IReadOnlyList<DiscoveryError> errors)
    {
        var output = new ListOutputDto
        {
            Specs = specs.Select(MapSpec).ToList(),
            Summary = new ListSummaryDto
            {
                TotalSpecs = specs.Count,
                FocusedCount = specs.Count(s => s.IsFocused),
                SkippedCount = specs.Count(s => s.IsSkipped),
                PendingCount = specs.Count(s => s.IsPending),
                ErrorCount = specs.Count(s => s.HasCompilationError),
                TotalFiles = specs.Select(s => s.RelativeSourceFile).Distinct().Count(),
                FilesWithErrors = errors.Count
            },
            Errors = errors.Select(e => new ListErrorDto
            {
                File = e.RelativeSourceFile,
                Message = e.Message
            }).ToList()
        };

        return JsonSerializer.Serialize(output, JsonOptions);
    }

    private static SpecInfoDto MapSpec(DiscoveredSpec spec) => new()
    {
        Id = spec.Id,
        Description = spec.Description,
        DisplayName = spec.DisplayName,
        ContextPath = spec.ContextPath.ToList(),
        SourceFile = spec.SourceFile,
        RelativeSourceFile = spec.RelativeSourceFile,
        LineNumber = spec.LineNumber,
        Type = GetSpecType(spec),
        IsPending = spec.IsPending,
        IsSkipped = spec.IsSkipped,
        IsFocused = spec.IsFocused,
        Tags = spec.Tags.ToList(),
        CompilationError = spec.CompilationError
    };

    private static string GetSpecType(DiscoveredSpec spec)
    {
        if (spec.HasCompilationError) return "error";
        if (spec.IsFocused) return "focused";
        if (spec.IsSkipped) return "skipped";
        if (spec.IsPending) return "pending";
        return "regular";
    }
}
