namespace DraftSpec.Cli.Options.Enums;

/// <summary>
/// Extension methods for <see cref="CoverageMapFormat"/>.
/// </summary>
public static class CoverageMapFormatExtensions
{
    /// <summary>
    /// Converts a string to a <see cref="CoverageMapFormat"/>.
    /// </summary>
    /// <param name="value">The string value (case-insensitive).</param>
    /// <returns>The parsed format.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid format.</exception>
    public static CoverageMapFormat ParseCoverageMapFormat(this string value)
    {
        return value.ToLowerInvariant() switch
        {
            "console" => CoverageMapFormat.Console,
            "json" => CoverageMapFormat.Json,
            _ => throw new ArgumentException(
                $"Unknown coverage-map format: '{value}'. Valid options: console, json",
                nameof(value))
        };
    }

    /// <summary>
    /// Tries to parse a string to a <see cref="CoverageMapFormat"/>.
    /// </summary>
    /// <param name="value">The string value (case-insensitive).</param>
    /// <param name="format">The parsed format, or default if parsing fails.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParseCoverageMapFormat(this string? value, out CoverageMapFormat format)
    {
        format = CoverageMapFormat.Console;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            format = value.ParseCoverageMapFormat();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a <see cref="CoverageMapFormat"/> to its CLI string representation.
    /// </summary>
    public static string ToCliString(this CoverageMapFormat format) => format switch
    {
        CoverageMapFormat.Console => "console",
        CoverageMapFormat.Json => "json",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };
}
