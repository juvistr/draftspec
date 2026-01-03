using DraftSpec.Cli.Services;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock static spec parser that returns configured results.
/// </summary>
public class MockStaticSpecParser : IStaticSpecParser
{
    private readonly Dictionary<string, StaticParseResult> _results = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Exception> _exceptions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tracks calls to ParseFileAsync.
    /// </summary>
    public List<string> ParseFileCalls { get; } = [];

    /// <summary>
    /// Configure a result to return for a specific file.
    /// </summary>
    public MockStaticSpecParser WithResult(string filePath, StaticParseResult result)
    {
        _results[filePath] = result;
        return this;
    }

    /// <summary>
    /// Configure an exception to throw for a specific file.
    /// </summary>
    public MockStaticSpecParser WithException(string filePath, Exception exception)
    {
        _exceptions[filePath] = exception;
        return this;
    }

    /// <summary>
    /// Configure a simple result with specs.
    /// </summary>
    public MockStaticSpecParser WithSpecs(string filePath, params StaticSpec[] specs)
    {
        _results[filePath] = new StaticParseResult { Specs = specs };
        return this;
    }

    /// <summary>
    /// Configure a result with warnings.
    /// </summary>
    public MockStaticSpecParser WithWarnings(string filePath, params string[] warnings)
    {
        _results[filePath] = new StaticParseResult { Warnings = warnings };
        return this;
    }

    /// <inheritdoc />
    public Task<StaticParseResult> ParseFileAsync(string csxFilePath, CancellationToken cancellationToken = default)
    {
        ParseFileCalls.Add(csxFilePath);

        if (_exceptions.TryGetValue(csxFilePath, out var ex))
        {
            throw ex;
        }

        if (_results.TryGetValue(csxFilePath, out var result))
        {
            return Task.FromResult(result);
        }

        // Return empty result for unconfigured files
        return Task.FromResult(new StaticParseResult());
    }
}
