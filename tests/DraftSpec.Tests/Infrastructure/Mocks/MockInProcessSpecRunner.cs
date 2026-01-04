using DraftSpec.Cli;
using DraftSpec.Formatters;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of IInProcessSpecRunner for testing.
/// </summary>
public class MockInProcessSpecRunner : IInProcessSpecRunner
{
    private InProcessRunSummary? _summary;
    private InProcessRunResult? _fileResult;
    private Exception? _exception;

    public List<(IReadOnlyList<string> Files, bool Parallel)> RunAllAsyncCalls { get; } = [];
    public List<string> RunFileAsyncCalls { get; } = [];

    public event Action<string>? OnBuildStarted;
    public event Action<BuildResult>? OnBuildCompleted;
    public event Action<string>? OnBuildSkipped;

    /// <summary>
    /// Configure the summary result returned by RunAllAsync.
    /// </summary>
    public MockInProcessSpecRunner WithSummary(InProcessRunSummary summary)
    {
        _summary = summary;
        return this;
    }

    /// <summary>
    /// Configure the result returned by RunFileAsync.
    /// </summary>
    public MockInProcessSpecRunner WithFileResult(InProcessRunResult result)
    {
        _fileResult = result;
        return this;
    }

    /// <summary>
    /// Configure the runner to throw an exception.
    /// </summary>
    public MockInProcessSpecRunner Throws(Exception exception)
    {
        _exception = exception;
        return this;
    }

    public Task<InProcessRunResult> RunFileAsync(string specFile, CancellationToken ct = default)
    {
        RunFileAsyncCalls.Add(specFile);

        if (_exception != null)
            throw _exception;

        return Task.FromResult(_fileResult ?? new InProcessRunResult(
            specFile,
            new SpecReport(),
            TimeSpan.Zero));
    }

    public Task<InProcessRunSummary> RunAllAsync(
        IReadOnlyList<string> specFiles,
        bool parallel = false,
        CancellationToken ct = default)
    {
        RunAllAsyncCalls.Add((specFiles, parallel));

        if (_exception != null)
            throw _exception;

        return Task.FromResult(_summary ?? new InProcessRunSummary([], TimeSpan.Zero));
    }

    public void ClearBuildCache()
    {
    }

    // Helper to fire events for testing
    public void RaiseBuildStarted(string path) => OnBuildStarted?.Invoke(path);
    public void RaiseBuildCompleted(BuildResult result) => OnBuildCompleted?.Invoke(result);
    public void RaiseBuildSkipped(string path) => OnBuildSkipped?.Invoke(path);
}
