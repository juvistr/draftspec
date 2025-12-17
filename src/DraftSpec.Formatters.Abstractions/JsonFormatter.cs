using System.Text.Json;

namespace DraftSpec.Formatters;

/// <summary>
/// Formats spec reports as JSON.
/// </summary>
public class JsonFormatter : IFormatter
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Create a JsonFormatter with default options (indented, camelCase).
    /// </summary>
    public JsonFormatter() : this(JsonOptionsProvider.Default)
    {
    }

    /// <summary>
    /// Create a JsonFormatter with custom serialization options.
    /// </summary>
    /// <param name="options">Custom JSON serializer options</param>
    public JsonFormatter(JsonSerializerOptions options)
    {
        _options = options ?? JsonOptionsProvider.Default;
    }

    public string FileExtension => ".json";

    public string Format(SpecReport report)
    {
        return JsonSerializer.Serialize(report, _options);
    }
}