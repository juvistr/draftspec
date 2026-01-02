namespace DraftSpec.Cli.CoverageMap;

/// <summary>
/// Represents references extracted from a spec body via Roslyn AST analysis.
/// </summary>
public sealed class SpecReference
{
    /// <summary>
    /// Unique spec ID matching the format from DiscoveredSpec.
    /// Format: "relative/path/file.spec.csx:Context/Path/spec description"
    /// </summary>
    public required string SpecId { get; init; }

    /// <summary>
    /// The spec description text.
    /// </summary>
    public required string SpecDescription { get; init; }

    /// <summary>
    /// Parent context path (describe/context blocks).
    /// </summary>
    public IReadOnlyList<string> ContextPath { get; init; } = [];

    /// <summary>
    /// Method calls found in the spec body (HIGH confidence).
    /// </summary>
    public IReadOnlyList<MethodCall> MethodCalls { get; init; } = [];

    /// <summary>
    /// Type references found in the spec body (MEDIUM confidence).
    /// </summary>
    public IReadOnlyList<TypeReference> TypeReferences { get; init; } = [];

    /// <summary>
    /// Using directives from the spec file (LOW confidence).
    /// </summary>
    public IReadOnlyList<string> UsingNamespaces { get; init; } = [];

    /// <summary>
    /// Absolute path to the spec file.
    /// </summary>
    public required string SourceFile { get; init; }

    /// <summary>
    /// 1-based line number of the spec definition.
    /// </summary>
    public required int LineNumber { get; init; }
}
