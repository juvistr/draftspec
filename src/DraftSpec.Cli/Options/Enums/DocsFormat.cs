using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Options.Enums;

/// <summary>
/// Output format for the docs command.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DocsFormat>))]
public enum DocsFormat
{
    /// <summary>
    /// Markdown format for stakeholder documentation.
    /// </summary>
    Markdown,

    /// <summary>
    /// HTML format with collapsible sections and styling.
    /// </summary>
    Html
}
