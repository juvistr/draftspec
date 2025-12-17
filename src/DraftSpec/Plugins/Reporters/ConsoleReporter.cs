using DraftSpec.Formatters;

namespace DraftSpec.Plugins.Reporters;

/// <summary>
/// Reporter that writes the spec report to the console using an IConsoleFormatter.
/// </summary>
public class ConsoleReporter : IReporter
{
    private readonly IConsoleFormatter _formatter;

    /// <summary>
    /// Create a ConsoleReporter with a custom formatter.
    /// </summary>
    /// <param name="formatter">The console formatter to use</param>
    public ConsoleReporter(IConsoleFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        _formatter = formatter;
    }

    /// <summary>
    /// Gets the reporter name identifier.
    /// </summary>
    public string Name => "console";

    /// <summary>
    /// Write the spec report to the console when the run completes.
    /// </summary>
    public Task OnRunCompletedAsync(SpecReport report)
    {
        _formatter.Format(report, Console.Out);
        return Task.CompletedTask;
    }
}
