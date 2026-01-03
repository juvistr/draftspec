using DraftSpec.Cli.IntegrationTests.Infrastructure;

namespace DraftSpec.Cli.IntegrationTests.Workflows;

/// <summary>
/// End-to-end workflow tests that verify complete user journeys.
/// These tests simulate real user scenarios from project initialization to spec execution.
/// </summary>
[NotInParallel("WorkflowTests")]
public class InitNewRunWorkflowTests : IntegrationTestBase
{
    /// <summary>
    /// Tests the complete new user workflow: init → new → run.
    /// This is the most common onboarding path for new DraftSpec users.
    /// </summary>
    [Test]
    public async Task InitNewRun_CompleteWorkflow_Succeeds()
    {
        // Step 1: Initialize a new DraftSpec project
        var initResult = await RunCliInDirectoryAsync(_tempDir, "init", ".");

        await Assert.That(initResult.ExitCode).IsEqualTo(0)
            .Because("init command should succeed");
        await Assert.That(File.Exists(Path.Combine(_tempDir, "spec_helper.csx"))).IsTrue()
            .Because("init should create spec_helper.csx");

        // Step 2: Create a new spec file
        var newResult = await RunCliInDirectoryAsync(_tempDir, "new", "MyFeature");

        await Assert.That(newResult.ExitCode).IsEqualTo(0)
            .Because("new command should succeed");

        var specFile = Path.Combine(_tempDir, "MyFeature.spec.csx");
        await Assert.That(File.Exists(specFile)).IsTrue()
            .Because("new should create MyFeature.spec.csx");

        // Step 3: Run the specs (the generated spec should pass)
        var runResult = await RunCliInDirectoryAsync(_tempDir, "run", ".");

        await Assert.That(runResult.ExitCode).IsEqualTo(0)
            .Because("generated spec should pass");
        await Assert.That(runResult.Output).Contains("MyFeature")
            .Because("output should mention the spec name");
    }

    /// <summary>
    /// Tests that init followed by run succeeds with message when no specs exist.
    /// Having no specs is not an error - just nothing to run.
    /// </summary>
    [Test]
    public async Task InitThenRun_WithoutNewSpec_ReturnsSuccessWithMessage()
    {
        // Initialize
        var initResult = await RunCliInDirectoryAsync(_tempDir, "init", ".");
        await Assert.That(initResult.ExitCode).IsEqualTo(0);

        // Run immediately - should report no specs found but succeed
        var runResult = await RunCliInDirectoryAsync(_tempDir, "run", ".");

        // No specs to run is not an error - just exit cleanly with a message
        await Assert.That(runResult.ExitCode).IsEqualTo(0)
            .Because("running without specs should succeed with a message");
        await Assert.That(runResult.Output).Contains("No spec files found")
            .Because("should indicate no specs were found");
    }

    /// <summary>
    /// Tests creating multiple specs and running them together.
    /// </summary>
    [Test]
    public async Task NewMultipleSpecs_ThenRun_ExecutesAll()
    {
        // Initialize
        await RunCliInDirectoryAsync(_tempDir, "init", ".");

        // Create multiple specs
        await RunCliInDirectoryAsync(_tempDir, "new", "FeatureA");
        await RunCliInDirectoryAsync(_tempDir, "new", "FeatureB");
        await RunCliInDirectoryAsync(_tempDir, "new", "FeatureC");

        // Verify all files exist
        await Assert.That(File.Exists(Path.Combine(_tempDir, "FeatureA.spec.csx"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(_tempDir, "FeatureB.spec.csx"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(_tempDir, "FeatureC.spec.csx"))).IsTrue();

        // Run all specs
        var runResult = await RunCliInDirectoryAsync(_tempDir, "run", ".");

        await Assert.That(runResult.ExitCode).IsEqualTo(0)
            .Because("all generated specs should pass");
    }

    /// <summary>
    /// Tests the init → new → validate → run workflow.
    /// </summary>
    [Test]
    public async Task InitNewValidateRun_CompleteWorkflow_Succeeds()
    {
        // Init
        await RunCliInDirectoryAsync(_tempDir, "init", ".");

        // New
        await RunCliInDirectoryAsync(_tempDir, "new", "ValidationTest");

        // Validate - should pass for generated spec
        var validateResult = await RunCliInDirectoryAsync(_tempDir, "validate", ".");
        await Assert.That(validateResult.ExitCode).IsEqualTo(0)
            .Because("generated spec should be valid");

        // Run
        var runResult = await RunCliInDirectoryAsync(_tempDir, "run", ".");
        await Assert.That(runResult.ExitCode).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that list command works after init and new.
    /// </summary>
    [Test]
    public async Task InitNewList_ShowsCreatedSpecs()
    {
        // Init and create specs
        await RunCliInDirectoryAsync(_tempDir, "init", ".");
        await RunCliInDirectoryAsync(_tempDir, "new", "ListTest");

        // List in JSON format
        var listResult = await RunCliInDirectoryAsync(_tempDir, "list", ".", "--list-format", "json");

        await Assert.That(listResult.ExitCode).IsEqualTo(0);
        await Assert.That(listResult.Output).Contains("ListTest")
            .Because("list should show the created spec");
    }

    /// <summary>
    /// Tests running a specific spec file after creating multiple.
    /// </summary>
    [Test]
    public async Task NewMultiple_RunSingle_ExecutesOnlySpecified()
    {
        // Initialize and create specs
        await RunCliInDirectoryAsync(_tempDir, "init", ".");
        await RunCliInDirectoryAsync(_tempDir, "new", "Alpha");
        await RunCliInDirectoryAsync(_tempDir, "new", "Beta");

        // Run only Alpha
        var runResult = await RunCliInDirectoryAsync(_tempDir, "run", "Alpha.spec.csx");

        await Assert.That(runResult.ExitCode).IsEqualTo(0);
        await Assert.That(runResult.Output).Contains("Alpha")
            .Because("output should mention the executed spec");
    }
}
