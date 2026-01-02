using System.Text.Json;
using DraftSpec.Formatters.Abstractions;

namespace DraftSpec.Formatters;

/// <summary>
/// Root report object containing all spec run results.
/// </summary>
public class SpecReport
{
    public DateTime Timestamp { get; set; }
    public string? Source { get; set; }
    public SpecSummary Summary { get; set; } = new();
    public IList<SpecContextReport> Contexts { get; set; } = [];

    /// <summary>
    /// Maximum allowed JSON payload size (10MB).
    /// Prevents memory exhaustion from malicious payloads.
    /// </summary>
    private const int MaxJsonSize = 10_000_000;

    /// <summary>
    /// Parse a JSON report string into a SpecReport object.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when json is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when payload exceeds size limit</exception>
    /// <exception cref="JsonException">Thrown when JSON is invalid or exceeds depth limit</exception>
    public static SpecReport FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        // Security: Check size BEFORE parsing to prevent memory exhaustion
        if (json.Length > MaxJsonSize)
            throw new InvalidOperationException(
                $"Report too large: {json.Length:N0} bytes exceeds maximum of {MaxJsonSize:N0} bytes");

        // Security: Use secure options with MaxDepth limit to prevent stack overflow
        return JsonSerializer.Deserialize<SpecReport>(json, JsonOptionsProvider.Secure)
               ?? throw new InvalidOperationException("Failed to parse JSON report");
    }

    /// <summary>
    /// Serialize this report to a JSON string.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptionsProvider.Default);
    }
}
