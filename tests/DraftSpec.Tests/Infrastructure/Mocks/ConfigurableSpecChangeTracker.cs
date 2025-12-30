using DraftSpec.Cli.Watch;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Configurable spec change tracker for testing incremental watch mode.
/// Allows tests to control what changes are reported.
/// </summary>
public class ConfigurableSpecChangeTracker : ISpecChangeTracker
{
    private readonly bool _hasChanges;
    private readonly bool _hasDynamicSpecs;
    private readonly IReadOnlyList<SpecChange> _changes;

    public bool RecordStateCalled { get; private set; }
    public string? LastRecordedFilePath { get; private set; }
    public int RecordStateCallCount { get; private set; }

    public ConfigurableSpecChangeTracker(
        bool hasChanges = false,
        bool hasDynamicSpecs = false,
        IReadOnlyList<SpecChange>? changes = null)
    {
        _hasChanges = hasChanges;
        _hasDynamicSpecs = hasDynamicSpecs;
        _changes = changes ?? [];
    }

    public void RecordState(string filePath, StaticParseResult parseResult)
    {
        RecordStateCalled = true;
        RecordStateCallCount++;
        LastRecordedFilePath = filePath;
    }

    public SpecChangeSet GetChanges(string filePath, StaticParseResult newResult, bool dependencyChanged)
    {
        if (!_hasChanges)
            return new SpecChangeSet(filePath, [], HasDynamicSpecs: false, DependencyChanged: false);

        return new SpecChangeSet(filePath, _changes, _hasDynamicSpecs, dependencyChanged);
    }

    public bool HasState(string filePath) => true;
    public void Clear() { }
    public void RecordDependency(string dependencyPath, DateTime lastModified) { }
    public bool HasDependencyChanged(string dependencyPath, DateTime currentModified) => false;
}
