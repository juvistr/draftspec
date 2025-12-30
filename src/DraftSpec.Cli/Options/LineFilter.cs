namespace DraftSpec.Cli.Options;

/// <summary>
/// Represents a line number filter for running specs at specific lines.
/// </summary>
/// <param name="File">The spec file path.</param>
/// <param name="Lines">The line numbers to run.</param>
public record LineFilter(string File, int[] Lines);
