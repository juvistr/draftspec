using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Options.Enums;

/// <summary>
/// Output format for the list command.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ListFormat>))]
public enum ListFormat
{
    /// <summary>
    /// Tree view showing nested context hierarchy.
    /// </summary>
    Tree,

    /// <summary>
    /// Flat list of all specs with full context paths.
    /// </summary>
    Flat,

    /// <summary>
    /// JSON format for programmatic consumption.
    /// </summary>
    Json
}

/// <summary>
/// Extension methods for <see cref="ListFormat"/>.
/// </summary>
public static class ListFormatExtensions
{
    /// <summary>
    /// Converts a string to a <see cref="ListFormat"/>.
    /// </summary>
    /// <param name="value">The string value (case-insensitive).</param>
    /// <returns>The parsed format.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid format.</exception>
    public static ListFormat ParseListFormat(this string value)
    {
        return value.ToLowerInvariant() switch
        {
            "tree" => ListFormat.Tree,
            "flat" => ListFormat.Flat,
            "json" => ListFormat.Json,
            _ => throw new ArgumentException(
                $"Unknown list format: '{value}'. Valid options: tree, flat, json",
                nameof(value))
        };
    }

    /// <summary>
    /// Tries to parse a string to a <see cref="ListFormat"/>.
    /// </summary>
    /// <param name="value">The string value (case-insensitive).</param>
    /// <param name="format">The parsed format, or default if parsing fails.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParseListFormat(this string? value, out ListFormat format)
    {
        format = ListFormat.Tree;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            format = value.ParseListFormat();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a <see cref="ListFormat"/> to its CLI string representation.
    /// </summary>
    public static string ToCliString(this ListFormat format) => format switch
    {
        ListFormat.Tree => "tree",
        ListFormat.Flat => "flat",
        ListFormat.Json => "json",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };
}
