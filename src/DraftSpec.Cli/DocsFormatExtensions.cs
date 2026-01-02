namespace DraftSpec.Cli.Options.Enums;

/// <summary>
/// Extension methods for <see cref="DocsFormat"/>.
/// </summary>
public static class DocsFormatExtensions
{
    /// <summary>
    /// Converts a string to a <see cref="DocsFormat"/>.
    /// </summary>
    /// <param name="value">The string value (case-insensitive).</param>
    /// <returns>The parsed format.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid format.</exception>
    public static DocsFormat ParseDocsFormat(this string value)
    {
        return value.ToLowerInvariant() switch
        {
            "markdown" or "md" => DocsFormat.Markdown,
            "html" => DocsFormat.Html,
            _ => throw new ArgumentException(
                $"Unknown docs format: '{value}'. Valid options: markdown, html",
                nameof(value))
        };
    }

    /// <summary>
    /// Tries to parse a string to a <see cref="DocsFormat"/>.
    /// </summary>
    /// <param name="value">The string value (case-insensitive).</param>
    /// <param name="format">The parsed format, or default if parsing fails.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParseDocsFormat(this string? value, out DocsFormat format)
    {
        format = DocsFormat.Markdown;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            format = value.ParseDocsFormat();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a <see cref="DocsFormat"/> to its CLI string representation.
    /// </summary>
    public static string ToCliString(this DocsFormat format) => format switch
    {
        DocsFormat.Markdown => "markdown",
        DocsFormat.Html => "html",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };
}
