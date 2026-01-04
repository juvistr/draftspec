using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of IFormatter for testing.
/// </summary>
public class MockFormatter : IFormatter
{
    private string _output = "formatted output";
    private Exception? _exception;

    public List<SpecReport> FormatCalls { get; } = [];

    public string FileExtension { get; set; } = ".txt";

    /// <summary>
    /// Configure the output returned by Format.
    /// </summary>
    public MockFormatter WithOutput(string output)
    {
        _output = output;
        return this;
    }

    /// <summary>
    /// Configure the formatter to throw an exception.
    /// </summary>
    public MockFormatter Throws(Exception exception)
    {
        _exception = exception;
        return this;
    }

    public string Format(SpecReport report)
    {
        FormatCalls.Add(report);

        if (_exception != null)
            throw _exception;

        return _output;
    }
}
