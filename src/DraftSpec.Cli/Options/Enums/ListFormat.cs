using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Options.Enums;

/// <summary>
/// Output format for the list command.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ListFormat>))]
public enum ListFormat
{
    /// <summary>
    /// Tree view showing nested context hierarchy.
    /// </summary>
    Tree,

    /// <summary>
    /// Flat list of all specs with full context paths.
    /// </summary>
    Flat,

    /// <summary>
    /// JSON format for programmatic consumption.
    /// </summary>
    Json
}
