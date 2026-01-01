using DraftSpec.Cli.IntegrationTests.Infrastructure;

namespace DraftSpec.Cli.IntegrationTests.Commands;

/// <summary>
/// Integration tests for the flaky command.
/// Tests run the actual CLI as a subprocess.
/// </summary>
[NotInParallel("FlakyCommand")]
public class FlakyCommandIntegrationTests : IntegrationTestBase
{
    #region No History

    [Test]
    public async Task NoHistory_ShowsRunSpecsMessage()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "flaky", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Flaky should succeed even without history");
        await Assert.That(result.Output).Contains("No test history found")
            .Because("Should indicate no history exists");
        await Assert.That(result.Output).Contains("draftspec run")
            .Because("Should suggest running specs first");
    }

    #endregion

    #region No Flaky Specs

    [Test]
    public async Task NoFlakySpecs_ShowsCleanMessage()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        // Create history with stable specs (no flakiness)
        CreateHistoryFile(specDir)
            .WithStableSpec("spec-1", "Feature > always passes", runCount: 10)
            .WithStableSpec("spec-2", "Feature > consistently passes", runCount: 10)
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "flaky", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("No flaky specs detected")
            .Because("Should indicate no flaky specs found");
    }

    [Test]
    public async Task NoFlakySpecs_ShowsAnalyzedCount()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        CreateHistoryFile(specDir)
            .WithStableSpec("spec-1", "Feature > test 1", runCount: 5)
            .WithStableSpec("spec-2", "Feature > test 2", runCount: 5)
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "flaky", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("Analyzed")
            .Because("Should show analysis info");
        await Assert.That(result.Output).Contains("2 specs")
            .Because("Should show number of specs analyzed");
    }

    #endregion

    #region Flaky Specs Detected

    [Test]
    public async Task DetectsFlakySpecs_ShowsReport()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        // Create history with a flaky spec (alternating pass/fail)
        CreateHistoryFile(specDir)
            .WithFlakySpec("flaky-spec", "Feature > unstable test", statusChanges: 5)
            .WithStableSpec("stable-spec", "Feature > stable test", runCount: 5)
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "flaky", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("Flaky Tests Detected")
            .Because("Should show flaky tests header");
        await Assert.That(result.Output).Contains("unstable test")
            .Because("Should list the flaky spec");
        await Assert.That(result.Output).Contains("pass rate")
            .Because("Should show pass rate");
    }

    [Test]
    public async Task DetectsFlakySpecs_ShowsSeverity()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        // Create history with high flakiness
        CreateHistoryFile(specDir)
            .WithFlakySpec("very-flaky", "Feature > very unstable", statusChanges: 8)
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "flaky", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("Flakiness:")
            .Because("Should show flakiness severity");
        await Assert.That(result.Output).Contains("status change")
            .Because("Should show status change count");
    }

    [Test]
    public async Task DetectsFlakySpecs_ShowsRecommendations()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        CreateHistoryFile(specDir)
            .WithFlakySpec("flaky-spec", "Feature > unstable", statusChanges: 4)
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "flaky", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("Recommendations")
            .Because("Should show recommendations section");
        await Assert.That(result.Output).Contains("--quarantine")
            .Because("Should suggest quarantine flag");
    }

    #endregion

    #region Clear Subcommand

    [Test]
    public async Task Clear_ExistingSpec_ClearsHistory()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        // Create history with a spec
        CreateHistoryFile(specDir)
            .WithFlakySpec("spec-to-clear", "Feature > to be cleared", statusChanges: 3)
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "flaky", ".", "--clear", "spec-to-clear");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Clear should succeed for existing spec");
        await Assert.That(result.Output).Contains("Cleared history for")
            .Because("Should confirm history was cleared");
    }

    [Test]
    public async Task Clear_NonexistentSpec_ReturnsOne()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        // Create history but without the spec we'll try to clear
        CreateHistoryFile(specDir)
            .WithStableSpec("other-spec", "Feature > other test", runCount: 3)
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "flaky", ".", "--clear", "nonexistent-spec");

        await Assert.That(result.ExitCode).IsEqualTo(1)
            .Because("Clear should fail for non-existent spec");
        await Assert.That(result.Output).Contains("No history found for")
            .Because("Should indicate spec was not found");
    }

    #endregion

    #region Custom Options

    [Test]
    public async Task MinStatusChanges_FiltersResults()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        // Create spec with exactly 3 status changes
        CreateHistoryFile(specDir)
            .WithFlakySpec("borderline-flaky", "Feature > borderline", statusChanges: 3)
            .Build();

        // With high threshold, should not detect as flaky
        var result = await RunCliInDirectoryAsync(specDir, "flaky", ".", "--min-changes", "5");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("No flaky specs detected")
            .Because("Higher threshold should filter out borderline flaky spec");
    }

    [Test]
    public async Task WindowSize_LimitsAnalysis()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        CreateHistoryFile(specDir)
            .WithFlakySpec("flaky-spec", "Feature > flaky", statusChanges: 3)
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "flaky", ".", "--window-size", "5");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("last 5 runs")
            .Because("Should show the window size used");
    }

    #endregion

    #region Error Handling

    [Test]
    public async Task InvalidDirectory_ReturnsExitCodeOne()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();
        var nonExistentPath = Path.Combine(specDir, "does-not-exist");

        var result = await RunCliInDirectoryAsync(specDir, "flaky", nonExistentPath);

        await Assert.That(result.ExitCode).IsEqualTo(1)
            .Because("Should fail for non-existent directory");
        await Assert.That(result.Output).Contains("not found")
            .Because("Should indicate directory was not found");
    }

    #endregion
}
