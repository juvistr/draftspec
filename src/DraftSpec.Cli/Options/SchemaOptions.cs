namespace DraftSpec.Cli.Options;

/// <summary>
/// Command-specific options for the 'schema' command.
/// </summary>
public class SchemaOptions
{
    /// <summary>
    /// Optional file path to write the JSON schema to.
    /// If null, writes to stdout.
    /// </summary>
    public string? OutputFile { get; set; }
}
