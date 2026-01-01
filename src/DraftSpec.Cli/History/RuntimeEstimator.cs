namespace DraftSpec.Cli.History;

/// <summary>
/// Default implementation of runtime estimation using percentile calculations.
/// </summary>
public class RuntimeEstimator : IRuntimeEstimator
{
    private const int TopSlowestCount = 5;

    /// <summary>
    /// Cached spec data with pre-sorted durations for efficient percentile calculations.
    /// </summary>
    private readonly record struct SpecData(
        string SpecId,
        string DisplayName,
        IReadOnlyList<double> SortedDurations);

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

        // Pre-compute sorted durations once per spec
        var specDataList = new List<SpecData>();

        foreach (var (specId, entry) in history.Specs)
        {
            if (entry.Runs.Count == 0) continue;

            var sortedDurations = entry.Runs
                .Where(r => r.DurationMs > 0)
                .Select(r => (double)r.DurationMs)
                .OrderBy(d => d)
                .ToList();

            if (sortedDurations.Count == 0) continue;

            specDataList.Add(new SpecData(specId, entry.DisplayName, sortedDurations));
        }

        if (specDataList.Count == 0)
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

        // Calculate all metrics using pre-sorted durations (no re-sorting)
        var specEstimates = new List<SpecEstimate>();
        double p50Total = 0, p95Total = 0, maxTotal = 0, totalAtPercentile = 0;
        var totalRunCount = 0;

        foreach (var spec in specDataList)
        {
            var estimate = CalculatePercentileFromSorted(spec.SortedDurations, percentile);
            var p50 = CalculatePercentileFromSorted(spec.SortedDurations, 50);
            var p95 = CalculatePercentileFromSorted(spec.SortedDurations, 95);
            var max = spec.SortedDurations[^1]; // Last element is max in sorted list

            totalAtPercentile += estimate;
            p50Total += p50;
            p95Total += p95;
            maxTotal += max;
            totalRunCount += spec.SortedDurations.Count;

            specEstimates.Add(new SpecEstimate
            {
                SpecId = spec.SpecId,
                DisplayName = spec.DisplayName,
                EstimateMs = estimate,
                RunCount = spec.SortedDurations.Count
            });
        }

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
            SampleSize = totalRunCount / specDataList.Count,
            SpecCount = specEstimates.Count,
            SlowestSpecs = slowestSpecs
        };
    }

    public double CalculatePercentile(IReadOnlyList<double> values, int percentile)
    {
        if (values.Count == 0) return 0;
        if (values.Count == 1) return values[0];

        var sorted = values.OrderBy(v => v).ToList();
        return CalculatePercentileFromSorted(sorted, percentile);
    }

    /// <summary>
    /// Calculates a percentile value from a pre-sorted list (no re-sorting).
    /// </summary>
    private static double CalculatePercentileFromSorted(IReadOnlyList<double> sortedValues, int percentile)
    {
        if (sortedValues.Count == 0) return 0;
        if (sortedValues.Count == 1) return sortedValues[0];

        var index = (int)Math.Ceiling(percentile / 100.0 * sortedValues.Count) - 1;
        return sortedValues[Math.Clamp(index, 0, sortedValues.Count - 1)];
    }
}
