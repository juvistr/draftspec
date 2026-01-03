namespace DraftSpec.Cli.Pipeline;

/// <summary>
/// Well-known keys for <see cref="CommandContext.Items"/> dictionary.
/// Use these constants for phase-to-phase communication to ensure consistency.
/// </summary>
/// <remarks>
/// Each phase documents which keys it requires and produces. See individual
/// phase documentation for the expected types and semantics of each key.
/// </remarks>
public static class ContextKeys
{
    /// <summary>
    /// Resolved absolute path to the project/spec directory.
    /// Type: <see cref="string"/>
    /// </summary>
    public const string ProjectPath = nameof(ProjectPath);

    /// <summary>
    /// List of discovered spec file paths.
    /// Type: <c>IReadOnlyList&lt;string&gt;</c>
    /// </summary>
    public const string SpecFiles = nameof(SpecFiles);

    /// <summary>
    /// Parsed spec definitions from spec files.
    /// Type: <c>IReadOnlyList&lt;ParsedSpec&gt;</c>
    /// </summary>
    public const string ParsedSpecs = nameof(ParsedSpecs);

    /// <summary>
    /// Whether quarantine mode is enabled.
    /// Type: <see cref="bool"/>
    /// </summary>
    public const string Quarantine = nameof(Quarantine);

    /// <summary>
    /// Active filter options for spec selection.
    /// Type: <c>FilterOptions</c>
    /// </summary>
    public const string Filter = nameof(Filter);

    /// <summary>
    /// Results from spec execution.
    /// Type: <c>IReadOnlyList&lt;SpecResult&gt;</c>
    /// </summary>
    public const string RunResults = nameof(RunResults);
}
