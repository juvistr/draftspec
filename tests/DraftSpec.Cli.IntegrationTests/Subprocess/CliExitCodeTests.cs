using System.Text.Json;
using DraftSpec.Cli.IntegrationTests.Infrastructure;

namespace DraftSpec.Cli.IntegrationTests.Subprocess;

/// <summary>
/// Tests that verify CLI exit codes and basic subprocess execution.
/// These tests run the actual CLI as a subprocess.
/// </summary>
[NotInParallel("CliExitCodeTests")]
public class CliExitCodeTests : IntegrationTestBase
{
    #region Run Command Exit Codes

    [Test]
    public async Task Run_WithPassingSpecs_ReturnsZero()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "run", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Passing specs should return exit code 0");
    }

    [Test]
    public async Task Run_WithFailingSpecs_ReturnsOne()
    {
        var specDir = CreateFixture().WithFailingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "run", ".");

        await Assert.That(result.ExitCode).IsEqualTo(1)
            .Because("Failing specs should return exit code 1");
    }

    [Test]
    public async Task Run_WithMixedSpecs_ReturnsOne()
    {
        var specDir = CreateFixture()
            .WithPassingSpec("pass1")
            .WithPassingSpec("pass2")
            .WithFailingSpec("fail1")
            .Build();

        var result = await RunCliInDirectoryAsync(specDir, "run", ".");

        await Assert.That(result.ExitCode).IsEqualTo(1)
            .Because("Any failing spec should return exit code 1");
    }

    [Test]
    public async Task Run_WithPendingSpecs_ReturnsZero()
    {
        var specDir = CreateFixture().WithPendingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "run", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Pending specs should not cause failure");
    }

    #endregion

    #region List Command Exit Codes

    [Test]
    public async Task List_WithValidPath_ReturnsZero()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "list", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("List command should succeed with valid path");
    }

    [Test]
    public async Task List_JsonFormat_ProducesValidJson()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "list", ".", "--list-format", "json");

        await Assert.That(result.ExitCode).IsEqualTo(0);

        // Verify output is valid JSON
        var doc = JsonDocument.Parse(result.Output);
        await Assert.That(doc.RootElement.TryGetProperty("specs", out _)).IsTrue()
            .Because("JSON output should have 'specs' property");
    }

    [Test]
    public async Task List_TreeFormat_ProducesOutput()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "list", ".", "--list-format", "tree");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("spec")
            .Because("Tree output should list spec files");
    }

    [Test]
    public async Task List_FlatFormat_ProducesOutput()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "list", ".", "--list-format", "flat");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("spec")
            .Because("Flat output should list spec files");
    }

    #endregion

    #region Validate Command Exit Codes

    [Test]
    public async Task Validate_WithValidSpecs_ReturnsZero()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "validate", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Valid specs should pass validation");
    }

    [Test]
    public async Task Validate_WithEmptyDirectory_ShowsNoSpecsMessage()
    {
        // Empty directory shows a message
        var result = await RunCliInDirectoryAsync(_tempDir, "validate", ".");

        await Assert.That(result.Output).Contains("No spec files found")
            .Because("Should indicate no specs were found");
    }

    [Test]
    public async Task Validate_ShowsSummary()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "validate", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("Files:")
            .Because("Validate should show summary");
    }

    #endregion

    #region Init Command Exit Codes

    [Test]
    public async Task Init_InEmptyDirectory_ReturnsZero()
    {
        var result = await RunCliInDirectoryAsync(_tempDir, "init", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Init in empty directory should succeed");

        // Verify files were created
        await Assert.That(File.Exists(Path.Combine(_tempDir, "spec_helper.csx"))).IsTrue()
            .Because("Init should create spec_helper.csx");
    }

    #endregion

    #region New Command Exit Codes

    [Test]
    public async Task New_CreatesSpecFile_ReturnsZero()
    {
        var result = await RunCliInDirectoryAsync(_tempDir, "new", "MyFeature");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("New command should succeed");

        // Verify file was created
        var expectedFile = Path.Combine(_tempDir, "MyFeature.spec.csx");
        await Assert.That(File.Exists(expectedFile)).IsTrue()
            .Because("New should create the spec file");
    }

    #endregion

    #region Schema Command Exit Codes

    [Test]
    public async Task Schema_ReturnsZero()
    {
        var result = await RunCliAsync("schema");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Schema command should succeed");
    }

    [Test]
    public async Task Schema_ProducesValidJsonSchema()
    {
        var result = await RunCliAsync("schema");

        await Assert.That(result.ExitCode).IsEqualTo(0);

        // Verify output is valid JSON schema
        var doc = JsonDocument.Parse(result.Output);
        await Assert.That(doc.RootElement.TryGetProperty("type", out var typeElement)).IsTrue();
        await Assert.That(typeElement.GetString()).IsEqualTo("object");
    }

    #endregion

    #region Stats Options

    [Test]
    public async Task Run_StatsOnly_ShowsStatsAndExits()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "run", ".", "--stats-only");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("--stats-only should return 0 when no focus mode");
        await Assert.That(result.Output).Contains("Discovered")
            .Because("Should show discovered specs message");
        await Assert.That(result.Output).Contains("spec(s)")
            .Because("Should show spec count");
    }

    [Test]
    public async Task Run_StatsOnly_WithFocusedSpecs_ReturnsTwo()
    {
        var specDir = CreateFixture().WithFocusedSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "run", ".", "--stats-only");

        await Assert.That(result.ExitCode).IsEqualTo(2)
            .Because("--stats-only with focus mode should return exit code 2");
        await Assert.That(result.Output).Contains("focused")
            .Because("Should mention focused specs");
        await Assert.That(result.Output).Contains("Focus mode active")
            .Because("Should show focus mode warning");
    }

    [Test]
    public async Task Run_NoStats_DoesNotShowStats()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "run", ".", "--no-stats");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Run should succeed");
        await Assert.That(result.Output).DoesNotContain("Discovered")
            .Because("--no-stats should suppress stats display");
    }

    [Test]
    public async Task Run_Default_ShowsStats()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "run", ".");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Run should succeed");
        await Assert.That(result.Output).Contains("Discovered")
            .Because("Default run should show stats");
    }

    #endregion

    #region Error Handling

    [Test]
    public async Task Run_WithNonexistentDirectory_ReturnsNonZero()
    {
        // Create temp dir but point to nonexistent subdirectory
        var nonexistent = Path.Combine(_tempDir, "nonexistent");

        var result = await RunCliInDirectoryAsync(_tempDir, "run", nonexistent);

        await Assert.That(result.ExitCode).IsNotEqualTo(0)
            .Because("Nonexistent path should fail");
    }

    [Test]
    public async Task UnknownCommand_ReturnsNonZero()
    {
        var result = await RunCliInDirectoryAsync(_tempDir, "unknowncommand");

        await Assert.That(result.ExitCode).IsNotEqualTo(0)
            .Because("Unknown command should fail");
    }

    [Test]
    public async Task NoCommand_ShowsUsage()
    {
        var result = await RunCliInDirectoryAsync(_tempDir);

        // Running with no arguments shows usage and returns success
        await Assert.That(result.Output).Contains("draftspec")
            .Because("Should show usage information");
    }

    #endregion
}
