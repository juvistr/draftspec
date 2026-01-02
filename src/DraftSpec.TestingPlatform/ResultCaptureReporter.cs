using DraftSpec.Formatters;
using DraftSpec.Plugins;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Reporter that captures spec results for MTP integration.
/// Results are collected in memory for mapping to TestNodeUpdateMessage.
/// </summary>
internal sealed class ResultCaptureReporter : IReporter
{
    private readonly List<SpecResult> _results = [];
    private readonly Lock _lock = new();

    /// <summary>
    /// Reporter name for identification.
    /// </summary>
    public string Name => "MtpResultCapture";

    /// <summary>
    /// Gets the collected results (thread-safe copy).
    /// </summary>
    public IReadOnlyList<SpecResult> Results
    {
        get
        {
            lock (_lock)
            {
                return _results.ToList();
            }
        }
    }

    /// <summary>
    /// Called after each spec completes. Captures the result.
    /// </summary>
    public Task OnSpecCompletedAsync(SpecResult result)
    {
        lock (_lock)
        {
            _results.Add(result);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the entire run is complete. No-op for capture reporter.
    /// </summary>
    public Task OnRunCompletedAsync(SpecReport report)
    {
        // Results are already captured via OnSpecCompletedAsync
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears captured results for reuse.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _results.Clear();
        }
    }
}
