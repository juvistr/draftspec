namespace DraftSpec.Cli.Options;

/// <summary>
/// Command-specific options for the 'validate' command.
/// Used to validate spec files without executing them.
/// </summary>
public class ValidateOptions
{
    /// <summary>
    /// Path to spec files or directory.
    /// </summary>
    public string Path { get; set; } = ".";

    /// <summary>
    /// Use static parsing only (no script execution).
    /// This is the default for validate command.
    /// </summary>
    public bool Static { get; set; }

    /// <summary>
    /// Treat warnings as errors (exit code 2 instead of 0).
    /// </summary>
    public bool Strict { get; set; }

    /// <summary>
    /// Show only errors, suppress progress and warnings.
    /// </summary>
    public bool Quiet { get; set; }

    /// <summary>
    /// Specific files to validate (for pre-commit hooks).
    /// When set, only these files are validated instead of scanning directory.
    /// </summary>
    public List<string>? Files { get; set; }
}
