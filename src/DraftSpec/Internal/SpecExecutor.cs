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
    public static SpecReport Execute(SpecContext rootContext, SpecRunnerBuilder? builder = null)
    {
        var runner = builder?.Build() ?? new SpecRunner();
        var results = runner.Run(rootContext);
        return SpecReportBuilder.Build(rootContext, results);
    }
}
