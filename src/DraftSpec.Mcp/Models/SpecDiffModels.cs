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

/// <summary>
/// Result of comparing two spec runs.
/// </summary>
public class SpecDiff
{
    /// <summary>
    /// Whether any regressions were detected.
    /// </summary>
    public bool HasRegressions => NewFailing > 0;

    /// <summary>
    /// Number of specs that were failing but now pass.
    /// </summary>
    public int NewPassing { get; init; }

    /// <summary>
    /// Number of specs that were passing but now fail (regressions).
    /// </summary>
    public int NewFailing { get; init; }

    /// <summary>
    /// Number of specs that failed in both runs.
    /// </summary>
    public int StillFailing { get; init; }

    /// <summary>
    /// Number of specs that passed in both runs.
    /// </summary>
    public int StillPassing { get; init; }

    /// <summary>
    /// Number of new specs not in baseline.
    /// </summary>
    public int NewSpecs { get; init; }

    /// <summary>
    /// Number of specs removed since baseline.
    /// </summary>
    public int RemovedSpecs { get; init; }

    /// <summary>
    /// Detailed list of changes.
    /// </summary>
    public List<SpecChange> Changes { get; init; } = [];

    /// <summary>
    /// Summary message for quick review.
    /// </summary>
    public string Summary => HasRegressions
        ? $"⚠️ {NewFailing} regression(s) detected"
        : NewPassing > 0
            ? $"✅ {NewPassing} fix(es), no regressions"
            : "✅ No changes in test results";
}

/// <summary>
/// A single change between baseline and current spec results.
/// </summary>
public class SpecChange
{
    /// <summary>
    /// Full context path of the spec (e.g., "Calculator > add > returns sum").
    /// </summary>
    public required string SpecPath { get; init; }

    /// <summary>
    /// Type of change.
    /// </summary>
    public ChangeType Type { get; init; }

    /// <summary>
    /// Status in the baseline run.
    /// </summary>
    public string? OldStatus { get; init; }

    /// <summary>
    /// Status in the current run.
    /// </summary>
    public string? NewStatus { get; init; }

    /// <summary>
    /// Error message if the spec failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
