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
        var propertyList = new List<IProperty>
        {
            DiscoveredTestNodeStateProperty.CachedInstance
        };

        // Add file location for IDE navigation if we have line number
        if (spec.LineNumber > 0)
        {
            propertyList.Add(CreateFileLocationProperty(spec.SourceFile, spec.LineNumber));
        }

        // Add TestMethodIdentifierProperty for IDE integration (Rider requires this)
        propertyList.Add(CreateTestMethodIdentifier(spec));

        return new TestNode
        {
            Uid = new TestNodeUid(spec.Id),
            DisplayName = spec.DisplayName,
            Properties = new PropertyBag(propertyList.ToArray())
        };
    }

    /// <summary>
    /// Creates a TestNode for a spec result (execution phase).
    /// </summary>
    public static TestNode CreateResultNode(DiscoveredSpec spec, SpecResult result)
    {
        var stateProperty = GetStateProperty(result);
        var propertyList = new List<IProperty> { stateProperty };

        // Add timing property if we have duration info
        if (result.TotalDuration > TimeSpan.Zero)
        {
            propertyList.Add(new TimingProperty(new TimingInfo(
                DateTimeOffset.UtcNow - result.TotalDuration,
                DateTimeOffset.UtcNow,
                result.TotalDuration)));
        }

        // Add file location for IDE navigation if we have line number
        if (spec.LineNumber > 0)
        {
            propertyList.Add(CreateFileLocationProperty(spec.SourceFile, spec.LineNumber));
        }

        return new TestNode
        {
            Uid = new TestNodeUid(spec.Id),
            DisplayName = spec.DisplayName,
            Properties = new PropertyBag(propertyList.ToArray())
        };
    }

    /// <summary>
    /// Creates a TestNode for a spec result using the result's own context path.
    /// Used when we don't have a DiscoveredSpec (running all specs).
    /// </summary>
    public static TestNode CreateResultNode(
        string relativeSourceFile,
        string absoluteSourceFile,
        SpecResult result)
    {
        var id = GenerateStableId(relativeSourceFile, result.ContextPath, result.Spec.Description);
        var displayName = GenerateDisplayName(result.ContextPath, result.Spec.Description);
        var stateProperty = GetStateProperty(result);
        var propertyList = new List<IProperty> { stateProperty };

        if (result.TotalDuration > TimeSpan.Zero)
        {
            propertyList.Add(new TimingProperty(new TimingInfo(
                DateTimeOffset.UtcNow - result.TotalDuration,
                DateTimeOffset.UtcNow,
                result.TotalDuration)));
        }

        // Add file location for IDE navigation if we have line number
        if (result.Spec.LineNumber > 0)
        {
            propertyList.Add(CreateFileLocationProperty(absoluteSourceFile, result.Spec.LineNumber));
        }

        return new TestNode
        {
            Uid = new TestNodeUid(id),
            DisplayName = displayName,
            Properties = new PropertyBag(propertyList.ToArray())
        };
    }

    /// <summary>
    /// Creates a TestNode for a discovery error (failed compilation or script error).
    /// </summary>
    public static TestNode CreateErrorNode(DiscoveryError error)
    {
        var stateProperty = new ErrorTestNodeStateProperty(
            error.Exception ?? new Exception(error.Message),
            error.Message);

        var propertyList = new List<IProperty> { stateProperty };

        // Add file location pointing to line 1 of the failed file
        propertyList.Add(CreateFileLocationProperty(error.SourceFile, 1));

        return new TestNode
        {
            Uid = new TestNodeUid(error.Id),
            DisplayName = error.DisplayName,
            Properties = new PropertyBag(propertyList.ToArray())
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
    /// Returns just the spec description since the tree view provides context hierarchy.
    /// </summary>
    public static string GenerateDisplayName(
        IReadOnlyList<string> contextPath,
        string specDescription)
    {
        // Just return the spec description - the IDE tree view shows the hierarchy
        // via TestMethodIdentifierProperty (TypeName = context path, MethodName = description)
        return specDescription;
    }

    /// <summary>
    /// Creates a TestFileLocationProperty for IDE navigation.
    /// </summary>
    private static TestFileLocationProperty CreateFileLocationProperty(string filePath, int lineNumber)
    {
        // MTP uses 0-based line numbers, but CallerLineNumber provides 1-based
        var zeroBasedLine = Math.Max(0, lineNumber - 1);
        var linePosition = new LinePosition(zeroBasedLine, 0);
        var lineSpan = new LinePositionSpan(linePosition, linePosition);

        return new TestFileLocationProperty(filePath, lineSpan);
    }

    /// <summary>
    /// Creates a TestMethodIdentifierProperty for IDE integration.
    /// Rider and other IDEs require this property to properly display tests.
    /// For CSX-based specs, we create synthetic type/method names based on the spec hierarchy.
    /// </summary>
    private static TestMethodIdentifierProperty CreateTestMethodIdentifier(DiscoveredSpec spec)
    {
        // Use the CSX file name (without extension) as the synthetic "type name"
        // and the spec description as the "method name"
        var fileName = Path.GetFileNameWithoutExtension(spec.RelativeSourceFile);

        // Build a type name from the context path
        // ECMA-335 allows spaces in identifiers, and ManagedNameUtilities now supports them
        // via escaping (see https://github.com/microsoft/vstest/issues/2733)
        var typeName = spec.ContextPath.Count > 0
            ? string.Join(".", spec.ContextPath)
            : fileName;

        // Use the spec description as the method name (no sanitization needed)
        var methodName = spec.Description;

        // Get the assembly name from the DraftSpec.TestingPlatform assembly
        var assemblyName = typeof(TestNodeMapper).Assembly.FullName ?? "DraftSpec.TestingPlatform";

        return new TestMethodIdentifierProperty(
            assemblyFullName: assemblyName,
            @namespace: "DraftSpec.Specs",
            typeName: typeName,
            methodName: methodName,
            methodArity: 0,
            parameterTypeFullNames: [],
            returnTypeFullName: "System.Void"
        );
    }
}
