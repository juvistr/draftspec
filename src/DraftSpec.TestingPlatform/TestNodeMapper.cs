using Microsoft.Testing.Platform.Extensions.Messages;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Maps between DraftSpec types and MTP TestNode messages.
/// </summary>
internal static class TestNodeMapper
{
    /// <summary>
    /// Creates a TestNode for a discovered spec (discovery phase).
    /// </summary>
    public static TestNode CreateDiscoveryNode(DiscoveredSpec spec)
    {
        var properties = new PropertyBag(DiscoveredTestNodeStateProperty.CachedInstance);

        return new TestNode
        {
            Uid = new TestNodeUid(spec.Id),
            DisplayName = spec.DisplayName,
            Properties = properties
        };
    }

    /// <summary>
    /// Creates a TestNode for a spec result (execution phase).
    /// </summary>
    public static TestNode CreateResultNode(DiscoveredSpec spec, SpecResult result)
    {
        var stateProperty = GetStateProperty(result);
        var properties = new PropertyBag(stateProperty);

        // Add timing property if we have duration info
        if (result.TotalDuration > TimeSpan.Zero)
        {
            properties = new PropertyBag(
                stateProperty,
                new TimingProperty(new TimingInfo(
                    DateTimeOffset.UtcNow - result.TotalDuration,
                    DateTimeOffset.UtcNow,
                    result.TotalDuration)));
        }

        return new TestNode
        {
            Uid = new TestNodeUid(spec.Id),
            DisplayName = spec.DisplayName,
            Properties = properties
        };
    }

    /// <summary>
    /// Creates a TestNode for a spec result using the result's own context path.
    /// Used when we don't have a DiscoveredSpec (running all specs).
    /// </summary>
    public static TestNode CreateResultNode(
        string relativeSourceFile,
        SpecResult result)
    {
        var id = GenerateStableId(relativeSourceFile, result.ContextPath, result.Spec.Description);
        var displayName = GenerateDisplayName(result.ContextPath, result.Spec.Description);
        var stateProperty = GetStateProperty(result);

        var properties = result.TotalDuration > TimeSpan.Zero
            ? new PropertyBag(
                stateProperty,
                new TimingProperty(new TimingInfo(
                    DateTimeOffset.UtcNow - result.TotalDuration,
                    DateTimeOffset.UtcNow,
                    result.TotalDuration)))
            : new PropertyBag(stateProperty);

        return new TestNode
        {
            Uid = new TestNodeUid(id),
            DisplayName = displayName,
            Properties = properties
        };
    }

    /// <summary>
    /// Gets the appropriate state property for a spec result.
    /// </summary>
    private static TestNodeStateProperty GetStateProperty(SpecResult result)
    {
        return result.Status switch
        {
            SpecStatus.Passed => PassedTestNodeStateProperty.CachedInstance,
            SpecStatus.Failed => result.Exception != null
                ? new FailedTestNodeStateProperty(result.Exception, result.Exception.Message)
                : new FailedTestNodeStateProperty("Test failed"),
            SpecStatus.Pending => new SkippedTestNodeStateProperty("Pending - no implementation"),
            SpecStatus.Skipped => new SkippedTestNodeStateProperty("Skipped"),
            _ => throw new ArgumentOutOfRangeException(nameof(result.Status))
        };
    }

    /// <summary>
    /// Generates a stable, unique ID for a spec result.
    /// Format: relative/path/file.spec.csx:Context/Path/spec description
    /// </summary>
    public static string GenerateStableId(
        string relativeSourceFile,
        IReadOnlyList<string> contextPath,
        string specDescription)
    {
        // Normalize path separators to forward slashes for cross-platform consistency
        var normalizedPath = relativeSourceFile.Replace('\\', '/');

        // Build context path with forward slashes
        var contextPathString = string.Join("/", contextPath);

        return $"{normalizedPath}:{contextPathString}/{specDescription}";
    }

    /// <summary>
    /// Generates a human-readable display name.
    /// Format: Context > Path > spec description
    /// </summary>
    public static string GenerateDisplayName(
        IReadOnlyList<string> contextPath,
        string specDescription)
    {
        var parts = new List<string>(contextPath) { specDescription };
        return string.Join(" > ", parts);
    }
}
