using System.Text.Json.Serialization;

namespace DraftSpec.Mcp.Models;

/// <summary>
/// A single change between baseline and current spec results.
/// </summary>
public class SpecChange
{
    /// <summary>
    /// Full context path of the spec (e.g., "Calculator > add > returns sum").
    /// </summary>
    public required string SpecPath { get; init; }

    /// <summary>
    /// Type of change.
    /// </summary>
    public ChangeType Type { get; init; }

    /// <summary>
    /// Status in the baseline run.
    /// </summary>
    public string? OldStatus { get; init; }

    /// <summary>
    /// Status in the current run.
    /// </summary>
    public string? NewStatus { get; init; }

    /// <summary>
    /// Error message if the spec failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
