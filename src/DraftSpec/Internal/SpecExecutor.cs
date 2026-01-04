using DraftSpec.Formatters;

namespace DraftSpec.Internal;

/// <summary>
/// Executes specs and produces a report.
/// </summary>
internal static class SpecExecutor
{
    /// <summary>
    /// Execute specs and return the report.
    /// </summary>
    public static async Task<SpecReport> ExecuteAsync(
        SpecContext rootContext,
        SpecRunnerBuilder? builder = null,
        CancellationToken cancellationToken = default)
    {
        var runner = builder?.Build() ?? new SpecRunner();
        var results = await runner.RunAsync(rootContext, cancellationToken).ConfigureAwait(false);
        return SpecReportBuilder.Build(rootContext, results);
    }
}
