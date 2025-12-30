using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.TestHelpers;

/// <summary>
/// Mock implementation of ISpecDiscoverer for testing.
/// </summary>
public class MockSpecDiscoverer : ISpecDiscoverer
{
    private SpecDiscoveryResult _result = new() { Specs = [], Errors = [] };
    private readonly Dictionary<string, IReadOnlyList<DiscoveredSpec>> _fileResults = new(StringComparer.OrdinalIgnoreCase);

    public List<string> DiscoverAsyncCalls { get; } = [];
    public List<string> DiscoverFileAsyncCalls { get; } = [];

    /// <summary>
    /// Configures the mock to return specific specs from DiscoverAsync.
    /// </summary>
    public MockSpecDiscoverer WithSpecs(params DiscoveredSpec[] specs)
    {
        _result = new SpecDiscoveryResult { Specs = specs.ToList(), Errors = _result.Errors };
        return this;
    }

    /// <summary>
    /// Configures the mock to return specific specs from DiscoverAsync.
    /// </summary>
    public MockSpecDiscoverer WithSpecs(IReadOnlyList<DiscoveredSpec> specs)
    {
        _result = new SpecDiscoveryResult { Specs = specs, Errors = _result.Errors };
        return this;
    }

    /// <summary>
    /// Configures the mock to return specific errors from DiscoverAsync.
    /// </summary>
    public MockSpecDiscoverer WithErrors(params DiscoveryError[] errors)
    {
        _result = new SpecDiscoveryResult { Specs = _result.Specs, Errors = errors.ToList() };
        return this;
    }

    /// <summary>
    /// Configures the mock to return specific specs for a file path.
    /// </summary>
    public MockSpecDiscoverer WithFileResult(string path, IReadOnlyList<DiscoveredSpec> specs)
    {
        _fileResults[path] = specs;
        return this;
    }

    public Task<SpecDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        DiscoverAsyncCalls.Add("DiscoverAsync");
        return Task.FromResult(_result);
    }

    public Task<IReadOnlyList<DiscoveredSpec>> DiscoverFileAsync(
        string csxFilePath,
        CancellationToken cancellationToken = default)
    {
        DiscoverFileAsyncCalls.Add(csxFilePath);

        if (_fileResults.TryGetValue(csxFilePath, out var specs))
        {
            return Task.FromResult(specs);
        }

        return Task.FromResult<IReadOnlyList<DiscoveredSpec>>(_result.Specs);
    }
}
