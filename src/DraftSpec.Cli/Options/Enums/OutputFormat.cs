using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Options.Enums;

/// <summary>
/// Output format for spec execution results.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<OutputFormat>))]
public enum OutputFormat
{
    /// <summary>
    /// Human-readable console output with colors and progress indicators.
    /// </summary>
    Console,

    /// <summary>
    /// JSON format for programmatic consumption.
    /// </summary>
    Json,

    /// <summary>
    /// Markdown format for documentation.
    /// </summary>
    Markdown,

    /// <summary>
    /// HTML format for web viewing.
    /// </summary>
    Html,

    /// <summary>
    /// JUnit XML format for CI/CD integration.
    /// </summary>
    JUnit
}

/// <summary>
/// Extension methods for <see cref="OutputFormat"/>.
/// </summary>
public static class OutputFormatExtensions
{
    /// <summary>
    /// Converts a string to an <see cref="OutputFormat"/>.
    /// </summary>
    /// <param name="value">The string value (case-insensitive).</param>
    /// <returns>The parsed format.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid format.</exception>
    public static OutputFormat ParseOutputFormat(this string value)
    {
        return value.ToLowerInvariant() switch
        {
            "console" => OutputFormat.Console,
            "json" => OutputFormat.Json,
            "markdown" => OutputFormat.Markdown,
            "html" => OutputFormat.Html,
            "junit" => OutputFormat.JUnit,
            _ => throw new ArgumentException(
                $"Unknown output format: '{value}'. Valid options: console, json, markdown, html, junit",
                nameof(value))
        };
    }

    /// <summary>
    /// Tries to parse a string to an <see cref="OutputFormat"/>.
    /// </summary>
    /// <param name="value">The string value (case-insensitive).</param>
    /// <param name="format">The parsed format, or default if parsing fails.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParseOutputFormat(this string? value, out OutputFormat format)
    {
        format = OutputFormat.Console;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            format = value.ParseOutputFormat();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Converts an <see cref="OutputFormat"/> to its CLI string representation.
    /// </summary>
    public static string ToCliString(this OutputFormat format) => format switch
    {
        OutputFormat.Console => "console",
        OutputFormat.Json => "json",
        OutputFormat.Markdown => "markdown",
        OutputFormat.Html => "html",
        OutputFormat.JUnit => "junit",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };
}
