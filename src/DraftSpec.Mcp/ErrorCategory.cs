using System.Text.Json.Serialization;

namespace DraftSpec.Mcp.Models;

/// <summary>
/// Categories of errors that can occur during spec execution.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ErrorCategory
{
    /// <summary>No error.</summary>
    None,

    /// <summary>Assertion failure (expect() did not match).</summary>
    Assertion,

    /// <summary>C# compilation error (syntax, type errors).</summary>
    Compilation,

    /// <summary>Unhandled exception during spec execution.</summary>
    Runtime,

    /// <summary>Execution exceeded time limit.</summary>
    Timeout,

    /// <summary>Error in beforeAll or before hook.</summary>
    Setup,

    /// <summary>Error in afterAll or after hook.</summary>
    Teardown,

    /// <summary>Invalid spec structure or configuration.</summary>
    Configuration,

    /// <summary>Request rejected due to rate limiting.</summary>
    RateLimited,

    /// <summary>Input validation failed (e.g., content size limit exceeded).</summary>
    Validation
}
