using System.Text.Json;
using System.Text.Json.Serialization;

namespace DraftSpec.Formatters;

/// <summary>
/// Centralized JSON serializer options to ensure consistent serialization across all components.
/// </summary>
public static class JsonOptionsProvider
{
    /// <summary>
    /// Default options for JSON serialization (indented output, camelCase, ignores null values).
    /// Use for human-readable output like reports and formatted responses.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Options for JSON deserialization with security limits.
    /// Use when parsing untrusted JSON input to prevent DoS attacks.
    /// </summary>
    public static JsonSerializerOptions Secure { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        MaxDepth = 64
    };

    /// <summary>
    /// Compact options for JSON serialization (no indentation, camelCase).
    /// Use for machine-to-machine communication where size matters.
    /// </summary>
    public static JsonSerializerOptions Compact { get; } = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
