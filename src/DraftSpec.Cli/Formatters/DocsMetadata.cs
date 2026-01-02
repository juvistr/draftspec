namespace DraftSpec.Cli.Formatters;

/// <summary>
/// Metadata for documentation generation.
/// </summary>
/// <param name="GeneratedAt">When the documentation was generated.</param>
/// <param name="Source">Optional source path for the specs.</param>
/// <param name="Results">Optional test results mapping spec ID to status.</param>
public record DocsMetadata(
    DateTime GeneratedAt,
    string? Source,
    IReadOnlyDictionary<string, string>? Results);
