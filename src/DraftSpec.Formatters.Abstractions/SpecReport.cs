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
    public List<SpecContext> Contexts { get; set; } = [];

    /// <summary>
    /// Parse a JSON report string into a SpecReport object.
    /// </summary>
    public static SpecReport FromJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Deserialize<SpecReport>(json, options)
            ?? throw new InvalidOperationException("Failed to parse JSON report");
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

    public bool Success => Failed == 0;
}

/// <summary>
/// A context (describe block) containing specs and nested contexts.
/// </summary>
public class SpecContext
{
    public string Description { get; set; } = "";
    public List<SpecResult> Specs { get; set; } = [];
    public List<SpecContext> Contexts { get; set; } = [];
}

/// <summary>
/// Result of a single spec (it block).
/// </summary>
public class SpecResult
{
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Error { get; set; }

    [JsonIgnore]
    public bool Passed => Status == "passed";

    [JsonIgnore]
    public bool Failed => Status == "failed";

    [JsonIgnore]
    public bool Pending => Status == "pending";

    [JsonIgnore]
    public bool Skipped => Status == "skipped";
}
