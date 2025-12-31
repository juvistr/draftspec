using DraftSpec.Formatters;

namespace DraftSpec.Plugins;

/// <summary>
/// Interface for reporters that receive spec execution events.
/// Reporters perform side effects like writing files, sending notifications, etc.
/// </summary>
public interface IReporter
{
    /// <summary>
    /// The name of the reporter (for identification).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Called when the spec run is about to start.
    /// </summary>
    /// <param name="context">Information about the upcoming run</param>
    Task OnRunStartingAsync(RunStartingContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after each spec completes (enables streaming output).
    /// </summary>
    /// <param name="result">The result of the spec that just completed</param>
    Task OnSpecCompletedAsync(SpecResult result)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after a batch of specs complete (parallel execution optimization).
    /// Default implementation calls OnSpecCompletedAsync for each result.
    /// Reporters may override for more efficient batch processing.
    /// </summary>
    /// <param name="results">The results of specs that completed</param>
    async Task OnSpecsBatchCompletedAsync(IReadOnlyList<SpecResult> results)
    {
        foreach (var result in results) await OnSpecCompletedAsync(result);
    }

    /// <summary>
    /// Called when the entire spec run is complete.
    /// This is the primary entry point for reporters that process full results.
    /// </summary>
    /// <param name="report">The complete spec report</param>
    Task OnRunCompletedAsync(SpecReport report);
}
