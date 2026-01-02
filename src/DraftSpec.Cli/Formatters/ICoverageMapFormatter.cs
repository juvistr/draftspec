using DraftSpec.Cli.CoverageMap;

namespace DraftSpec.Cli.Formatters;

/// <summary>
/// Formats coverage map results for output.
/// </summary>
public interface ICoverageMapFormatter
{
    /// <summary>
    /// Formats the coverage map result.
    /// </summary>
    /// <param name="result">The coverage map result to format.</param>
    /// <param name="gapsOnly">If true, only show uncovered methods.</param>
    /// <returns>Formatted output string.</returns>
    string Format(CoverageMapResult result, bool gapsOnly);
}
