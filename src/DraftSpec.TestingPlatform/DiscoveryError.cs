namespace DraftSpec.TestingPlatform;

/// <summary>
/// Represents an error that occurred during spec discovery.
/// </summary>
/// <remarks>
/// Discovery errors typically occur when a CSX file fails to compile
/// (e.g., missing methods, syntax errors, missing references).
/// These are surfaced as failed test nodes in the MTP UI.
/// </remarks>
public sealed class DiscoveryError
{
    /// <summary>
    /// Absolute path to the source file that failed.
    /// </summary>
    public required string SourceFile { get; init; }

    /// <summary>
    /// Relative path to the source file from the project root.
    /// </summary>
    public required string RelativeSourceFile { get; init; }

    /// <summary>
    /// The error message from the compiler or runtime.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The full exception that caused the error, if available.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Generates a stable ID for this error node.
    /// </summary>
    public string Id => $"{RelativeSourceFile}:DISCOVERY_ERROR";

    /// <summary>
    /// Display name for the error node.
    /// </summary>
    public string DisplayName => $"[Discovery Error] {RelativeSourceFile}";
}
