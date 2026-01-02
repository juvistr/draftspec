namespace DraftSpec.Cli.CoverageMap;

/// <summary>
/// Coverage information for a single source method.
/// </summary>
public sealed class MethodCoverage
{
    /// <summary>
    /// The source method being analyzed.
    /// </summary>
    public required SourceMethod Method { get; init; }

    /// <summary>
    /// The highest confidence level from all covering specs.
    /// </summary>
    public required CoverageConfidence Confidence { get; init; }

    /// <summary>
    /// Specs that cover this method, ordered by confidence (highest first).
    /// </summary>
    public IReadOnlyList<SpecCoverageInfo> CoveringSpecs { get; init; } = [];
}
