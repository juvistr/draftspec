using System.Text.Json;
using System.Text.Json.Serialization;
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

/// <summary>
/// Root report object containing all spec run results.
/// Mirrors DraftSpec.Formatters.SpecReport structure.
/// </summary>
public class SpecReport
{
    public DateTime Timestamp { get; set; }
    public string? Source { get; set; }
    public SpecSummary Summary { get; set; } = new();
    public List<SpecContextReport> Contexts { get; set; } = [];

    private const int MaxJsonSize = 10_000_000;

    public static SpecReport? FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        if (json.Length > MaxJsonSize)
            return null;

        try
        {
            return JsonSerializer.Deserialize<SpecReport>(json, JsonOptionsProvider.Secure);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Summary statistics for the spec run.
/// </summary>
public class SpecSummary
{
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Pending { get; set; }
    public int Skipped { get; set; }
    public double DurationMs { get; set; }

    [JsonIgnore] public bool Success => Failed == 0;
}

/// <summary>
/// A context (describe block) containing specs and nested contexts.
/// </summary>
public class SpecContextReport
{
    public string Description { get; set; } = "";
    public List<SpecResultReport> Specs { get; set; } = [];
    public List<SpecContextReport> Contexts { get; set; } = [];
}

/// <summary>
/// Result of a single spec (it block).
/// </summary>
public class SpecResultReport
{
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public double? DurationMs { get; set; }
    public string? Error { get; set; }
}