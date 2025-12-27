namespace DraftSpec.TestingPlatform;

/// <summary>
/// Represents a discovered spec with its metadata for MTP test enumeration.
/// </summary>
/// <remarks>
/// This is a flattened view of a spec from the SpecContext tree, containing
/// all information needed for MTP discovery and execution.
/// </remarks>
public sealed class DiscoveredSpec
{
    /// <summary>
    /// Stable, unique identifier for this spec.
    /// Format: relative/path/file.spec.csx:Context/Path/spec description
    /// </summary>
    /// <example>
    /// specs/UserService.spec.csx:UserService/CreateAsync/creates a user with valid data
    /// </example>
    public required string Id { get; init; }

    /// <summary>
    /// The spec description (the "it should..." text).
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Full display name including context path.
    /// </summary>
    /// <example>
    /// UserService > CreateAsync > creates a user with valid data
    /// </example>
    public required string DisplayName { get; init; }

    /// <summary>
    /// The context path as an array of context descriptions.
    /// Does not include the spec description itself.
    /// </summary>
    /// <example>
    /// ["UserService", "CreateAsync"]
    /// </example>
    public required IReadOnlyList<string> ContextPath { get; init; }

    /// <summary>
    /// Absolute path to the source CSX file.
    /// </summary>
    public required string SourceFile { get; init; }

    /// <summary>
    /// Relative path to the source CSX file from the project root.
    /// Used in the stable ID.
    /// </summary>
    public required string RelativeSourceFile { get; init; }

    /// <summary>
    /// True if the spec has no body (placeholder for future implementation).
    /// </summary>
    public bool IsPending { get; init; }

    /// <summary>
    /// True if the spec is explicitly skipped (via xit()).
    /// </summary>
    public bool IsSkipped { get; init; }

    /// <summary>
    /// True if the spec is focused (via fit()).
    /// </summary>
    public bool IsFocused { get; init; }

    /// <summary>
    /// Tags associated with this spec for filtering.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Reference to the original spec definition for execution.
    /// </summary>
    internal SpecDefinition? SpecDefinition { get; init; }

    /// <summary>
    /// Reference to the containing context for execution.
    /// </summary>
    internal SpecContext? Context { get; init; }
}
