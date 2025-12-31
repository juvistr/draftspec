namespace DraftSpec.Cli.Formatters;

/// <summary>
/// A file that failed to parse.
/// </summary>
public sealed class ListErrorDto
{
    /// <summary>
    /// Relative path to the file that failed.
    /// </summary>
    public required string File { get; init; }

    /// <summary>
    /// Error message describing why parsing failed.
    /// </summary>
    public required string Message { get; init; }
}
