using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Options.Enums;

/// <summary>
/// Extension methods for <see cref="CoverageFormat"/>.
/// </summary>
public static class CoverageFormatExtensions
{
    /// <summary>
    /// Converts a string to a <see cref="CoverageFormat"/>.
    /// </summary>
    /// <param name="value">The string value (case-insensitive).</param>
    /// <returns>The parsed format.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid format.</exception>
    public static CoverageFormat ParseCoverageFormat(this string value)
    {
        return value.ToLowerInvariant() switch
        {
            "cobertura" => CoverageFormat.Cobertura,
            "xml" => CoverageFormat.Xml,
            "coverage" => CoverageFormat.Coverage,
            _ => throw new ArgumentException(
                $"Unknown coverage format: '{value}'. Valid options: cobertura, xml, coverage",
                nameof(value))
        };
    }

    /// <summary>
    /// Tries to parse a string to a <see cref="CoverageFormat"/>.
    /// </summary>
    /// <param name="value">The string value (case-insensitive).</param>
    /// <param name="format">The parsed format, or default if parsing fails.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParseCoverageFormat(this string? value, out CoverageFormat format)
    {
        format = CoverageFormat.Cobertura;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            format = value.ParseCoverageFormat();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a <see cref="CoverageFormat"/> to its CLI string representation.
    /// </summary>
    public static string ToCliString(this CoverageFormat format) => format switch
    {
        CoverageFormat.Cobertura => "cobertura",
        CoverageFormat.Xml => "xml",
        CoverageFormat.Coverage => "coverage",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    /// <summary>
    /// Gets the file extension for the coverage format.
    /// </summary>
    public static string GetFileExtension(this CoverageFormat format) => format switch
    {
        CoverageFormat.Cobertura => "cobertura.xml",
        CoverageFormat.Xml => "xml",
        CoverageFormat.Coverage => "coverage",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };
}
