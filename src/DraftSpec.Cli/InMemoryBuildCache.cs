namespace DraftSpec.Cli;

/// <summary>
/// In-memory implementation of build cache.
/// </summary>
public class InMemoryBuildCache : IBuildCache
{
    private readonly Dictionary<string, DateTime> _lastBuildTime = new();
    private readonly Dictionary<string, DateTime> _lastSourceModified = new();

    public bool NeedsRebuild(string directory, DateTime latestSourceModification)
    {
        if (!_lastBuildTime.TryGetValue(directory, out _))
            return true;

        if (!_lastSourceModified.TryGetValue(directory, out var lastModified))
            return true;

        return latestSourceModification > lastModified;
    }

    public void UpdateCache(string directory, DateTime buildTime, DateTime sourceModification)
    {
        _lastBuildTime[directory] = buildTime;
        _lastSourceModified[directory] = sourceModification;
    }

    public void Clear()
    {
        _lastBuildTime.Clear();
        _lastSourceModified.Clear();
    }
}
