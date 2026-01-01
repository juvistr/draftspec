namespace DraftSpec.Cli.History;

/// <summary>
/// Default implementation of runtime estimation using percentile calculations.
/// </summary>
public class RuntimeEstimator : IRuntimeEstimator
{
    private const int TopSlowestCount = 5;

    public RuntimeEstimate Calculate(SpecHistory history, int percentile = 50)
    {
        if (history.Specs.Count == 0)
        {
            return new RuntimeEstimate
            {
                P50Ms = 0,
                P95Ms = 0,
                MaxMs = 0,
                TotalEstimateMs = 0,
                Percentile = percentile,
                SampleSize = 0,
                SpecCount = 0,
                SlowestSpecs = []
            };
        }

        var specEstimates = new List<SpecEstimate>();
        var allRunCounts = new List<int>();

        foreach (var (specId, entry) in history.Specs)
        {
            if (entry.Runs.Count == 0) continue;

            var durations = entry.Runs
                .Where(r => r.DurationMs > 0)
                .Select(r => r.DurationMs)
                .ToList();

            if (durations.Count == 0) continue;

            var estimate = CalculatePercentile(durations, percentile);
            allRunCounts.Add(durations.Count);

            specEstimates.Add(new SpecEstimate
            {
                SpecId = specId,
                DisplayName = entry.DisplayName,
                EstimateMs = estimate,
                RunCount = durations.Count
            });
        }

        if (specEstimates.Count == 0)
        {
            return new RuntimeEstimate
            {
                P50Ms = 0,
                P95Ms = 0,
                MaxMs = 0,
                TotalEstimateMs = 0,
                Percentile = percentile,
                SampleSize = 0,
                SpecCount = 0,
                SlowestSpecs = []
            };
        }

        // Calculate totals by summing individual spec estimates
        var allEstimates = specEstimates.Select(s => s.EstimateMs).ToList();
        var totalAtPercentile = allEstimates.Sum();

        // For P50 and P95, recalculate each spec's contribution
        var p50Total = specEstimates.Sum(s =>
        {
            var entry = history.Specs[s.SpecId];
            var durations = entry.Runs.Where(r => r.DurationMs > 0).Select(r => r.DurationMs).ToList();
            return durations.Count > 0 ? CalculatePercentile(durations, 50) : 0;
        });

        var p95Total = specEstimates.Sum(s =>
        {
            var entry = history.Specs[s.SpecId];
            var durations = entry.Runs.Where(r => r.DurationMs > 0).Select(r => r.DurationMs).ToList();
            return durations.Count > 0 ? CalculatePercentile(durations, 95) : 0;
        });

        var maxTotal = specEstimates.Sum(s =>
        {
            var entry = history.Specs[s.SpecId];
            var durations = entry.Runs.Where(r => r.DurationMs > 0).Select(r => r.DurationMs).ToList();
            return durations.Count > 0 ? durations.Max() : 0;
        });

        var slowestSpecs = specEstimates
            .OrderByDescending(s => s.EstimateMs)
            .Take(TopSlowestCount)
            .ToList();

        return new RuntimeEstimate
        {
            P50Ms = p50Total,
            P95Ms = p95Total,
            MaxMs = maxTotal,
            TotalEstimateMs = totalAtPercentile,
            Percentile = percentile,
            SampleSize = allRunCounts.Count > 0 ? (int)allRunCounts.Average() : 0,
            SpecCount = specEstimates.Count,
            SlowestSpecs = slowestSpecs
        };
    }

    public double CalculatePercentile(IReadOnlyList<double> values, int percentile)
    {
        if (values.Count == 0) return 0;
        if (values.Count == 1) return values[0];

        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
    }
}
