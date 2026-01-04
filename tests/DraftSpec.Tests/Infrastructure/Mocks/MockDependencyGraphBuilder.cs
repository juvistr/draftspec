using DraftSpec.Cli.DependencyGraph;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of <see cref="IDependencyGraphBuilder"/> for testing.
/// </summary>
public class MockDependencyGraphBuilder : IDependencyGraphBuilder
{
    private DependencyGraph? _graphToReturn;
    private readonly List<(string SpecDirectory, string? SourceDirectory)> _buildAsyncCalls = [];

    /// <summary>
    /// Gets the calls made to <see cref="BuildAsync"/>.
    /// </summary>
    public IReadOnlyList<(string SpecDirectory, string? SourceDirectory)> BuildAsyncCalls => _buildAsyncCalls;

    /// <summary>
    /// Configures the mock to return the specified graph.
    /// </summary>
    public MockDependencyGraphBuilder WithGraph(DependencyGraph graph)
    {
        _graphToReturn = graph;
        return this;
    }

    /// <inheritdoc />
    public Task<DependencyGraph> BuildAsync(
        string specDirectory,
        string? sourceDirectory = null,
        CancellationToken cancellationToken = default)
    {
        _buildAsyncCalls.Add((specDirectory, sourceDirectory));

        if (_graphToReturn != null)
            return Task.FromResult(_graphToReturn);

        // Return an empty graph by default
        var pathComparer = new MockPathComparer();
        return Task.FromResult(new DependencyGraph(pathComparer));
    }
}
