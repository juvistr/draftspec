namespace DraftSpec.TestingPlatform;

/// <summary>
/// Represents a spec discovered via static syntax parsing.
/// </summary>
/// <remarks>
/// Static specs are discovered by parsing CSX files without execution.
/// This allows discovering spec structure even when files have compilation errors.
/// </remarks>
public sealed class StaticSpec
{
    /// <summary>
    /// The spec description (the "it should..." text).
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The context path as a list of context descriptions.
    /// Does not include the spec description itself.
    /// </summary>
    public required IReadOnlyList<string> ContextPath { get; init; }

    /// <summary>
    /// Line number in the source file where this spec was defined (1-based).
    /// </summary>
    public required int LineNumber { get; init; }

    /// <summary>
    /// The type of spec (regular, focused, or skipped).
    /// </summary>
    public required StaticSpecType Type { get; init; }

    /// <summary>
    /// True if the spec has no body (placeholder for future implementation).
    /// </summary>
    public bool IsPending { get; init; }
}
