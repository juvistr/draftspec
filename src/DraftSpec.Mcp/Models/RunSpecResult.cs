using DraftSpec.Formatters;

namespace DraftSpec.Mcp.Models;

/// <summary>
/// Result of executing a spec through the MCP server.
/// </summary>
public class RunSpecResult
{
    /// <summary>
    /// Whether the spec execution completed successfully (exit code 0 and valid report).
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Process exit code. 0 indicates success, non-zero indicates failure.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Parsed spec report from the execution. Null if parsing failed.
    /// </summary>
    public SpecReport? Report { get; init; }

    /// <summary>
    /// Structured error information for AI parsing. Null if no error.
    /// </summary>
    public SpecError? Error { get; init; }

    /// <summary>
    /// Console output from the spec execution (stdout).
    /// </summary>
    public string? ConsoleOutput { get; init; }

    /// <summary>
    /// Error output from the spec execution (stderr).
    /// Kept for backward compatibility - prefer using Error for structured access.
    /// </summary>
    public string? ErrorOutput { get; init; }

    /// <summary>
    /// Total execution time in milliseconds.
    /// </summary>
    public double DurationMs { get; init; }
}
