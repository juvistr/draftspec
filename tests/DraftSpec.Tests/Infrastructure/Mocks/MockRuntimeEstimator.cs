using DraftSpec.Cli.History;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of IRuntimeEstimator for testing.
/// </summary>
public class MockRuntimeEstimator : IRuntimeEstimator
{
    private RuntimeEstimate _estimate = new()
    {
        P50Ms = 0,
        P95Ms = 0,
        MaxMs = 0,
        TotalEstimateMs = 0,
        Percentile = 50,
        SampleSize = 0,
        SpecCount = 0,
        SlowestSpecs = []
    };

    private readonly Dictionary<int, double> _percentileResults = new();

    /// <summary>
    /// Gets how many times Calculate was called.
    /// </summary>
    public int CalculateCalls { get; private set; }

    /// <summary>
    /// Gets the last history passed to Calculate.
    /// </summary>
    public SpecHistory? LastHistory { get; private set; }

    /// <summary>
    /// Gets the last percentile passed to Calculate.
    /// </summary>
    public int LastPercentile { get; private set; }

    /// <summary>
    /// Configure the estimate to return.
    /// </summary>
    public MockRuntimeEstimator WithEstimate(RuntimeEstimate estimate)
    {
        _estimate = estimate;
        return this;
    }

    /// <summary>
    /// Configure a percentile calculation result.
    /// </summary>
    public MockRuntimeEstimator WithPercentileResult(int percentile, double result)
    {
        _percentileResults[percentile] = result;
        return this;
    }

    public RuntimeEstimate Calculate(SpecHistory history, int percentile = 50)
    {
        CalculateCalls++;
        LastHistory = history;
        LastPercentile = percentile;
        return _estimate;
    }

    public double CalculatePercentile(IReadOnlyList<double> values, int percentile)
    {
        if (_percentileResults.TryGetValue(percentile, out var result))
        {
            return result;
        }

        // Default: return simple calculation
        if (values.Count == 0) return 0;
        if (values.Count == 1) return values[0];
        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
    }
}
