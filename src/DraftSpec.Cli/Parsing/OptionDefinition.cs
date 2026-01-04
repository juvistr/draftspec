namespace DraftSpec.Cli.Parsing;

/// <summary>
/// Defines a command-line option with its names and handler.
/// </summary>
/// <param name="Names">Option names that trigger this handler (e.g., "--format", "-f").</param>
/// <param name="Handler">Handler function that processes the option and returns the result.</param>
public sealed record OptionDefinition(
    string[] Names,
    Func<string[], int, CliOptions, OptionHandlerResult> Handler);
