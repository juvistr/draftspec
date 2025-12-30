using DraftSpec.TestingPlatform;

namespace DraftSpec.Cli.Services;

/// <summary>
/// Collects pre-run statistics about discovered specs using static parsing.
/// </summary>
public class SpecStatsCollector : ISpecStatsCollector
{
    /// <inheritdoc />
    public async Task<SpecStats> CollectAsync(
        IReadOnlyList<string> specFiles,
        string projectPath,
        CancellationToken ct = default)
    {
        if (specFiles.Count == 0)
        {
            return new SpecStats(
                Total: 0,
                Regular: 0,
                Focused: 0,
                Skipped: 0,
                Pending: 0,
                HasFocusMode: false,
                FileCount: 0);
        }

        var parser = new StaticSpecParser(projectPath);
        var allSpecs = new List<StaticSpec>();

        foreach (var specFile in specFiles)
        {
            ct.ThrowIfCancellationRequested();

            var result = await parser.ParseFileAsync(specFile, ct);
            allSpecs.AddRange(result.Specs);
        }

        var focused = allSpecs.Count(s => s.Type == StaticSpecType.Focused);
        var skipped = allSpecs.Count(s => s.Type == StaticSpecType.Skipped);
        var pending = allSpecs.Count(s => s.IsPending);

        // Regular = total - focused - skipped
        // Note: pending is orthogonal to type (a pending spec can be regular, focused, or skipped)
        var regular = allSpecs.Count - focused - skipped;

        return new SpecStats(
            Total: allSpecs.Count,
            Regular: regular,
            Focused: focused,
            Skipped: skipped,
            Pending: pending,
            HasFocusMode: focused > 0,
            FileCount: specFiles.Count);
    }
}
