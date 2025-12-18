namespace DraftSpec.Mcp.Models;

/// <summary>
/// Input for a single spec in a batch execution.
/// </summary>
public class BatchSpecInput
{
    /// <summary>
    /// Name or identifier for this spec (used in results mapping).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The spec content using describe/it/expect syntax.
    /// </summary>
    public required string Content { get; init; }
}

/// <summary>
/// Result of batch spec execution.
/// </summary>
public class BatchSpecResult
{
    /// <summary>
    /// Whether all specs passed.
    /// </summary>
    public bool AllPassed { get; init; }

    /// <summary>
    /// Total number of specs executed.
    /// </summary>
    public int TotalSpecs { get; init; }

    /// <summary>
    /// Number of specs that passed.
    /// </summary>
    public int PassedSpecs { get; init; }

    /// <summary>
    /// Number of specs that failed.
    /// </summary>
    public int FailedSpecs { get; init; }

    /// <summary>
    /// Total execution time in milliseconds.
    /// </summary>
    public double TotalDurationMs { get; init; }

    /// <summary>
    /// Individual results for each spec.
    /// </summary>
    public List<NamedSpecResult> Results { get; init; } = [];
}

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
