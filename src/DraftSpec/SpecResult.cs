using DraftSpec.Middleware;

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
public sealed record SpecResult(
    SpecDefinition Spec,
    SpecStatus Status,
    IReadOnlyList<string> ContextPath,
    TimeSpan Duration = default,
    Exception? Exception = null)
{
    /// <summary>
    /// Retry information if the spec was retried.
    /// Null if no retry middleware was configured or no retries occurred.
    /// </summary>
    public RetryInfo? RetryInfo { get; init; }

    /// <summary>
    /// Full description including context path and spec description, space-separated.
    /// </summary>
    public string FullDescription => string.Join(" ", ContextPath.Append(Spec.Description));
}
