namespace DraftSpec.Cli.CoverageMap;

/// <summary>
/// Represents a type reference found in a spec body.
/// </summary>
public sealed class TypeReference
{
    /// <summary>
    /// The referenced type name.
    /// Example: "TodoService" or "List&lt;Todo&gt;"
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// How the type was referenced.
    /// </summary>
    public ReferenceKind Kind { get; init; }

    /// <summary>
    /// 1-based line number of the reference.
    /// </summary>
    public int LineNumber { get; init; }
}
