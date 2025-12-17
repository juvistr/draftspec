using System.Text.Json;
using System.Text.Json.Serialization;

namespace DraftSpec.Formatters;

/// <summary>
/// Root report object containing all spec run results.
/// </summary>
public class SpecReport
{
    public DateTime Timestamp { get; set; }
    public string? Source { get; set; }
    public SpecSummary Summary { get; set; } = new();
    public List<SpecContextReport> Contexts { get; set; } = [];

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

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Security: Limit nesting depth to prevent stack overflow
            MaxDepth = 64
        };
        return JsonSerializer.Deserialize<SpecReport>(json, options)
               ?? throw new InvalidOperationException("Failed to parse JSON report");
    }

    /// <summary>
    /// Serialize this report to a JSON string.
    /// </summary>
    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return JsonSerializer.Serialize(this, options);
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

    public bool Success => Failed == 0;
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

    [JsonIgnore] public bool Passed => Status == "passed";

    [JsonIgnore] public bool Failed => Status == "failed";

    [JsonIgnore] public bool Pending => Status == "pending";

    [JsonIgnore] public bool Skipped => Status == "skipped";
}