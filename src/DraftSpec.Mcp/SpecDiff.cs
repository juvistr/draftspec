using System.Text.Json.Serialization;

namespace DraftSpec.Mcp.Models;

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
