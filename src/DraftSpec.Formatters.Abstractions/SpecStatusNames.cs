namespace DraftSpec.Formatters;

/// <summary>
/// String constants for spec status values used in reports and formatters.
/// These correspond to the SpecStatus enum values in the core library.
/// </summary>
public static class SpecStatusNames
{
    /// <summary>
    /// The spec executed successfully without throwing an exception.
    /// </summary>
    public const string Passed = "passed";

    /// <summary>
    /// The spec threw an exception during execution.
    /// </summary>
    public const string Failed = "failed";

    /// <summary>
    /// The spec has no body defined (placeholder for future implementation).
    /// </summary>
    public const string Pending = "pending";

    /// <summary>
    /// The spec was skipped due to filtering, focus mode, or explicit skip.
    /// </summary>
    public const string Skipped = "skipped";
}
