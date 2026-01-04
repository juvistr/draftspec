using DraftSpec.Cli.History;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Estimate;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Estimate;

/// <summary>
/// Tests for <see cref="EstimateOutputPhase"/>.
/// </summary>
public class EstimateOutputPhaseTests
{
    private MockConsole _console = null!;

    [Before(Test)]
    public void SetUp()
    {
        _console = new MockConsole();
    }

    #region Empty History Tests

    [Test]
    public async Task ExecuteAsync_NoHistory_ShowsRunSpecsFirstMessage()
    {
        var estimator = new MockRuntimeEstimator();
        var phase = new EstimateOutputPhase(estimator);
        var context = CreateContext(SpecHistory.Empty);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No test history found");
        await Assert.That(_console.Output).Contains("Run specs first to collect data");
        await Assert.That(_console.Output).Contains("draftspec run");
    }

    [Test]
    public async Task ExecuteAsync_NoTimingData_ShowsNoTimingDataMessage()
    {
        var history = new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry { DisplayName = "spec1", Runs = [] }
            }
        };
        var estimate = new RuntimeEstimate
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
        var estimator = new MockRuntimeEstimator().WithEstimate(estimate);
        var phase = new EstimateOutputPhase(estimator);
        var context = CreateContext(history);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No timing data available");
    }

    #endregion

    #region Estimate Output Tests

    [Test]
    public async Task ExecuteAsync_WithEstimate_ShowsRuntimeEstimate()
    {
        var history = CreateHistoryWithSpec();
        var estimate = new RuntimeEstimate
        {
            P50Ms = 135000,  // 2m 15s
            P95Ms = 272000,  // 4m 32s
            MaxMs = 361000,  // 6m 01s
            TotalEstimateMs = 135000,
            Percentile = 50,
            SampleSize = 47,
            SpecCount = 10,
            SlowestSpecs = []
        };
        var estimator = new MockRuntimeEstimator().WithEstimate(estimate);
        var phase = new EstimateOutputPhase(estimator);
        var context = CreateContext(history);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("Runtime Estimate (based on 47 historical runs)");
        await Assert.That(_console.Output).Contains("P50 (median):");
        await Assert.That(_console.Output).Contains("2m 15s");
        await Assert.That(_console.Output).Contains("P95:");
        await Assert.That(_console.Output).Contains("4m 32s");
        await Assert.That(_console.Output).Contains("Max observed:");
        await Assert.That(_console.Output).Contains("6m 01s");
    }

    [Test]
    public async Task ExecuteAsync_WithSlowestSpecs_ShowsSlowestSpecs()
    {
        var history = CreateHistoryWithSpec();
        var estimate = new RuntimeEstimate
        {
            P50Ms = 10000,
            P95Ms = 20000,
            MaxMs = 30000,
            TotalEstimateMs = 10000,
            Percentile = 50,
            SampleSize = 10,
            SpecCount = 3,
            SlowestSpecs =
            [
                new SpecEstimate
                {
                    SpecId = "test:slow",
                    DisplayName = "Integration > Database > handles concurrent writes",
                    EstimateMs = 45000,
                    RunCount = 5
                },
                new SpecEstimate
                {
                    SpecId = "test:medium",
                    DisplayName = "EmailService > SendAsync > retries on failure",
                    EstimateMs = 23000,
                    RunCount = 5
                }
            ]
        };
        var estimator = new MockRuntimeEstimator().WithEstimate(estimate);
        var phase = new EstimateOutputPhase(estimator);
        var context = CreateContext(history);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("Slowest specs (P50):");
        await Assert.That(_console.Output).Contains("1.");
        await Assert.That(_console.Output).Contains("Integration > Database > handles concurrent writes");
        await Assert.That(_console.Output).Contains("45.0s");
        await Assert.That(_console.Output).Contains("2.");
        await Assert.That(_console.Output).Contains("EmailService > SendAsync > retries on failure");
    }

    [Test]
    public async Task ExecuteAsync_ShowsRecommendedCiTimeout()
    {
        var history = CreateHistoryWithSpec();
        var estimate = new RuntimeEstimate
        {
            P50Ms = 10000,
            P95Ms = 120000, // 2 minutes
            MaxMs = 180000,
            TotalEstimateMs = 10000,
            Percentile = 50,
            SampleSize = 10,
            SpecCount = 5,
            SlowestSpecs = []
        };
        var estimator = new MockRuntimeEstimator().WithEstimate(estimate);
        var phase = new EstimateOutputPhase(estimator);
        var context = CreateContext(history);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        // 2x P95 = 4 minutes
        await Assert.That(_console.Output).Contains("Recommended CI timeout:");
        await Assert.That(_console.Output).Contains("4m 00s");
        await Assert.That(_console.Output).Contains("(2x P95)");
    }

    #endregion

    #region OutputSeconds Mode Tests

    [Test]
    public async Task ExecuteAsync_OutputSeconds_ReturnsSecondsOnly()
    {
        var history = CreateHistoryWithSpec();
        var estimate = new RuntimeEstimate
        {
            P50Ms = 135500, // 135.5 seconds
            P95Ms = 200000,
            MaxMs = 300000,
            TotalEstimateMs = 135500,
            Percentile = 50,
            SampleSize = 10,
            SpecCount = 5,
            SlowestSpecs = []
        };
        var estimator = new MockRuntimeEstimator().WithEstimate(estimate);
        var phase = new EstimateOutputPhase(estimator);
        var context = CreateContext(history);
        context.Set(ContextKeys.OutputSeconds, true);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output.Trim()).IsEqualTo("135.5");
    }

    [Test]
    public async Task ExecuteAsync_OutputSeconds_DoesNotShowHumanReadableOutput()
    {
        var history = CreateHistoryWithSpec();
        var estimate = new RuntimeEstimate
        {
            P50Ms = 60000,
            P95Ms = 120000,
            MaxMs = 180000,
            TotalEstimateMs = 60000,
            Percentile = 50,
            SampleSize = 10,
            SpecCount = 5,
            SlowestSpecs = []
        };
        var estimator = new MockRuntimeEstimator().WithEstimate(estimate);
        var phase = new EstimateOutputPhase(estimator);
        var context = CreateContext(history);
        context.Set(ContextKeys.OutputSeconds, true);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).DoesNotContain("Runtime Estimate");
        await Assert.That(_console.Output).DoesNotContain("P50");
        await Assert.That(_console.Output).DoesNotContain("P95");
        await Assert.That(_console.Output).DoesNotContain("Recommended");
    }

    #endregion

    #region Percentile Option Tests

    [Test]
    public async Task ExecuteAsync_UsesPercentileFromContext()
    {
        var history = CreateHistoryWithSpec();
        var estimate = new RuntimeEstimate
        {
            P50Ms = 60000,
            P95Ms = 120000,
            MaxMs = 180000,
            TotalEstimateMs = 120000,
            Percentile = 95,
            SampleSize = 10,
            SpecCount = 5,
            SlowestSpecs =
            [
                new SpecEstimate
                {
                    SpecId = "test:slow",
                    DisplayName = "slow spec",
                    EstimateMs = 10000,
                    RunCount = 5
                }
            ]
        };
        var estimator = new MockRuntimeEstimator().WithEstimate(estimate);
        var phase = new EstimateOutputPhase(estimator);
        var context = CreateContext(history);
        context.Set(ContextKeys.Percentile, 95);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(estimator.LastPercentile).IsEqualTo(95);
        await Assert.That(_console.Output).Contains("Slowest specs (P95):");
    }

    #endregion

    #region Duration Formatting Tests

    [Test]
    public async Task ExecuteAsync_FormatsDurationInMilliseconds()
    {
        var history = CreateHistoryWithSpec();
        var estimate = new RuntimeEstimate
        {
            P50Ms = 500,
            P95Ms = 800,
            MaxMs = 950,
            TotalEstimateMs = 500,
            Percentile = 50,
            SampleSize = 5,
            SpecCount = 1,
            SlowestSpecs = []
        };
        var estimator = new MockRuntimeEstimator().WithEstimate(estimate);
        var phase = new EstimateOutputPhase(estimator);
        var context = CreateContext(history);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("500ms");
        await Assert.That(_console.Output).Contains("800ms");
    }

    [Test]
    public async Task ExecuteAsync_FormatsDurationInSeconds()
    {
        var history = CreateHistoryWithSpec();
        var estimate = new RuntimeEstimate
        {
            P50Ms = 5500,  // 5.5 seconds
            P95Ms = 10000,
            MaxMs = 15000,
            TotalEstimateMs = 5500,
            Percentile = 50,
            SampleSize = 5,
            SpecCount = 1,
            SlowestSpecs = []
        };
        var estimator = new MockRuntimeEstimator().WithEstimate(estimate);
        var phase = new EstimateOutputPhase(estimator);
        var context = CreateContext(history);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("5.5s");
    }

    [Test]
    public async Task ExecuteAsync_FormatsDurationInMinutes()
    {
        var history = CreateHistoryWithSpec();
        var estimate = new RuntimeEstimate
        {
            P50Ms = 135000, // 2m 15s
            P95Ms = 300000,
            MaxMs = 450000,
            TotalEstimateMs = 135000,
            Percentile = 50,
            SampleSize = 5,
            SpecCount = 1,
            SlowestSpecs = []
        };
        var estimator = new MockRuntimeEstimator().WithEstimate(estimate);
        var phase = new EstimateOutputPhase(estimator);
        var context = CreateContext(history);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("2m 15s");
    }

    [Test]
    public async Task ExecuteAsync_FormatsDurationInHours()
    {
        var history = CreateHistoryWithSpec();
        var estimate = new RuntimeEstimate
        {
            P50Ms = 3723000, // 1h 02m 03s
            P95Ms = 7200000,
            MaxMs = 10800000,
            TotalEstimateMs = 3723000,
            Percentile = 50,
            SampleSize = 5,
            SpecCount = 1,
            SlowestSpecs = []
        };
        var estimator = new MockRuntimeEstimator().WithEstimate(estimate);
        var phase = new EstimateOutputPhase(estimator);
        var context = CreateContext(history);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("1h 02m 03s");
    }

    #endregion

    #region FormatDuration Unit Tests

    [Test]
    public async Task FormatDuration_LessThanSecond_ReturnsMilliseconds()
    {
        var result = EstimateOutputPhase.FormatDuration(500);
        await Assert.That(result).IsEqualTo("500ms");
    }

    [Test]
    public async Task FormatDuration_BetweenSecondsAndMinutes_ReturnsSeconds()
    {
        var result = EstimateOutputPhase.FormatDuration(5500);
        await Assert.That(result).IsEqualTo("5.5s");
    }

    [Test]
    public async Task FormatDuration_BetweenMinutesAndHours_ReturnsMinutesAndSeconds()
    {
        var result = EstimateOutputPhase.FormatDuration(135000);
        await Assert.That(result).IsEqualTo("2m 15s");
    }

    [Test]
    public async Task FormatDuration_OverAnHour_ReturnsHoursMinutesSeconds()
    {
        var result = EstimateOutputPhase.FormatDuration(3723000);
        await Assert.That(result).IsEqualTo("1h 02m 03s");
    }

    #endregion

    #region Helper Methods

    private CommandContext CreateContext(SpecHistory history)
    {
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = new MockFileSystem()
        };
        context.Set(ContextKeys.History, history);
        context.Set(ContextKeys.Percentile, 50);
        context.Set(ContextKeys.OutputSeconds, false);
        return context;
    }

    private static SpecHistory CreateHistoryWithSpec()
    {
        return new SpecHistory
        {
            Specs = new Dictionary<string, SpecHistoryEntry>
            {
                ["test:spec1"] = new SpecHistoryEntry
                {
                    DisplayName = "spec1",
                    Runs = [new SpecRun { Status = "passed", DurationMs = 1000 }]
                }
            }
        };
    }

    #endregion
}
