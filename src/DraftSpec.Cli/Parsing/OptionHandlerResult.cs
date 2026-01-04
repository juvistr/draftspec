namespace DraftSpec.Cli.Parsing;

/// <summary>
/// Result of processing a command-line option.
/// </summary>
/// <param name="ConsumedArgs">Number of arguments consumed (including the option itself).</param>
/// <param name="Error">Error message if parsing failed, null otherwise.</param>
public readonly record struct OptionHandlerResult(int ConsumedArgs, string? Error = null)
{
    /// <summary>
    /// Creates a successful result that consumed a single argument (the flag itself).
    /// </summary>
    public static OptionHandlerResult Flag() => new(1);

    /// <summary>
    /// Creates a successful result that consumed the option and one value argument.
    /// </summary>
    public static OptionHandlerResult Value() => new(2);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static OptionHandlerResult Failed(string error) => new(0, error);
}
