namespace DraftSpec.Cli.Options;

/// <summary>
/// Command-specific options for the 'new' command.
/// </summary>
public class NewOptions
{
    /// <summary>
    /// Directory where the new spec file will be created.
    /// </summary>
    public string Path { get; set; } = ".";

    /// <summary>
    /// Name of the spec to create (without .spec.csx extension).
    /// </summary>
    public string? SpecName { get; set; }
}
