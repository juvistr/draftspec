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
        var output = new ListOutput
        {
            Specs = specs.Select(MapSpec).ToList(),
            Summary = new ListSummary
            {
                TotalSpecs = specs.Count,
                FocusedCount = specs.Count(s => s.IsFocused),
                SkippedCount = specs.Count(s => s.IsSkipped),
                PendingCount = specs.Count(s => s.IsPending),
                ErrorCount = specs.Count(s => s.HasCompilationError),
                TotalFiles = specs.Select(s => s.RelativeSourceFile).Distinct().Count(),
                FilesWithErrors = errors.Count
            },
            Errors = errors.Select(e => new ListError
            {
                File = e.RelativeSourceFile,
                Message = e.Message
            }).ToList()
        };

        return JsonSerializer.Serialize(output, JsonOptions);
    }

    private static SpecInfo MapSpec(DiscoveredSpec spec) => new()
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

    // DTO classes for JSON serialization
    private sealed class ListOutput
    {
        public required List<SpecInfo> Specs { get; init; }
        public required ListSummary Summary { get; init; }
        public required List<ListError> Errors { get; init; }
    }

    private sealed class SpecInfo
    {
        public required string Id { get; init; }
        public required string Description { get; init; }
        public required string DisplayName { get; init; }
        public required List<string> ContextPath { get; init; }
        public required string SourceFile { get; init; }
        public required string RelativeSourceFile { get; init; }
        public required int LineNumber { get; init; }
        public required string Type { get; init; }
        public required bool IsPending { get; init; }
        public required bool IsSkipped { get; init; }
        public required bool IsFocused { get; init; }
        public required List<string> Tags { get; init; }
        public string? CompilationError { get; init; }
    }

    private sealed class ListSummary
    {
        public required int TotalSpecs { get; init; }
        public required int FocusedCount { get; init; }
        public required int SkippedCount { get; init; }
        public required int PendingCount { get; init; }
        public required int ErrorCount { get; init; }
        public required int TotalFiles { get; init; }
        public required int FilesWithErrors { get; init; }
    }

    private sealed class ListError
    {
        public required string File { get; init; }
        public required string Message { get; init; }
    }
}
