using DraftSpec.Cli.Watch;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of ISpecChangeTracker for testing.
/// </summary>
public class MockSpecChangeTracker : ISpecChangeTracker
{
    private readonly Dictionary<string, StaticParseResult> _states = new();
    private readonly Dictionary<string, DateTime> _dependencies = new();
    private SpecChangeSet? _nextChangeSet;

    public List<(string FilePath, StaticParseResult ParseResult)> RecordStateCalls { get; } = [];
    public List<(string FilePath, StaticParseResult NewResult, bool DependencyChanged)> GetChangesCalls { get; } = [];

    /// <summary>
    /// Configure the next SpecChangeSet to return from GetChanges.
    /// </summary>
    public MockSpecChangeTracker WithNextChangeSet(SpecChangeSet changeSet)
    {
        _nextChangeSet = changeSet;
        return this;
    }

    public void RecordState(string filePath, StaticParseResult parseResult)
    {
        RecordStateCalls.Add((filePath, parseResult));
        _states[filePath] = parseResult;
    }

    public SpecChangeSet GetChanges(string filePath, StaticParseResult newResult, bool dependencyChanged)
    {
        GetChangesCalls.Add((filePath, newResult, dependencyChanged));

        if (_nextChangeSet != null)
            return _nextChangeSet;

        // Default: no changes
        return new SpecChangeSet(filePath, [], false, dependencyChanged);
    }

    public bool HasState(string filePath) => _states.ContainsKey(filePath);

    public void Clear()
    {
        _states.Clear();
        _dependencies.Clear();
    }

    public void RecordDependency(string dependencyPath, DateTime lastModified)
    {
        _dependencies[dependencyPath] = lastModified;
    }

    public bool HasDependencyChanged(string dependencyPath, DateTime currentModified)
    {
        if (!_dependencies.TryGetValue(dependencyPath, out var recorded))
            return true;
        return currentModified != recorded;
    }
}
