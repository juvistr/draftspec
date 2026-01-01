using DraftSpec.Cli.IntegrationTests.Infrastructure;

namespace DraftSpec.Cli.IntegrationTests.Commands;

/// <summary>
/// Integration tests for the estimate command.
/// Tests run the actual CLI as a subprocess.
/// </summary>
[NotInParallel("EstimateCommand")]
public class EstimateCommandIntegrationTests : IntegrationTestBase
{
    #region No History

    [Test]
    public async Task NoHistory_ShowsRunSpecsMessage()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "estimate", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Estimate should succeed even without history");
        await Assert.That(result.Output).Contains("No test history found")
            .Because("Should indicate no history exists");
        await Assert.That(result.Output).Contains("draftspec run")
            .Because("Should suggest running specs first");
    }

    #endregion

    #region With History

    [Test]
    public async Task WithHistory_ShowsP50P95Max()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        // Create history with timing data
        CreateHistoryFile(specDir)
            .WithTimedSpec("spec-1", "Feature > calculates totals", 100, 120, 110, 105, 115)
            .WithTimedSpec("spec-2", "Feature > validates input", 50, 55, 48, 52, 60)
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "estimate", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Estimate should succeed");
        await Assert.That(result.Output).Contains("Runtime Estimate")
            .Because("Should show estimate header");
        await Assert.That(result.Output).Contains("P50")
            .Because("Should show P50 median");
        await Assert.That(result.Output).Contains("P95")
            .Because("Should show P95");
        await Assert.That(result.Output).Contains("Max observed")
            .Because("Should show maximum observed time");
    }

    [Test]
    public async Task WithHistory_ShowsSlowestSpecs()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        // Create history with varied timing data
        CreateHistoryFile(specDir)
            .WithTimedSpec("slow-spec", "Feature > slow operation", 500, 600, 550)
            .WithTimedSpec("fast-spec", "Feature > fast operation", 10, 12, 11)
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "estimate", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("Slowest specs")
            .Because("Should show slowest specs section");
        await Assert.That(result.Output).Contains("slow operation")
            .Because("Should list the slow spec");
    }

    [Test]
    public async Task WithHistory_ShowsRecommendedTimeout()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        CreateHistoryFile(specDir)
            .WithTimedSpec("spec-1", "Feature > test", 100, 150, 120)
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "estimate", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("Recommended CI timeout")
            .Because("Should show recommended CI timeout");
        await Assert.That(result.Output).Contains("2x P95")
            .Because("Should explain timeout is 2x P95");
    }

    #endregion

    #region Output Seconds

    [Test]
    public async Task OutputSeconds_ReturnsMachineReadableNumber()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        // Create history with known timing
        CreateHistoryFile(specDir)
            .WithTimedSpec("spec-1", "Feature > test", 1000, 1000, 1000) // 1 second each
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "estimate", ".", "--output-seconds");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        // Output should be a simple number (seconds as decimal)
        var output = result.Output.Trim();
        await Assert.That(double.TryParse(output, out _)).IsTrue()
            .Because("Output should be a parseable number");
    }

    [Test]
    public async Task OutputSeconds_NoExtraOutput()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        CreateHistoryFile(specDir)
            .WithTimedSpec("spec-1", "Feature > test", 500)
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "estimate", ".", "--output-seconds");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        // Should not contain human-readable labels
        await Assert.That(result.Output).DoesNotContain("Runtime Estimate")
            .Because("Machine-readable mode should not include headers");
        await Assert.That(result.Output).DoesNotContain("P50")
            .Because("Machine-readable mode should not include percentile labels");
    }

    #endregion

    #region Custom Percentile

    [Test]
    public async Task CustomPercentile_UsesSpecifiedValue()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        CreateHistoryFile(specDir)
            .WithTimedSpec("spec-1", "Feature > test", 100, 200, 300, 400, 500)
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "estimate", ".", "--percentile", "75");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("P75")
            .Because("Should show the custom percentile in slowest specs section");
    }

    #endregion

    #region Error Handling

    [Test]
    public async Task InvalidDirectory_ReturnsExitCodeOne()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();
        var nonExistentPath = Path.Combine(specDir, "does-not-exist");

        var result = await RunCliInDirectoryAsync(specDir, "estimate", nonExistentPath);

        await Assert.That(result.ExitCode).IsEqualTo(1)
            .Because("Should fail for non-existent directory");
        await Assert.That(result.Output).Contains("not found")
            .Because("Should indicate directory was not found");
    }

    #endregion
}
