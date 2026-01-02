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

    [JsonIgnore] public bool Passed => string.Equals(Status, SpecStatusNames.Passed, StringComparison.Ordinal);

    [JsonIgnore] public bool Failed => string.Equals(Status, SpecStatusNames.Failed, StringComparison.Ordinal);

    [JsonIgnore] public bool Pending => string.Equals(Status, SpecStatusNames.Pending, StringComparison.Ordinal);

    [JsonIgnore] public bool Skipped => string.Equals(Status, SpecStatusNames.Skipped, StringComparison.Ordinal);
}
