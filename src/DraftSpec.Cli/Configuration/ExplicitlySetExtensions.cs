namespace DraftSpec.Cli.Configuration;

/// <summary>
/// Extension methods for applying configuration values only when not explicitly set via CLI.
/// Reduces repetitive patterns in CliOptions.ApplyDefaults.
/// </summary>
public static class ExplicitlySetExtensions
{
    /// <summary>
    /// Applies a nullable value type if the property was not explicitly set.
    /// </summary>
    public static void ApplyIfNotSet<T>(
        this HashSet<string> explicitlySet,
        string propertyName,
        Action<T> setter,
        T? value) where T : struct
    {
        if (!explicitlySet.Contains(propertyName) && value.HasValue)
            setter(value.Value);
    }

    /// <summary>
    /// Applies a string value if the property was not explicitly set and the value is not null/empty.
    /// </summary>
    public static void ApplyIfNotEmpty(
        this HashSet<string> explicitlySet,
        string propertyName,
        Action<string> setter,
        string? value)
    {
        if (!explicitlySet.Contains(propertyName) && !string.IsNullOrEmpty(value))
            setter(value);
    }

    /// <summary>
    /// Applies a joined list of values if the property was not explicitly set and the list is not empty.
    /// </summary>
    public static void ApplyIfNotEmpty(
        this HashSet<string> explicitlySet,
        string propertyName,
        Action<string> setter,
        IReadOnlyList<string>? values,
        string separator = ",")
    {
        if (!explicitlySet.Contains(propertyName) && values is { Count: > 0 })
            setter(string.Join(separator, values));
    }

    /// <summary>
    /// Delegate for TryParse-style methods.
    /// </summary>
    public delegate bool TryParseFunc<T>(string value, out T result);

    /// <summary>
    /// Applies a parsed value if the property was not explicitly set and parsing succeeds.
    /// </summary>
    public static void ApplyIfValid<T>(
        this HashSet<string> explicitlySet,
        string propertyName,
        Action<T> setter,
        string? value,
        TryParseFunc<T> tryParse)
    {
        if (!explicitlySet.Contains(propertyName) && !string.IsNullOrEmpty(value) && tryParse(value, out var parsed))
            setter(parsed);
    }

    /// <summary>
    /// Applies true if the property was not explicitly set and the condition is true.
    /// </summary>
    public static void ApplyIfTrue(
        this HashSet<string> explicitlySet,
        string propertyName,
        Action<bool> setter,
        bool? condition)
    {
        if (!explicitlySet.Contains(propertyName) && condition == true)
            setter(true);
    }
}
