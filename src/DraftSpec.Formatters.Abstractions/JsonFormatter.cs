using System.Text.Json;
using System.Text.Json.Serialization;

namespace DraftSpec.Formatters;

/// <summary>
/// Formats spec reports as JSON.
/// </summary>
public class JsonFormatter : IFormatter
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Create a JsonFormatter with default options (indented, camelCase).
    /// </summary>
    public JsonFormatter() : this(DefaultOptions)
    {
    }

    /// <summary>
    /// Create a JsonFormatter with custom serialization options.
    /// </summary>
    /// <param name="options">Custom JSON serializer options</param>
    public JsonFormatter(JsonSerializerOptions options)
    {
        _options = options ?? DefaultOptions;
    }

    public string FileExtension => ".json";

    public string Format(SpecReport report)
    {
        return JsonSerializer.Serialize(report, _options);
    }
}
