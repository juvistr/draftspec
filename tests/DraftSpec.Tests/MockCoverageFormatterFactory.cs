using DraftSpec.Cli.Coverage;

namespace DraftSpec.Tests.Infrastructure.Mocks;

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
