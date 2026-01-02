using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Formatters;

/// <summary>
/// Formats discovered specs as living documentation.
/// </summary>
public interface IDocsFormatter
{
    /// <summary>
    /// Formats the discovered specs as documentation.
    /// </summary>
    /// <param name="specs">The discovered specs to format.</param>
    /// <param name="metadata">Metadata about the documentation generation.</param>
    /// <returns>Formatted documentation string.</returns>
    string Format(IReadOnlyList<DiscoveredSpec> specs, DocsMetadata metadata);
}
