namespace DraftSpec.Cli.CoverageMap;

/// <summary>
/// Information about a spec that covers a method.
/// </summary>
public sealed class SpecCoverageInfo
{
    /// <summary>
    /// The spec's unique ID.
    /// </summary>
    public required string SpecId { get; init; }

    /// <summary>
    /// Human-readable display name for the spec.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// The confidence level of this spec's coverage.
    /// </summary>
    public required CoverageConfidence Confidence { get; init; }

    /// <summary>
    /// Explanation of why this spec covers the method.
    /// Example: "Direct call: CreateAsync()"
    /// </summary>
    public string? MatchReason { get; init; }

    /// <summary>
    /// Relative path to the spec file.
    /// </summary>
    public string? SpecFile { get; init; }

    /// <summary>
    /// Line number in the spec file.
    /// </summary>
    public int LineNumber { get; init; }
}
