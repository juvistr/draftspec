using System.Text.Json.Serialization;

namespace DraftSpec.Mcp.Models;

/// <summary>
/// Structured error information for AI parsing and automated handling.
/// </summary>
public class SpecError
{
    /// <summary>
    /// The category of error that occurred.
    /// </summary>
    public ErrorCategory Category { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Stack trace if available.
    /// </summary>
    public string? StackTrace { get; init; }

    /// <summary>
    /// Source file where the error occurred.
    /// </summary>
    public string? SourceFile { get; init; }

    /// <summary>
    /// Line number in the source file.
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// Column number in the source file.
    /// </summary>
    public int? ColumnNumber { get; init; }

    /// <summary>
    /// For assertion errors: the expected value.
    /// </summary>
    public string? ExpectedValue { get; init; }

    /// <summary>
    /// For assertion errors: the actual value received.
    /// </summary>
    public string? ActualValue { get; init; }

    /// <summary>
    /// For compilation errors: the compiler error code (e.g., CS1002).
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// The spec description where the error occurred, if known.
    /// </summary>
    public string? SpecDescription { get; init; }
}
