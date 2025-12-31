using DraftSpec.Cli;
using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock spec runner for unit testing.
/// Supports configurable results, delays, and call tracking.
/// </summary>
public class MockRunner : IInProcessSpecRunner
{
    private readonly bool _success;
    private readonly int _passedCount;
    private readonly int _failedCount;
    private readonly int _pendingCount;
    private readonly int _skippedCount;
    private readonly int _delayMs;
    private readonly bool _throwArgumentException;

    /// <summary>
    /// Creates a MockRunner with configurable behavior.
    /// </summary>
    public MockRunner(
        bool success = true,
        int passedCount = 0,
        int failedCount = 0,
        int pendingCount = 0,
        int skippedCount = 0,
        int delayMs = 0,
        bool throwArgumentException = false)
    {
        _success = success;
        _passedCount = passedCount;
        _failedCount = failedCount;
        _pendingCount = pendingCount;
        _skippedCount = skippedCount;
        _delayMs = delayMs;
        _throwArgumentException = throwArgumentException;
    }

    // Call tracking
    public bool RunAllCalled { get; private set; }
    public int RunAllCallCount { get; private set; }
    public IReadOnlyList<string>? LastSpecFiles { get; private set; }
    public bool LastParallelFlag { get; private set; }

    // Event registration tracking
    public bool OnBuildStartedRegistered { get; private set; }
    public bool OnBuildCompletedRegistered { get; private set; }
    public bool OnBuildSkippedRegistered { get; private set; }

    // Event for synchronization in tests
    public event Action? OnRunAllCompleted;

#pragma warning disable CS0067 // Backing fields stored but not invoked (mock only tracks registration)
    private event Action<string>? _onBuildStarted;
    private event Action<BuildResult>? _onBuildCompleted;
    private event Action<string>? _onBuildSkipped;
#pragma warning restore CS0067

    public event Action<string>? OnBuildStarted
    {
        add { _onBuildStarted += value; OnBuildStartedRegistered = true; }
        remove { _onBuildStarted -= value; }
    }

    public event Action<BuildResult>? OnBuildCompleted
    {
        add { _onBuildCompleted += value; OnBuildCompletedRegistered = true; }
        remove { _onBuildCompleted -= value; }
    }

    public event Action<string>? OnBuildSkipped
    {
        add { _onBuildSkipped += value; OnBuildSkippedRegistered = true; }
        remove { _onBuildSkipped -= value; }
    }

    public Task<InProcessRunResult> RunFileAsync(string specFile, CancellationToken ct = default)
    {
        return Task.FromResult(CreateResult(specFile));
    }

    public async Task<InProcessRunSummary> RunAllAsync(IReadOnlyList<string> specFiles, bool parallel = false, CancellationToken ct = default)
    {
        if (_throwArgumentException)
            throw new ArgumentException("Test exception from MockRunner");

        RunAllCalled = true;
        RunAllCallCount++;
        LastSpecFiles = specFiles;
        LastParallelFlag = parallel;

        if (_delayMs > 0)
            await Task.Delay(_delayMs, ct);

        var results = specFiles.Select(CreateResult).ToList();

        OnRunAllCompleted?.Invoke();

        return new InProcessRunSummary(results, TimeSpan.Zero);
    }

    private InProcessRunResult CreateResult(string specFile)
    {
        var total = _passedCount + _failedCount + _pendingCount + _skippedCount;
        return new InProcessRunResult(
            specFile,
            new SpecReport
            {
                Summary = new SpecSummary
                {
                    Total = total > 0 ? total : 1,
                    Passed = _passedCount,
                    Failed = _failedCount,
                    Pending = _pendingCount,
                    Skipped = _skippedCount
                }
            },
            TimeSpan.Zero,
            _success ? null : new Exception("Test failed"));
    }

    public void ClearBuildCache() { }
}
