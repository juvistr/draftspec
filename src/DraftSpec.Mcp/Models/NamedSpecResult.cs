using DraftSpec.Formatters;

namespace DraftSpec.Mcp.Models;

/// <summary>
/// Result of a single named spec in a batch.
/// </summary>
public class NamedSpecResult
{
    /// <summary>
    /// Name or identifier of the spec (from BatchSpecInput.Name).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether this spec passed.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Process exit code.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Parsed spec report from execution.
    /// </summary>
    public SpecReport? Report { get; init; }

    /// <summary>
    /// Structured error information if failed.
    /// </summary>
    public SpecError? Error { get; init; }

    /// <summary>
    /// Execution time in milliseconds.
    /// </summary>
    public double DurationMs { get; init; }
}
