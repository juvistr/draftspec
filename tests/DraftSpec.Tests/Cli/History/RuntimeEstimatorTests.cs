using DraftSpec.Cli.History;

namespace DraftSpec.Tests.Cli.History;

/// <summary>
/// Tests for RuntimeEstimator which calculates runtime estimates from historical data.
/// </summary>
public class RuntimeEstimatorTests
{
    private RuntimeEstimator _estimator = null!;

    [Before(Test)]
    public void SetUp()
    {
        _estimator = new RuntimeEstimator();
    }

    #region CalculatePercentile Tests

    [Test]
    public async Task CalculatePercentile_EmptyList_ReturnsZero()
    {
        var result = _estimator.CalculatePercentile([], 50);

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task CalculatePercentile_SingleValue_ReturnsValue()
    {
        var result = _estimator.CalculatePercentile([100.0], 50);

        await Assert.That(result).IsEqualTo(100.0);
    }

    [Test]
    public async Task CalculatePercentile_P50_ReturnsMedian()
    {
        // 10, 20, 30, 40, 50 - median is 30
        var values = new List<double> { 10, 20, 30, 40, 50 };

        var result = _estimator.CalculatePercentile(values, 50);

        await Assert.That(result).IsEqualTo(30);
    }

    [Test]
    public async Task CalculatePercentile_P95_ReturnsHighValue()
    {
        // 10 values: 10, 20, 30, 40, 50, 60, 70, 80, 90, 100
        // P95 should be 100 (95% of 10 = 9.5, ceiling = 10, index 9)
        var values = new List<double> { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

        var result = _estimator.CalculatePercentile(values, 95);

        await Assert.That(result).IsEqualTo(100);
    }

    [Test]
    public async Task CalculatePercentile_P99_ReturnsMaxForSmallList()
    {
        var values = new List<double> { 10, 20, 30, 40, 50 };

        var result = _estimator.CalculatePercentile(values, 99);

        await Assert.That(result).IsEqualTo(50);
    }

    [Test]
    public async Task CalculatePercentile_P1_ReturnsMinValue()
    {
        var values = new List<double> { 10, 20, 30, 40, 50 };

        var result = _estimator.CalculatePercentile(values, 1);

        await Assert.That(result).IsEqualTo(10);
    }

    [Test]
    public async Task CalculatePercentile_UnsortedInput_SortsAndCalculates()
    {
        // Input is unsorted
        var values = new List<double> { 50, 10, 40, 20, 30 };

        var result = _estimator.CalculatePercentile(values, 50);

        await Assert.That(result).IsEqualTo(30);
    }

    #endregion

    #region Calculate - Empty History Tests

    [Test]
    public async Task Calculate_EmptyHistory_ReturnsZeroEstimate()
    {
        var history = SpecHistory.Empty;

        var result = _estimator.Calculate(history);

        await Assert.That(result.P50Ms).IsEqualTo(0);
        await Assert.That(result.P95Ms).IsEqualTo(0);
        await Assert.That(result.MaxMs).IsEqualTo(0);
        await Assert.That(result.TotalEstimateMs).IsEqualTo(0);
        await Assert.That(result.SampleSize).IsEqualTo(0);
        await Assert.That(result.SpecCount).IsEqualTo(0);
        await Assert.That(result.SlowestSpecs).IsEmpty();
    }

    [Test]
    public async Task Calculate_HistoryWithNoRuns_ReturnsZeroEstimate()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "spec1",
                    Runs = []
                }
            }
        };

        var result = _estimator.Calculate(history);

        await Assert.That(result.SpecCount).IsEqualTo(0);
        await Assert.That(result.TotalEstimateMs).IsEqualTo(0);
    }

    [Test]
    public async Task Calculate_HistoryWithZeroDurations_ReturnsZeroEstimate()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "spec1",
                    Runs = [
                        new SpecRun { Status = "passed", DurationMs = 0 },
                        new SpecRun { Status = "passed", DurationMs = 0 }
                    ]
                }
            }
        };

        var result = _estimator.Calculate(history);

        await Assert.That(result.SpecCount).IsEqualTo(0);
        await Assert.That(result.TotalEstimateMs).IsEqualTo(0);
    }

    #endregion

    #region Calculate - Single Spec Tests

    [Test]
    public async Task Calculate_SingleSpecSingleRun_ReturnsRunDuration()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "spec1",
                    Runs = [new SpecRun { Status = "passed", DurationMs = 100 }]
                }
            }
        };

        var result = _estimator.Calculate(history);

        await Assert.That(result.SpecCount).IsEqualTo(1);
        await Assert.That(result.TotalEstimateMs).IsEqualTo(100);
        await Assert.That(result.P50Ms).IsEqualTo(100);
        await Assert.That(result.P95Ms).IsEqualTo(100);
        await Assert.That(result.MaxMs).IsEqualTo(100);
        await Assert.That(result.SampleSize).IsEqualTo(1);
    }

    [Test]
    public async Task Calculate_SingleSpecMultipleRuns_ReturnsPercentileEstimate()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "spec1",
                    Runs = [
                        new SpecRun { Status = "passed", DurationMs = 100 },
                        new SpecRun { Status = "passed", DurationMs = 200 },
                        new SpecRun { Status = "passed", DurationMs = 300 },
                        new SpecRun { Status = "passed", DurationMs = 400 },
                        new SpecRun { Status = "passed", DurationMs = 500 }
                    ]
                }
            }
        };

        var result = _estimator.Calculate(history, percentile: 50);

        await Assert.That(result.SpecCount).IsEqualTo(1);
        await Assert.That(result.TotalEstimateMs).IsEqualTo(300); // P50 = median
        await Assert.That(result.P50Ms).IsEqualTo(300);
        await Assert.That(result.MaxMs).IsEqualTo(500);
        await Assert.That(result.SampleSize).IsEqualTo(5);
    }

    #endregion

    #region Calculate - Multiple Specs Tests

    [Test]
    public async Task Calculate_MultipleSpecs_SumsEstimates()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "spec1",
                    Runs = [new SpecRun { Status = "passed", DurationMs = 100 }]
                },
                ["test:spec2"] = new SpecHistoryEntry
                {
                    DisplayName = "spec2",
                    Runs = [new SpecRun { Status = "passed", DurationMs = 200 }]
                },
                ["test:spec3"] = new SpecHistoryEntry
                {
                    DisplayName = "spec3",
                    Runs = [new SpecRun { Status = "passed", DurationMs = 300 }]
                }
            }
        };

        var result = _estimator.Calculate(history);

        await Assert.That(result.SpecCount).IsEqualTo(3);
        await Assert.That(result.TotalEstimateMs).IsEqualTo(600); // 100 + 200 + 300
        await Assert.That(result.P50Ms).IsEqualTo(600);
        await Assert.That(result.MaxMs).IsEqualTo(600);
    }

    [Test]
    public async Task Calculate_MultipleSpecsWithVaryingRuns_CalculatesPerSpecThenSums()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:fast"] = new SpecHistoryEntry
                {
                    DisplayName = "fast spec",
                    Runs = [
                        new SpecRun { Status = "passed", DurationMs = 10 },
                        new SpecRun { Status = "passed", DurationMs = 20 },
                        new SpecRun { Status = "passed", DurationMs = 30 }
                    ]
                },
                ["test:slow"] = new SpecHistoryEntry
                {
                    DisplayName = "slow spec",
                    Runs = [
                        new SpecRun { Status = "passed", DurationMs = 1000 },
                        new SpecRun { Status = "passed", DurationMs = 2000 },
                        new SpecRun { Status = "passed", DurationMs = 3000 }
                    ]
                }
            }
        };

        var result = _estimator.Calculate(history, percentile: 50);

        // fast spec P50 = 20ms, slow spec P50 = 2000ms
        await Assert.That(result.SpecCount).IsEqualTo(2);
        await Assert.That(result.TotalEstimateMs).IsEqualTo(2020); // 20 + 2000
    }

    #endregion

    #region Calculate - Slowest Specs Tests

    [Test]
    public async Task Calculate_ReturnsSlowestSpecsInOrder()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:fast"] = new SpecHistoryEntry
                {
                    DisplayName = "fast spec",
                    Runs = [new SpecRun { Status = "passed", DurationMs = 10 }]
                },
                ["test:medium"] = new SpecHistoryEntry
                {
                    DisplayName = "medium spec",
                    Runs = [new SpecRun { Status = "passed", DurationMs = 100 }]
                },
                ["test:slow"] = new SpecHistoryEntry
                {
                    DisplayName = "slow spec",
                    Runs = [new SpecRun { Status = "passed", DurationMs = 1000 }]
                }
            }
        };

        var result = _estimator.Calculate(history);

        await Assert.That(result.SlowestSpecs.Count).IsEqualTo(3);
        await Assert.That(result.SlowestSpecs[0].DisplayName).IsEqualTo("slow spec");
        await Assert.That(result.SlowestSpecs[0].EstimateMs).IsEqualTo(1000);
        await Assert.That(result.SlowestSpecs[1].DisplayName).IsEqualTo("medium spec");
        await Assert.That(result.SlowestSpecs[2].DisplayName).IsEqualTo("fast spec");
    }

    [Test]
    public async Task Calculate_LimitsToTopFiveSlowestSpecs()
    {
        var specs = new Dictionary<string, SpecHistoryEntry>();
        for (int i = 1; i <= 10; i++)
        {
            specs[$"test:spec{i}"] = new SpecHistoryEntry
            {
                DisplayName = $"spec{i}",
                Runs = [new SpecRun { Status = "passed", DurationMs = i * 100 }]
            };
        }

        var history = new SpecHistory { Specs = specs };

        var result = _estimator.Calculate(history);

        await Assert.That(result.SlowestSpecs.Count).IsEqualTo(5);
        await Assert.That(result.SlowestSpecs[0].EstimateMs).IsEqualTo(1000); // spec10
        await Assert.That(result.SlowestSpecs[4].EstimateMs).IsEqualTo(600);  // spec6
    }

    [Test]
    public async Task Calculate_SlowestSpecsIncludeSpecIdAndRunCount()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test.spec.csx:Context/spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "Context > spec1",
                    Runs = [
                        new SpecRun { Status = "passed", DurationMs = 100 },
                        new SpecRun { Status = "passed", DurationMs = 200 },
                        new SpecRun { Status = "passed", DurationMs = 300 }
                    ]
                }
            }
        };

        var result = _estimator.Calculate(history);

        await Assert.That(result.SlowestSpecs[0].SpecId).IsEqualTo("test.spec.csx:Context/spec1");
        await Assert.That(result.SlowestSpecs[0].DisplayName).IsEqualTo("Context > spec1");
        await Assert.That(result.SlowestSpecs[0].RunCount).IsEqualTo(3);
    }

    #endregion

    #region Calculate - Percentile Parameter Tests

    [Test]
    public async Task Calculate_CustomPercentile_UsesForTotalEstimate()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "spec1",
                    Runs = [
                        new SpecRun { Status = "passed", DurationMs = 100 },
                        new SpecRun { Status = "passed", DurationMs = 200 },
                        new SpecRun { Status = "passed", DurationMs = 300 },
                        new SpecRun { Status = "passed", DurationMs = 400 },
                        new SpecRun { Status = "passed", DurationMs = 500 }
                    ]
                }
            }
        };

        var resultP50 = _estimator.Calculate(history, percentile: 50);
        var resultP95 = _estimator.Calculate(history, percentile: 95);

        await Assert.That(resultP50.TotalEstimateMs).IsEqualTo(300);
        await Assert.That(resultP50.Percentile).IsEqualTo(50);
        await Assert.That(resultP95.TotalEstimateMs).IsEqualTo(500);
        await Assert.That(resultP95.Percentile).IsEqualTo(95);
    }

    [Test]
    public async Task Calculate_AlwaysReturnsP50AndP95Regardless()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "spec1",
                    Runs = [
                        new SpecRun { Status = "passed", DurationMs = 100 },
                        new SpecRun { Status = "passed", DurationMs = 200 },
                        new SpecRun { Status = "passed", DurationMs = 300 },
                        new SpecRun { Status = "passed", DurationMs = 400 },
                        new SpecRun { Status = "passed", DurationMs = 500 }
                    ]
                }
            }
        };

        // Request P75 but still get P50 and P95
        var result = _estimator.Calculate(history, percentile: 75);

        await Assert.That(result.P50Ms).IsEqualTo(300);
        await Assert.That(result.P95Ms).IsEqualTo(500);
        await Assert.That(result.TotalEstimateMs).IsEqualTo(400); // P75
        await Assert.That(result.Percentile).IsEqualTo(75);
    }

    #endregion

    #region Calculate - Mixed Data Tests

    [Test]
    public async Task Calculate_MixedValidAndZeroDurations_IgnoresZeros()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "spec1",
                    Runs = [
                        new SpecRun { Status = "passed", DurationMs = 0 },  // ignored
                        new SpecRun { Status = "passed", DurationMs = 100 },
                        new SpecRun { Status = "passed", DurationMs = 0 },  // ignored
                        new SpecRun { Status = "passed", DurationMs = 200 }
                    ]
                }
            }
        };

        var result = _estimator.Calculate(history, percentile: 50);

        // Only considers 100 and 200
        await Assert.That(result.SampleSize).IsEqualTo(2);
        await Assert.That(result.TotalEstimateMs).IsEqualTo(100); // P50 of [100, 200] = 100
    }

    [Test]
    public async Task Calculate_MultipleSpecsWithMixedData_HandlesCorrectly()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "spec1",
                    Runs = [new SpecRun { Status = "passed", DurationMs = 100 }]
                },
                ["test:spec2"] = new SpecHistoryEntry
                {
                    DisplayName = "spec2",
                    Runs = [] // no runs
                },
                ["test:spec3"] = new SpecHistoryEntry
                {
                    DisplayName = "spec3",
                    Runs = [new SpecRun { Status = "passed", DurationMs = 0 }] // zero duration
                },
                ["test:spec4"] = new SpecHistoryEntry
                {
                    DisplayName = "spec4",
                    Runs = [new SpecRun { Status = "passed", DurationMs = 200 }]
                }
            }
        };

        var result = _estimator.Calculate(history);

        // Only spec1 and spec4 have valid data
        await Assert.That(result.SpecCount).IsEqualTo(2);
        await Assert.That(result.TotalEstimateMs).IsEqualTo(300); // 100 + 200
    }

    #endregion
}
