using System.ComponentModel;

namespace DraftSpec.Cli.Formatters;

/// <summary>
/// A discovered spec definition.
/// </summary>
public sealed class SpecInfoDto
{
    /// <summary>
    /// Unique identifier for the spec, derived from context path and description.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The spec's description string (from `it("...")`).
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Full display name including context path (e.g., "UserService > CreateAsync > validates email").
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Array of parent context descriptions (from nested `describe()`/`context()` blocks).
    /// </summary>
    public required List<string> ContextPath { get; init; }

    /// <summary>
    /// Absolute path to the source file containing this spec.
    /// </summary>
    public required string SourceFile { get; init; }

    /// <summary>
    /// Relative path from the project root to the source file.
    /// </summary>
    public required string RelativeSourceFile { get; init; }

    /// <summary>
    /// Line number where the spec is defined (1-based).
    /// </summary>
    public required int LineNumber { get; init; }

    /// <summary>
    /// The spec type: regular, focused (fit), skipped (xit), pending (no body), or error (compilation failed).
    /// </summary>
    [Description("regular, focused, skipped, pending, or error")]
    public required string Type { get; init; }

    /// <summary>
    /// Whether the spec is pending (has no body).
    /// </summary>
    public required bool IsPending { get; init; }

    /// <summary>
    /// Whether the spec is explicitly skipped (xit).
    /// </summary>
    public required bool IsSkipped { get; init; }

    /// <summary>
    /// Whether the spec is focused (fit).
    /// </summary>
    public required bool IsFocused { get; init; }

    /// <summary>
    /// Tags applied to this spec via `.tag()` or inherited from parent contexts.
    /// </summary>
    public required List<string> Tags { get; init; }

    /// <summary>
    /// Compilation error message if the file failed to compile, null otherwise.
    /// </summary>
    public string? CompilationError { get; init; }
}
