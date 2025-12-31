using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Options.Enums;

/// <summary>
/// Extension methods for <see cref="PartitionStrategy"/>.
/// </summary>
public static class PartitionStrategyExtensions
{
    /// <summary>
    /// Converts a string to a <see cref="PartitionStrategy"/>.
    /// </summary>
    /// <param name="value">The string value (case-insensitive).</param>
    /// <returns>The parsed strategy.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid strategy.</exception>
    public static PartitionStrategy ParsePartitionStrategy(this string value)
    {
        return value.ToLowerInvariant() switch
        {
            "file" => PartitionStrategy.File,
            "spec-count" => PartitionStrategy.SpecCount,
            _ => throw new ArgumentException(
                $"Unknown partition strategy: '{value}'. Valid options: file, spec-count",
                nameof(value))
        };
    }

    /// <summary>
    /// Tries to parse a string to a <see cref="PartitionStrategy"/>.
    /// </summary>
    /// <param name="value">The string value (case-insensitive).</param>
    /// <param name="strategy">The parsed strategy, or default if parsing fails.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParsePartitionStrategy(this string? value, out PartitionStrategy strategy)
    {
        strategy = PartitionStrategy.File;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            strategy = value.ParsePartitionStrategy();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a <see cref="PartitionStrategy"/> to its CLI string representation.
    /// </summary>
    public static string ToCliString(this PartitionStrategy strategy) => strategy switch
    {
        PartitionStrategy.File => "file",
        PartitionStrategy.SpecCount => "spec-count",
        _ => throw new ArgumentOutOfRangeException(nameof(strategy))
    };
}
