namespace DraftSpec.Middleware;

/// <summary>
/// Information about retry attempts for a spec.
/// </summary>
public sealed record RetryInfo
{
    /// <summary>
    /// Number of times the spec was attempted.
    /// </summary>
    public required int Attempts { get; init; }

    /// <summary>
    /// Maximum retries configured.
    /// </summary>
    public required int MaxRetries { get; init; }

    /// <summary>
    /// Whether the spec eventually passed after retries.
    /// </summary>
    public bool PassedAfterRetry => Attempts > 1;
}
