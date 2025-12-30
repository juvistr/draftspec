using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Watch;

/// <summary>
/// Tracks spec state across file changes for incremental watch mode.
/// </summary>
public sealed class SpecChangeTracker : ISpecChangeTracker
{
    private readonly Dictionary<string, StaticParseResult> _fileStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _dependencies = new(StringComparer.OrdinalIgnoreCase);
    private readonly SpecDiffer _differ = new();

    /// <inheritdoc/>
    public void RecordState(string filePath, StaticParseResult parseResult)
    {
        _fileStates[filePath] = parseResult;
    }

    /// <inheritdoc/>
    public SpecChangeSet GetChanges(string filePath, StaticParseResult newResult, bool dependencyChanged)
    {
        _fileStates.TryGetValue(filePath, out var oldResult);
        return _differ.Diff(filePath, oldResult, newResult, dependencyChanged);
    }

    /// <inheritdoc/>
    public bool HasState(string filePath)
    {
        return _fileStates.ContainsKey(filePath);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _fileStates.Clear();
        _dependencies.Clear();
    }

    /// <inheritdoc/>
    public void RecordDependency(string dependencyPath, DateTime lastModified)
    {
        _dependencies[dependencyPath] = lastModified;
    }

    /// <inheritdoc/>
    public bool HasDependencyChanged(string dependencyPath, DateTime currentModified)
    {
        if (!_dependencies.TryGetValue(dependencyPath, out var recorded))
            return true; // First time seeing this dependency

        return currentModified > recorded;
    }
}
