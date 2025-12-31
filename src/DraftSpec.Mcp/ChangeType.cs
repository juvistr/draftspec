using System.Text.Json.Serialization;

namespace DraftSpec.Mcp.Models;

/// <summary>
/// Type of change detected between baseline and current spec results.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChangeType
{
    /// <summary>Was passing, now failing (regression).</summary>
    Regression,

    /// <summary>Was failing, now passing (fix).</summary>
    Fix,

    /// <summary>New spec that didn't exist in baseline.</summary>
    New,

    /// <summary>Spec existed in baseline but not in current.</summary>
    Removed,

    /// <summary>Status changed but not a regression or fix (e.g., pending to skipped).</summary>
    StatusChange
}
