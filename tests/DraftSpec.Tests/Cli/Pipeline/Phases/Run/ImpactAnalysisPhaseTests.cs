using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Run;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Pipeline.Phases.Run;

/// <summary>
/// Tests for <see cref="ImpactAnalysisPhase"/>.
/// </summary>
/// <remarks>
/// Note: Some tests require real directories because DependencyGraphBuilder
/// uses Directory.EnumerateFiles internally. Full integration tests for
/// affected spec detection would need to use a temporary directory with
/// real spec files.
/// </remarks>
public class ImpactAnalysisPhaseTests
{
    private MockConsole _console = null!;
    private MockFileSystem _fileSystem = null!;
    private MockGitService _gitService = null!;
    private MockPathComparer _pathComparer = null!;

    [Before(Test)]
    public void SetUp()
    {
        _console = new MockConsole();
        _fileSystem = new MockFileSystem();
        _gitService = new MockGitService();
        _pathComparer = new MockPathComparer();
    }

    #region No AffectedBy Tests

    [Test]
    public async Task ExecuteAsync_NoAffectedBy_PassesThroughUnchanged()
    {
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer);
        var context = CreateContext();
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsTrue();
        await Assert.That(_gitService.GetChangedFilesCalls).IsEmpty();
    }

    [Test]
    public async Task ExecuteAsync_EmptyAffectedBy_PassesThroughUnchanged()
    {
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer);
        var context = CreateContext(affectedBy: "");
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsTrue();
    }

    #endregion

    #region No Spec Files Tests

    [Test]
    public async Task ExecuteAsync_NoSpecFiles_ReturnsZero()
    {
        _gitService.WithChangedFiles("/project/src/Service.cs");
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer);
        var context = CreateContext(affectedBy: "HEAD~1", specFiles: []);
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsFalse();
        await Assert.That(_console.Output).Contains("No spec files to analyze");
    }

    [Test]
    public async Task ExecuteAsync_NullSpecFiles_ReturnsZero()
    {
        _gitService.WithChangedFiles("/project/src/Service.cs");
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer);
        var context = CreateContext(affectedBy: "HEAD~1");
        // Don't set SpecFiles - it will be null
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsFalse();
    }

    #endregion

    #region Git Error Tests

    [Test]
    public async Task ExecuteAsync_GitError_ReturnsError()
    {
        _gitService.ThrowsOnGetChangedFiles(new InvalidOperationException("Git not found"));
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer);
        var specFiles = new List<string> { "/project/specs/test.spec.csx" };
        var context = CreateContext(affectedBy: "HEAD~1", specFiles: specFiles);

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("Failed to get changed files");
        await Assert.That(_console.Errors).Contains("Git not found");
    }

    #endregion

    #region No Changed Files Tests

    [Test]
    public async Task ExecuteAsync_NoChangedFiles_ReturnsZero()
    {
        _gitService.WithChangedFiles(); // empty
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer);
        var specFiles = new List<string> { "/project/specs/test.spec.csx" };
        var context = CreateContext(affectedBy: "HEAD~1", specFiles: specFiles);
        var pipelineCalled = false;

        var result = await phase.ExecuteAsync(
            context,
            (_, _) =>
            {
                pipelineCalled = true;
                return Task.FromResult(0);
            },
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(pipelineCalled).IsFalse();
        await Assert.That(_console.Output).Contains("No changed files detected");
    }

    #endregion

    #region Git Service Integration Tests

    [Test]
    public async Task ExecuteAsync_CallsGitServiceCorrectly()
    {
        _gitService.WithChangedFiles(); // empty - avoids DependencyGraphBuilder
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer);
        var specFiles = new List<string> { "/project/specs/test.spec.csx" };
        var context = CreateContext(affectedBy: "staged", projectPath: "/my/project", specFiles: specFiles);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_gitService.GetChangedFilesCalls).Count().IsEqualTo(1);
        await Assert.That(_gitService.GetChangedFilesCalls[0].Reference).IsEqualTo("staged");
        await Assert.That(_gitService.GetChangedFilesCalls[0].WorkingDirectory).IsEqualTo("/my/project");
    }

    #endregion

    #region Console Output Tests

    [Test]
    public async Task ExecuteAsync_OutputsImpactAnalysisMessage()
    {
        _gitService.WithChangedFiles(); // empty
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer);
        var specFiles = new List<string> { "/project/specs/test.spec.csx" };
        var context = CreateContext(affectedBy: "HEAD~1", specFiles: specFiles);

        await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(_console.Output).Contains("Analyzing impact of changes: HEAD~1");
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task ExecuteAsync_NoProjectPath_ReturnsError()
    {
        var phase = new ImpactAnalysisPhase(_gitService, _pathComparer);
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        context.Set(ContextKeys.AffectedBy, "HEAD~1");

        var result = await phase.ExecuteAsync(
            context,
            (_, _) => Task.FromResult(0),
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("ProjectPath not set");
    }

    #endregion

    #region Helper Methods

    private CommandContext CreateContext(
        string? affectedBy = null,
        string projectPath = "/project",
        IReadOnlyList<string>? specFiles = null,
        bool dryRun = false)
    {
        var context = new CommandContext
        {
            Path = ".",
            Console = _console,
            FileSystem = _fileSystem
        };
        context.Set(ContextKeys.ProjectPath, projectPath);

        if (affectedBy != null)
            context.Set(ContextKeys.AffectedBy, affectedBy);

        if (specFiles != null)
            context.Set<IReadOnlyList<string>>(ContextKeys.SpecFiles, specFiles);

        context.Set(ContextKeys.DryRun, dryRun);

        return context;
    }

    #endregion
}
