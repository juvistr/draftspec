using DraftSpec.Cli.Coverage;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock coverage formatter for unit testing.
/// </summary>
public class MockCoverageFormatter : ICoverageFormatter
{
    /// <summary>
    /// The file extension this formatter uses.
    /// </summary>
    public string FileExtension { get; set; } = ".txt";

    /// <summary>
    /// The name of this format.
    /// </summary>
    public string FormatName { get; set; } = "mock";

    /// <summary>
    /// The content returned by Format().
    /// </summary>
    public string FormattedContent { get; set; } = "";

    /// <summary>
    /// Whether Format() was called.
    /// </summary>
    public bool FormatCalled { get; private set; }

    public string Format(CoverageReport report)
    {
        FormatCalled = true;
        return FormattedContent;
    }
}
