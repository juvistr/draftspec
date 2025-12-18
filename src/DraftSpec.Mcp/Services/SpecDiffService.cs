using DraftSpec.Mcp.Models;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Compares two spec runs to detect regressions and fixes.
/// </summary>
public static class SpecDiffService
{
    /// <summary>
    /// Compare baseline and current spec reports to detect changes.
    /// </summary>
    /// <param name="baseline">The baseline spec report (previous run)</param>
    /// <param name="current">The current spec report (new run)</param>
    /// <returns>Diff result with changes and summary</returns>
    public static SpecDiff Compare(SpecReport? baseline, SpecReport? current)
    {
        if (baseline == null && current == null)
        {
            return new SpecDiff();
        }

        var baselineSpecs = baseline != null ? FlattenSpecs(baseline) : [];
        var currentSpecs = current != null ? FlattenSpecs(current) : [];

        var changes = new List<SpecChange>();
        var newPassing = 0;
        var newFailing = 0;
        var stillFailing = 0;
        var stillPassing = 0;
        var newSpecs = 0;
        var removedSpecs = 0;

        // Find specs in current that exist in baseline
        foreach (var (path, currentSpec) in currentSpecs)
        {
            if (baselineSpecs.TryGetValue(path, out var baselineSpec))
            {
                var baselineStatus = NormalizeStatus(baselineSpec.Status);
                var currentStatus = NormalizeStatus(currentSpec.Status);

                if (baselineStatus == currentStatus)
                {
                    // No change
                    if (currentStatus == "passed")
                        stillPassing++;
                    else if (currentStatus == "failed")
                        stillFailing++;
                }
                else if (baselineStatus == "passed" && currentStatus == "failed")
                {
                    // Regression!
                    newFailing++;
                    changes.Add(new SpecChange
                    {
                        SpecPath = path,
                        Type = ChangeType.Regression,
                        OldStatus = baselineSpec.Status,
                        NewStatus = currentSpec.Status,
                        ErrorMessage = currentSpec.Error
                    });
                }
                else if (baselineStatus == "failed" && currentStatus == "passed")
                {
                    // Fix!
                    newPassing++;
                    changes.Add(new SpecChange
                    {
                        SpecPath = path,
                        Type = ChangeType.Fix,
                        OldStatus = baselineSpec.Status,
                        NewStatus = currentSpec.Status
                    });
                }
                else
                {
                    // Other status change (e.g., pending -> skipped)
                    changes.Add(new SpecChange
                    {
                        SpecPath = path,
                        Type = ChangeType.StatusChange,
                        OldStatus = baselineSpec.Status,
                        NewStatus = currentSpec.Status
                    });
                }
            }
            else
            {
                // New spec
                newSpecs++;
                changes.Add(new SpecChange
                {
                    SpecPath = path,
                    Type = ChangeType.New,
                    OldStatus = null,
                    NewStatus = currentSpec.Status
                });
            }
        }

        // Find specs in baseline that don't exist in current
        foreach (var (path, baselineSpec) in baselineSpecs)
        {
            if (!currentSpecs.ContainsKey(path))
            {
                removedSpecs++;
                changes.Add(new SpecChange
                {
                    SpecPath = path,
                    Type = ChangeType.Removed,
                    OldStatus = baselineSpec.Status,
                    NewStatus = null
                });
            }
        }

        return new SpecDiff
        {
            NewPassing = newPassing,
            NewFailing = newFailing,
            StillFailing = stillFailing,
            StillPassing = stillPassing,
            NewSpecs = newSpecs,
            RemovedSpecs = removedSpecs,
            Changes = changes
        };
    }

    /// <summary>
    /// Flatten a spec report into a dictionary of path -> spec result.
    /// </summary>
    private static Dictionary<string, SpecResultReport> FlattenSpecs(SpecReport report)
    {
        var result = new Dictionary<string, SpecResultReport>();
        FlattenContext(report.Contexts, [], result);
        return result;
    }

    private static void FlattenContext(
        List<SpecContextReport> contexts,
        List<string> parentPath,
        Dictionary<string, SpecResultReport> result)
    {
        foreach (var context in contexts)
        {
            var currentPath = parentPath.Concat([context.Description]).ToList();

            // Add specs from this context
            foreach (var spec in context.Specs)
            {
                var fullPath = string.Join(" > ", currentPath.Concat([spec.Description]));
                result[fullPath] = spec;
            }

            // Recurse into nested contexts
            FlattenContext(context.Contexts, currentPath, result);
        }
    }

    /// <summary>
    /// Normalize status string for comparison.
    /// </summary>
    private static string NormalizeStatus(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "pass" or "passed" => "passed",
            "fail" or "failed" => "failed",
            "pending" => "pending",
            "skip" or "skipped" => "skipped",
            _ => status.ToLowerInvariant()
        };
    }
}
