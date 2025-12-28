namespace DraftSpec.TestingPlatform;

/// <summary>
/// Result of statically parsing a CSX spec file.
/// </summary>
/// <remarks>
/// Contains specs discovered via syntax tree analysis, along with
/// any warnings about patterns that couldn't be fully analyzed
/// (e.g., dynamic descriptions, loop-generated specs).
/// </remarks>
public sealed class StaticParseResult
{
    /// <summary>
    /// Specs discovered via static parsing.
    /// </summary>
    public IReadOnlyList<StaticSpec> Specs { get; init; } = [];

    /// <summary>
    /// Warnings about patterns that couldn't be fully analyzed.
    /// </summary>
    /// <remarks>
    /// Examples: dynamic descriptions, loop-generated specs, conditional specs.
    /// </remarks>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>
    /// True if all spec patterns in the file were successfully analyzed.
    /// False if some patterns couldn't be parsed (dynamic descriptions, loops, etc.).
    /// </summary>
    public bool IsComplete { get; init; } = true;
}
