using DraftSpec.Cli.CoverageMap;
using DraftSpec.Cli.Services;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of <see cref="ICoverageMapService"/> for testing.
/// </summary>
public class MockCoverageMapService : ICoverageMapService
{
    private CoverageMapResult _result = new();
    private Exception? _exceptionToThrow;

    public List<ComputeCoverageCall> ComputeCoverageAsyncCalls { get; } = [];

    public MockCoverageMapService WithResult(CoverageMapResult result)
    {
        _result = result;
        return this;
    }

    public MockCoverageMapService WithMethods(params MethodCoverage[] methods)
    {
        _result = new CoverageMapResult
        {
            AllMethods = methods.ToList(),
            Summary = new CoverageSummary
            {
                TotalMethods = methods.Length,
                HighConfidence = methods.Count(m => m.Confidence == CoverageConfidence.High),
                MediumConfidence = methods.Count(m => m.Confidence == CoverageConfidence.Medium),
                LowConfidence = methods.Count(m => m.Confidence == CoverageConfidence.Low),
                Uncovered = methods.Count(m => m.Confidence == CoverageConfidence.None)
            }
        };
        return this;
    }

    public MockCoverageMapService Throws(Exception exception)
    {
        _exceptionToThrow = exception;
        return this;
    }

    public Task<CoverageMapResult> ComputeCoverageAsync(
        IReadOnlyList<string> sourceFiles,
        IReadOnlyList<string> specFiles,
        string projectPath,
        string? sourcePath = null,
        string? specPath = null,
        string? namespaceFilter = null,
        CancellationToken ct = default)
    {
        ComputeCoverageAsyncCalls.Add(new ComputeCoverageCall(
            sourceFiles, specFiles, projectPath, sourcePath, specPath, namespaceFilter));

        if (_exceptionToThrow is not null)
        {
            throw _exceptionToThrow;
        }

        return Task.FromResult(_result);
    }

    public record ComputeCoverageCall(
        IReadOnlyList<string> SourceFiles,
        IReadOnlyList<string> SpecFiles,
        string ProjectPath,
        string? SourcePath,
        string? SpecPath,
        string? NamespaceFilter);
}
