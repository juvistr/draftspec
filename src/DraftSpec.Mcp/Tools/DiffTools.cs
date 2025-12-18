using System.ComponentModel;
using System.Text.Json;
using DraftSpec.Formatters;
using DraftSpec.Mcp.Models;
using DraftSpec.Mcp.Services;
using ModelContextProtocol.Server;
using MpcModels = DraftSpec.Mcp.Models;

namespace DraftSpec.Mcp.Tools;

/// <summary>
/// MCP tools for comparing spec results.
/// </summary>
[McpServerToolType]
public static class DiffTools
{
    /// <summary>
    /// Compare two spec runs to detect regressions and fixes.
    /// </summary>
    [McpServerTool(Name = "diff_specs")]
    [Description("Compare baseline and current spec results to detect regressions. " +
                 "Useful for CI/CD gates, PR reviews, and refactoring verification. " +
                 "Returns detailed changes including regressions (passed→failed), " +
                 "fixes (failed→passed), new specs, and removed specs.")]
    public static string DiffSpecs(
        [Description("Baseline spec results JSON (from a previous run_spec call)")]
        string baselineResults,
        [Description("Current spec results JSON (from the latest run_spec call)")]
        string currentResults)
    {
        MpcModels.SpecReport? baseline = null;
        MpcModels.SpecReport? current = null;

        // Parse baseline results
        if (!string.IsNullOrWhiteSpace(baselineResults))
        {
            try
            {
                // Try to parse as RunSpecResult first (full response from run_spec)
                var runResult = JsonSerializer.Deserialize<RunSpecResult>(baselineResults, JsonOptionsProvider.Default);
                baseline = runResult?.Report;

                // If no report in RunSpecResult, try parsing as direct SpecReport
                baseline ??= JsonSerializer.Deserialize<MpcModels.SpecReport>(baselineResults, JsonOptionsProvider.Default);
            }
            catch (JsonException)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "Failed to parse baseline results as valid spec report JSON"
                }, JsonOptionsProvider.Default);
            }
        }

        // Parse current results
        if (!string.IsNullOrWhiteSpace(currentResults))
        {
            try
            {
                var runResult = JsonSerializer.Deserialize<RunSpecResult>(currentResults, JsonOptionsProvider.Default);
                current = runResult?.Report;
                current ??= JsonSerializer.Deserialize<MpcModels.SpecReport>(currentResults, JsonOptionsProvider.Default);
            }
            catch (JsonException)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "Failed to parse current results as valid spec report JSON"
                }, JsonOptionsProvider.Default);
            }
        }

        var diff = SpecDiffService.Compare(baseline, current);

        return JsonSerializer.Serialize(diff, JsonOptionsProvider.Default);
    }
}
