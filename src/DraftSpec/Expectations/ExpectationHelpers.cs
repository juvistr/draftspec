namespace DraftSpec.Expectations;

/// <summary>
/// Shared helper methods for expectation formatting.
/// </summary>
internal static class ExpectationHelpers
{
    /// <summary>
    /// Format a value for display in assertion messages.
    /// </summary>
    public static string Format(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            _ => value.ToString() ?? "null"
        };
    }
}