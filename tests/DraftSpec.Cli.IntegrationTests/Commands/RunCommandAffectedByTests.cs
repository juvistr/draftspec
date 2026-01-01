using DraftSpec.Cli.IntegrationTests.Infrastructure;

namespace DraftSpec.Cli.IntegrationTests.Commands;

/// <summary>
/// Integration tests for the run command's --affected-by option (test impact analysis).
/// Tests run the actual CLI as a subprocess with real git repositories.
/// </summary>
[NotInParallel("RunAffectedBy")]
public class RunCommandAffectedByTests : IntegrationTestBase
{
    private const string PassingSpec = """
        #r "../../../src/DraftSpec/bin/Release/net10.0/DraftSpec.dll"
        using static DraftSpec.Dsl;

        describe("Feature", () =>
        {
            it("passes", () =>
            {
                expect(1).toBe(1);
            });
        });
        """;

    private const string SourceFile = """
        namespace MyApp;

        public class Calculator
        {
            public int Add(int a, int b) => a + b;
        }
        """;

    private const string ModifiedSourceFile = """
        namespace MyApp;

        public class Calculator
        {
            public int Add(int a, int b) => a + b;
            public int Subtract(int a, int b) => a - b;
        }
        """;

    #region Staged Changes

    [Test]
    public async Task AffectedBy_Staged_ShowsImpactAnalysis()
    {
        var repoPath = CreateGitRepo()
            .WithFile("src/Calculator.cs", SourceFile)
            .WithFile("specs/Calculator.spec.csx", PassingSpec)
            .WithCommit("Initial commit")
            .WithStagedChange("src/Calculator.cs", ModifiedSourceFile)
            .Build();

        var result = await RunCliInDirectoryAsync(repoPath, "run", "specs", "--affected-by", "staged");

        await Assert.That(result.Output).Contains("Analyzing impact of changes")
            .Because("Should show impact analysis is running");
        await Assert.That(result.Output).Contains("staged")
            .Because("Should show the analysis target");
    }

    [Test]
    public async Task AffectedBy_Staged_NoChanges_ShowsNoChangesMessage()
    {
        var repoPath = CreateGitRepo()
            .WithFile("src/Calculator.cs", SourceFile)
            .WithFile("specs/Calculator.spec.csx", PassingSpec)
            .WithCommit("Initial commit")
            .Build();

        var result = await RunCliInDirectoryAsync(repoPath, "run", "specs", "--affected-by", "staged");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Should succeed with no changes");
        await Assert.That(result.Output).Contains("No changed files detected")
            .Because("Should indicate no changes found");
    }

    #endregion

    #region HEAD Comparisons

    [Test]
    public async Task AffectedBy_HeadTilde1_ShowsChangedFilesCount()
    {
        var repoPath = CreateGitRepo()
            .WithFile("src/Calculator.cs", SourceFile)
            .WithFile("specs/Calculator.spec.csx", PassingSpec)
            .WithCommit("Initial commit")
            .WithCommit("Second commit")
            .Build();

        var result = await RunCliInDirectoryAsync(repoPath, "run", "specs", "--affected-by", "HEAD~1");

        await Assert.That(result.Output).Contains("Changed files")
            .Because("Should show changed files count");
    }

    #endregion

    #region Dry Run

    [Test]
    public async Task AffectedBy_DryRun_ListsSpecsWithoutRunning()
    {
        var repoPath = CreateGitRepo()
            .WithFile("src/Calculator.cs", SourceFile)
            .WithFile("specs/Calculator.spec.csx", PassingSpec)
            .WithCommit("Initial commit")
            .WithStagedChange("src/Calculator.cs", ModifiedSourceFile)
            .Build();

        var result = await RunCliInDirectoryAsync(repoPath, "run", "specs", "--affected-by", "staged", "--dry-run");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Dry run should succeed");
        await Assert.That(result.Output).Contains("dry run")
            .Because("Should indicate dry run mode");
    }

    [Test]
    public async Task AffectedBy_DryRun_DoesNotExecuteSpecs()
    {
        var repoPath = CreateGitRepo()
            .WithFile("src/Calculator.cs", SourceFile)
            .WithFile("specs/Calculator.spec.csx", PassingSpec)
            .WithCommit("Initial commit")
            .WithStagedChange("src/Calculator.cs", ModifiedSourceFile)
            .Build();

        var result = await RunCliInDirectoryAsync(repoPath, "run", "specs", "--affected-by", "staged", "--dry-run");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        // Should not contain actual spec execution output
        await Assert.That(result.Output).DoesNotContain("passed")
            .Because("Dry run should not execute specs");
        await Assert.That(result.Output).DoesNotContain("failed")
            .Because("Dry run should not execute specs");
    }

    #endregion

    #region No Affected Specs

    [Test]
    public async Task AffectedBy_NoAffectedSpecs_ShowsNoAffectedMessage()
    {
        // Create a repo with a source file change that doesn't affect any specs
        var repoPath = CreateGitRepo()
            .WithFile("src/Calculator.cs", SourceFile)
            .WithFile("src/Unrelated.cs", "class Unrelated {}")
            .WithFile("specs/Calculator.spec.csx", PassingSpec)
            .WithCommit("Initial commit")
            .WithStagedChange("src/Unrelated.cs", "class Unrelated { int x; }")
            .Build();

        var result = await RunCliInDirectoryAsync(repoPath, "run", "specs", "--affected-by", "staged");

        await Assert.That(result.ExitCode).IsEqualTo(0)
            .Because("Should succeed with no affected specs");
        // Either no changed files or no affected specs
        var hasExpectedMessage = result.Output.Contains("No affected specs") ||
                                  result.Output.Contains("Affected specs: 0");
        await Assert.That(hasExpectedMessage).IsTrue()
            .Because("Should indicate no specs were affected");
    }

    #endregion

    #region Not a Git Repository

    [Test]
    public async Task AffectedBy_NotGitRepo_ShowsError()
    {
        // Create a regular directory without git
        var specDir = CreateFixture().WithPassingSpec().Build();

        var result = await RunCliInDirectoryAsync(specDir, "run", ".", "--affected-by", "staged");

        await Assert.That(result.ExitCode).IsEqualTo(1)
            .Because("Should fail in non-git directory");
        await Assert.That(result.Output).Contains("Failed to get changed files")
            .Because("Should show git error message");
    }

    #endregion

    #region Error Handling

    [Test]
    public async Task AffectedBy_InvalidRef_ReturnsExitCodeOne()
    {
        var repoPath = CreateGitRepo()
            .WithFile("src/Calculator.cs", SourceFile)
            .WithFile("specs/Calculator.spec.csx", PassingSpec)
            .WithCommit("Initial commit")
            .Build();

        var result = await RunCliInDirectoryAsync(repoPath, "run", "specs", "--affected-by", "nonexistent-branch");

        await Assert.That(result.ExitCode).IsEqualTo(1)
            .Because("Should fail for invalid git ref");
        await Assert.That(result.Output).Contains("Failed to get changed files")
            .Because("Should show error message");
    }

    #endregion
}
