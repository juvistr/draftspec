using DraftSpec.Cli.Coverage;

namespace DraftSpec.Tests.TestHelpers;

/// <summary>
/// Mock coverage formatter factory for unit testing.
/// </summary>
public class MockCoverageFormatterFactory : ICoverageFormatterFactory
{
    private readonly Dictionary<string, ICoverageFormatter> _formatters = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Register a formatter for a given format name.
    /// </summary>
    public MockCoverageFormatterFactory AddFormatter(string format, ICoverageFormatter formatter)
    {
        _formatters[format] = formatter;
        return this;
    }

    public ICoverageFormatter? GetFormatter(string format)
    {
        return _formatters.TryGetValue(format, out var formatter) ? formatter : null;
    }
}

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
