using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Watch;

/// <summary>
/// Compares two StaticParseResults to identify spec changes.
/// </summary>
public sealed class SpecDiffer
{
    /// <summary>
    /// Compares old and new parse results to find changes.
    /// </summary>
    /// <param name="filePath">The path to the spec file.</param>
    /// <param name="oldResult">The previous parse result (null for first run).</param>
    /// <param name="newResult">The current parse result.</param>
    /// <param name="dependencyChanged">Whether a dependency (like spec_helper.csx) changed.</param>
    /// <returns>A SpecChangeSet containing detected changes.</returns>
    public SpecChangeSet Diff(
        string filePath,
        StaticParseResult? oldResult,
        StaticParseResult newResult,
        bool dependencyChanged)
    {
        // If either has dynamic specs, require full run
        if (!newResult.IsComplete || (oldResult != null && !oldResult.IsComplete))
        {
            return new SpecChangeSet(filePath, [], HasDynamicSpecs: true, dependencyChanged);
        }

        if (dependencyChanged)
        {
            return new SpecChangeSet(filePath, [], HasDynamicSpecs: false, DependencyChanged: true);
        }

        var oldSpecs = oldResult?.Specs ?? [];
        var newSpecs = newResult.Specs;

        var changes = new List<SpecChange>();

        // Build lookup by full path (ContextPath + Description)
        var oldByPath = new Dictionary<string, StaticSpec>();
        foreach (var spec in oldSpecs)
        {
            var path = GetFullPath(spec);
            // Handle duplicate paths gracefully (take first occurrence)
            oldByPath.TryAdd(path, spec);
        }

        var newByPath = new Dictionary<string, StaticSpec>();
        foreach (var spec in newSpecs)
        {
            var path = GetFullPath(spec);
            newByPath.TryAdd(path, spec);
        }

        // Find added and modified specs
        foreach (var spec in newSpecs)
        {
            var path = GetFullPath(spec);
            if (!oldByPath.TryGetValue(path, out var oldSpec))
            {
                changes.Add(new SpecChange(
                    spec.Description,
                    spec.ContextPath,
                    SpecChangeType.Added,
                    NewLineNumber: spec.LineNumber));
            }
            else if (HasChanged(oldSpec, spec))
            {
                changes.Add(new SpecChange(
                    spec.Description,
                    spec.ContextPath,
                    SpecChangeType.Modified,
                    OldLineNumber: oldSpec.LineNumber,
                    NewLineNumber: spec.LineNumber));
            }
        }

        // Find deleted specs
        foreach (var spec in oldSpecs)
        {
            var path = GetFullPath(spec);
            if (!newByPath.ContainsKey(path))
            {
                changes.Add(new SpecChange(
                    spec.Description,
                    spec.ContextPath,
                    SpecChangeType.Deleted,
                    OldLineNumber: spec.LineNumber));
            }
        }

        return new SpecChangeSet(filePath, changes, HasDynamicSpecs: false, DependencyChanged: false);
    }

    private static string GetFullPath(StaticSpec spec)
        => string.Join(" > ", spec.ContextPath.Append(spec.Description));

    private static bool HasChanged(StaticSpec oldSpec, StaticSpec newSpec)
    {
        // Line number change indicates body modification
        return oldSpec.LineNumber != newSpec.LineNumber
            || oldSpec.Type != newSpec.Type
            || oldSpec.IsPending != newSpec.IsPending;
    }
}
