namespace DraftSpec;

public enum SpecStatus
{
    Passed,
    Failed,
    Pending,
    Skipped
}

/// <summary>
/// The result of executing a single spec.
/// </summary>
public class SpecResult
{
    public SpecDefinition Spec { get; }
    public SpecStatus Status { get; }
    public TimeSpan Duration { get; }
    public Exception? Exception { get; }

    /// <summary>
    /// The path of context descriptions leading to this spec (excluding the spec's own description).
    /// </summary>
    public IReadOnlyList<string> ContextPath { get; }

    /// <summary>
    /// Full description including context path and spec description, space-separated.
    /// </summary>
    public string FullDescription => string.Join(" ", ContextPath.Append(Spec.Description));

    public SpecResult(
        SpecDefinition spec,
        SpecStatus status,
        IReadOnlyList<string> contextPath,
        TimeSpan duration = default,
        Exception? exception = null)
    {
        Spec = spec;
        Status = status;
        ContextPath = contextPath;
        Duration = duration;
        Exception = exception;
    }
}
