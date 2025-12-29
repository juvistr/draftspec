namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Implementation that creates coverage formatters.
/// </summary>
public class CoverageFormatterFactory : ICoverageFormatterFactory
{
    /// <inheritdoc />
    public ICoverageFormatter? GetFormatter(string format) => format.ToLowerInvariant() switch
    {
        "html" => new CoverageHtmlFormatter(),
        "json" => new CoverageJsonFormatter(),
        _ => null
    };
}
