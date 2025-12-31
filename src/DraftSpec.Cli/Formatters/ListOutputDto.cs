namespace DraftSpec.Cli.Formatters;

/// <summary>
/// Root DTO for the JSON output of `draftspec list --format json`.
/// </summary>
/// <remarks>
/// This type is used for JSON schema generation. Changes to this type
/// will affect the published schema at docs/schemas/list-output.schema.json.
/// </remarks>
public sealed class ListOutputDto
{
    /// <summary>
    /// List of discovered spec definitions.
    /// </summary>
    public required List<SpecInfoDto> Specs { get; init; }

    /// <summary>
    /// Summary statistics for the discovery run.
    /// </summary>
    public required ListSummaryDto Summary { get; init; }

    /// <summary>
    /// List of files that failed to parse.
    /// </summary>
    public required List<ListErrorDto> Errors { get; init; }
}
