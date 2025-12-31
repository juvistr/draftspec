using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using DraftSpec.Formatters;
using ModelContextProtocol.Server;

namespace DraftSpec.Mcp.Tools;

/// <summary>
/// Result of parsing a natural language assertion.
/// </summary>
public record AssertionParseResult
{
    /// <summary>
    /// Whether parsing was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The generated DraftSpec expect() code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Confidence score (0.0 to 1.0) indicating how well the pattern matched.
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Description of the pattern that matched (if successful).
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Error message if parsing failed.
    /// </summary>
    public string? Error { get; init; }
}
