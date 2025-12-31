using DraftSpec.Coverage;
using DraftSpec.Middleware;

namespace DraftSpec;

/// <summary>
/// The result of executing a single spec, including status, timing, and error information.
/// </summary>
/// <param name="Spec">The spec definition that was executed.</param>
/// <param name="Status">The execution outcome (Passed, Failed, Pending, or Skipped).</param>
/// <param name="ContextPath">The breadcrumb trail of context descriptions from root to the spec's parent.</param>
/// <param name="Duration">The time taken to execute the spec body (excludes hooks).</param>
/// <param name="Exception">The exception that caused failure, if Status is Failed.</param>
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
    /// Time spent executing beforeEach hooks (from all ancestor contexts).
    /// </summary>
    public TimeSpan BeforeEachDuration { get; init; }

    /// <summary>
    /// Time spent executing afterEach hooks (from all ancestor contexts).
    /// </summary>
    public TimeSpan AfterEachDuration { get; init; }

    /// <summary>
    /// Total execution time including hooks and spec body.
    /// </summary>
    public TimeSpan TotalDuration => BeforeEachDuration + Duration + AfterEachDuration;

    /// <summary>
    /// Per-spec coverage data if coverage tracking is enabled.
    /// Null if coverage middleware is not configured or not active.
    /// </summary>
    public SpecCoverageData? CoverageInfo { get; init; }

    /// <summary>
    /// Cached full description to avoid repeated string allocations.
    /// </summary>
    private string? _fullDescription;

    /// <summary>
    /// Full description including context path and spec description, space-separated.
    /// Lazily computed and cached on first access.
    /// </summary>
    public string FullDescription => _fullDescription ??= string.Join(" ", ContextPath.Append(Spec.Description));
}
