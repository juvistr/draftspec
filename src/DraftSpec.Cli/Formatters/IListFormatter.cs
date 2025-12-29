using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Formatters;

/// <summary>
/// Formats discovered specs for list command output.
/// </summary>
public interface IListFormatter
{
    /// <summary>
    /// Formats the discovered specs and any discovery errors.
    /// </summary>
    /// <param name="specs">The discovered specs to format.</param>
    /// <param name="errors">Any discovery errors encountered.</param>
    /// <returns>Formatted output string.</returns>
    string Format(IReadOnlyList<DiscoveredSpec> specs, IReadOnlyList<DiscoveryError> errors);
}
