using DraftSpec.Formatters;

namespace DraftSpec.Plugins;

/// <summary>
/// Context provided when a spec run is starting.
/// </summary>
public class RunStartingContext
{
    /// <summary>
    /// The total number of specs that will be executed.
    /// </summary>
    public int TotalSpecs { get; }

    /// <summary>
    /// The time the run started.
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    /// Create a new RunStartingContext.
    /// </summary>
    public RunStartingContext(int totalSpecs, DateTime startTime)
    {
        TotalSpecs = totalSpecs;
        StartTime = startTime;
    }
}
