using System.Text.Json.Serialization;

namespace DraftSpec.Formatters.Abstractions;

/// <summary>
/// Result of a single spec (it block).
/// </summary>
public class SpecResultReport
{
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public double? DurationMs { get; set; }
    public string? Error { get; set; }

    [JsonIgnore] public bool Passed => Status == SpecStatusNames.Passed;

    [JsonIgnore] public bool Failed => Status == SpecStatusNames.Failed;

    [JsonIgnore] public bool Pending => Status == SpecStatusNames.Pending;

    [JsonIgnore] public bool Skipped => Status == SpecStatusNames.Skipped;
}
