using DraftSpec.Coverage;
using DraftSpec.Middleware;

namespace DraftSpec;

/// <summary>
/// Represents the execution status of a spec.
/// </summary>
public enum SpecStatus
{
    /// <summary>
    /// The spec executed successfully without throwing an exception.
    /// </summary>
    Passed,

    /// <summary>
    /// The spec threw an exception during execution.
    /// </summary>
    Failed,

    /// <summary>
    /// The spec has no body defined (placeholder for future implementation).
    /// </summary>
    Pending,

    /// <summary>
    /// The spec was skipped due to filtering, focus mode, or explicit skip.
    /// </summary>
    Skipped
}
