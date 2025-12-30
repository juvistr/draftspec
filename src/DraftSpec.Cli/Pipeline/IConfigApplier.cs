namespace DraftSpec.Cli.Pipeline;

/// <summary>
/// Applies project configuration defaults to CLI options.
/// </summary>
public interface IConfigApplier
{
    /// <summary>
    /// Load configuration from the path specified in options and apply defaults.
    /// Values explicitly set on the command line take precedence.
    /// </summary>
    /// <param name="options">The CLI options to apply configuration to.</param>
    void ApplyConfig(CliOptions options);
}
